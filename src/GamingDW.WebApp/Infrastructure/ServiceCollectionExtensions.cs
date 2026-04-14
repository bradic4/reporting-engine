using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using GamingDW.Core.Data;
using GamingDW.WebApp.Auth;
using GamingDW.WebApp.Services;
using GamingDW.WebApp.Validation;

namespace GamingDW.WebApp.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the database provider (SQLite or PostgreSQL) based on configuration.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration config)
    {
        var dbProvider = config.GetValue<string>("DatabaseProvider")?.ToLower() ?? "sqlite";
        if (dbProvider == "postgres")
        {
            var connStr = config.GetConnectionString("PostgresConnection");
            services.AddDbContext<GamingDbContext>(opt => opt.UseNpgsql(connStr));
        }
        else
        {
            var connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=GamingDW.db";
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
            services.AddDbContext<GamingDbContext>(opt => opt.UseSqlite(connStr));
        }
        return services;
    }

    /// <summary>
    /// Registers all application services (scoped, transient).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<ITargetService, TargetService>();
        services.AddScoped<ILiveMetricsService, LiveMetricsService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddTransient<GlobalExceptionHandler>();

        services.AddHostedService<DailySummaryJob>();

        return services;
    }

    /// <summary>
    /// Configures cookie authentication with lockout and rate limiting.
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AdminSettings>(config.GetSection("AdminSettings"));
        var envPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
            services.PostConfigure<AdminSettings>(s => s.DefaultPassword = envPassword);

        var cookieHours = config.GetValue("CookieSettings:ExpireHours", 8);
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opt =>
            {
                opt.LoginPath = "/login.html";
                opt.ExpireTimeSpan = TimeSpan.FromHours(cookieHours);
                opt.SlidingExpiration = config.GetValue("CookieSettings:SlidingExpiration", true);
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanEditReports", p => p.RequireClaim("CanEditReports", "True"));
            options.AddPolicy("CanManageStaff", p => p.RequireClaim("CanManageStaff", "True"));
            options.AddPolicy("CanSetTargets", p => p.RequireClaim("CanSetTargets", "True"));
            options.AddPolicy("CanViewLive", p => p.RequireClaim("CanViewLive", "True"));
        });

        services.AddRateLimiter(opt =>
        {
            opt.RejectionStatusCode = 429;
            opt.AddFixedWindowLimiter("login", o => { o.PermitLimit = 5; o.Window = TimeSpan.FromMinutes(1); });
        });

        return services;
    }

    /// <summary>
    /// Configures observability: health checks, Swagger, FluentValidation.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<GamingDbContext>(tags: ["database"]);

        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new() { Title = "Gaming DW API", Version = "v1",
                Description = "Marketing Analytics Workbench — Enterprise API" });
        });

        return services;
    }
}
