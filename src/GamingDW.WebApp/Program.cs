using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using Serilog;
using GamingDW.Core.Data;
using GamingDW.WebApp.Auth;
using GamingDW.WebApp.Endpoints;
using GamingDW.WebApp.Infrastructure;
using GamingDW.WebApp.Services;
using GamingDW.WebApp.Validation;

// ─── Serilog bootstrap ───
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gamingdw-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog integration
    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/gamingdw-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
        .Enrich.FromLogContext());

    // ─── Database ───
    var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider")?.ToLower() ?? "sqlite";
    if (dbProvider == "postgres")
    {
        var connStr = builder.Configuration.GetConnectionString("PostgresConnection");
        builder.Services.AddDbContext<GamingDbContext>(opt => opt.UseNpgsql(connStr));
    }
    else
    {
        var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=GamingDW.db";
        if (connStr.StartsWith("Data Source=") && !Path.IsPathRooted(connStr.Replace("Data Source=", "")))
        {
            var dbFile = connStr.Replace("Data Source=", "");
            var dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 5; i++)
            {
                var candidate = Path.Combine(dir, dbFile);
                if (File.Exists(candidate)) { connStr = $"Data Source={candidate}"; break; }
                dir = Directory.GetParent(dir)?.FullName ?? dir;
            }
        }
        builder.Services.AddDbContext<GamingDbContext>(opt => opt.UseSqlite(connStr));
    }

    // ─── Configuration ───
    builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));
    var envPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
    if (!string.IsNullOrEmpty(envPassword))
        builder.Services.PostConfigure<AdminSettings>(s => s.DefaultPassword = envPassword);

    // ─── Services ───
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
    builder.Services.AddScoped<ITargetService, TargetService>();
    builder.Services.AddScoped<ILiveMetricsService, LiveMetricsService>();
    builder.Services.AddScoped<IStaffService, StaffService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddTransient<GlobalExceptionHandler>();

    // ─── Background Jobs ───
    builder.Services.AddHostedService<DailySummaryJob>();

    // ─── Health Checks ───
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<GamingDbContext>(tags: ["database"]);

    // ─── FluentValidation ───
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    // ─── Swagger / OpenAPI ───
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opt =>
    {
        opt.SwaggerDoc("v1", new() { Title = "Gaming DW API", Version = "v1",
            Description = "Marketing Analytics Workbench — Enterprise API" });
    });

    // ─── Authentication ───
    var cookieHours = builder.Configuration.GetValue("CookieSettings:ExpireHours", 8);
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(opt =>
        {
            opt.LoginPath = "/login.html";
            opt.ExpireTimeSpan = TimeSpan.FromHours(cookieHours);
            opt.SlidingExpiration = builder.Configuration.GetValue("CookieSettings:SlidingExpiration", true);
            opt.Cookie.HttpOnly = true;
            opt.Cookie.SameSite = SameSiteMode.Strict;
            opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            opt.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                { ctx.Response.StatusCode = 401; return Task.CompletedTask; }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });
    builder.Services.AddAuthorization();

    // ─── Rate Limiting ───
    builder.Services.AddRateLimiter(opt =>
    {
        opt.RejectionStatusCode = 429;
        opt.AddFixedWindowLimiter("login", o => { o.PermitLimit = 5; o.Window = TimeSpan.FromMinutes(1); });
    });

    var app = builder.Build();

    // ─── DB Init ───
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GamingDbContext>();
        try { db.Database.Migrate(); Log.Information("Migrations applied"); }
        catch { Log.Warning("Migration failed, using EnsureCreated"); db.Database.EnsureCreated(); }

        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        await auth.SeedAdminAsync();
    }

    // ─── Middleware Pipeline ───
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

    // Swagger (dev only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opt => opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Gaming DW API v1"));
    }

    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    // ─── Root ───
    app.MapGet("/", (HttpContext ctx) =>
        ctx.User.Identity?.IsAuthenticated == true
            ? Results.Redirect("/index.html")
            : Results.Redirect("/login.html"));

    // ─── Endpoints ───
    app.MapAuthEndpoints();
    app.MapReportEndpoints();
    app.MapTargetEndpoints();
    app.MapLiveEndpoints();
    app.MapStaffEndpoints();
    app.MapAuditEndpoints();
    app.MapExportEndpoints();
    app.MapHealthEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
