using Serilog;
using GamingDW.Core.Data;
using GamingDW.WebApp.Auth;
using GamingDW.WebApp.Endpoints;
using GamingDW.WebApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

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

    // ─── Service Registration (extracted to extension methods) ───
    builder.Services.AddDatabaseServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddAuthenticationServices(builder.Configuration);
    builder.Services.AddObservability();

    var app = builder.Build();

    // ─── DB Init — fail fast, no EnsureCreated fallback ───
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GamingDbContext>();
        try
        {
            db.Database.Migrate();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration failed. Run 'dotnet ef database update' to apply migrations.");
            throw; // Fail fast — do not silently fall back to EnsureCreated
        }

        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        await auth.SeedAdminAsync();
    }

    // ─── Middleware Pipeline ───
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

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
