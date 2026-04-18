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

        var gdb = scope.ServiceProvider.GetRequiredService<GamingDbContext>();
        if (!gdb.DailyReports.Any())
        {
            var rnd = new Random();
            for (int i = 14; i >= 1; i--)
            {
                var d = DateOnly.FromDateTime(DateTime.Today.AddDays(-i));
                gdb.DailyReports.Add(new GamingDW.Core.Models.DailyReport
                {
                    Id = 0,
                    Date = d,
                    Registrations = rnd.Next(50, 250),
                    FTDs = rnd.Next(20, 100),
                    Deposits = (decimal)(rnd.NextDouble() * 100000 + 20000),
                    Withdrawals = (decimal)(rnd.NextDouble() * 30000 + 5000),
                    GGR = (decimal)(rnd.NextDouble() * 80000 + 10000),
                    ActivePlayers = rnd.Next(300, 1200),
                    Sessions = rnd.Next(1000, 5000),
                    BonusCost = (decimal)(rnd.NextDouble() * 20000 + 2000),
                    Notes = i % 3 == 0 ? "Weekend promo" : null
                });
            }
            await gdb.SaveChangesAsync();
            Log.Information("Added sample daily reports for demonstration.");
        }

        if (!gdb.Users.Any())
        {
            var rng = new Random(42);
            Log.Information("Generating 1000 users...");
            var users = GamingDW.DataGenerator.Generators.UserGenerator.Generate(1000, rng);
            gdb.Users.AddRange(users);
            await gdb.SaveChangesAsync();

            Log.Information("Generating activity data (sessions, transactions, gameplay logs)...");
            var (sessions, transactions, gameplayLogs) = GamingDW.DataGenerator.Generators.ActivityGenerator.GenerateAll(users, rng);

            foreach (var batch in sessions.Chunk(5000))
            {
                gdb.UserSessions.AddRange(batch);
                await gdb.SaveChangesAsync();
                gdb.ChangeTracker.Clear();
            }

            foreach (var batch in transactions.Chunk(5000))
            {
                gdb.Transactions.AddRange(batch);
                await gdb.SaveChangesAsync();
                gdb.ChangeTracker.Clear();
            }

            foreach (var batch in gameplayLogs.Chunk(5000))
            {
                gdb.GameplayLogs.AddRange(batch);
                await gdb.SaveChangesAsync();
                gdb.ChangeTracker.Clear();
            }

            // Seed Kpi Targets
            var kpiTargets = new List<GamingDW.Core.Models.KpiTarget>
            {
                new GamingDW.Core.Models.KpiTarget { MetricName = "GGR", TargetValue = 500000, Period = "Monthly", PeriodStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), CreatedBy = "admin", CreatedAt = DateTime.UtcNow },
                new GamingDW.Core.Models.KpiTarget { MetricName = "Deposits", TargetValue = 1000000, Period = "Monthly", PeriodStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), CreatedBy = "admin", CreatedAt = DateTime.UtcNow },
                new GamingDW.Core.Models.KpiTarget { MetricName = "ActivePlayers", TargetValue = 1000, Period = "Daily", PeriodStart = DateOnly.FromDateTime(DateTime.UtcNow), CreatedBy = "admin", CreatedAt = DateTime.UtcNow }
            };
            gdb.KpiTargets.AddRange(kpiTargets);
            await gdb.SaveChangesAsync();

            Log.Information("Detailed game dummy data imported successfully!");
        }
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
