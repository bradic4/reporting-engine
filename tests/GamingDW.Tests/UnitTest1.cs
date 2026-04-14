using System.Text.Json;
using GamingDW.Core.Data;
using GamingDW.Core.Models;
using GamingDW.WebApp;
using GamingDW.WebApp.Auth;
using GamingDW.WebApp.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GamingDW.Tests;

/// <summary>
/// Creates a fresh in-memory DbContext for each test.
/// </summary>
public static class TestDbFactory
{
    public static GamingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<GamingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new GamingDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}

// ═══════════════════════════════════════
// AUTH SERVICE TESTS
// ═══════════════════════════════════════
public class AuthServiceTests
{
    private AuthService CreateService(GamingDbContext db, string adminPassword = "TestAdmin123!")
    {
        var settings = Options.Create(new AdminSettings
        {
            DefaultPassword = adminPassword,
            MaxFailedAttempts = 3,
            LockoutMinutes = 15
        });
        return new AuthService(db, settings, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task SeedAdmin_CreatesAdminUser()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        await svc.SeedAdminAsync();

        var admin = await db.StaffUsers.FirstOrDefaultAsync(u => u.Username == "admin");
        admin.Should().NotBeNull();
        admin!.CanManageStaff.Should().BeTrue();
        admin.PasswordHash.Should().NotBe("admin");
    }

    [Fact]
    public async Task SeedAdmin_DoesNotDuplicate()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        await svc.SeedAdminAsync();
        await svc.SeedAdminAsync();

        db.StaffUsers.Count(u => u.Username == "admin").Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_CorrectPassword_ReturnsUser()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db, "MyPassword123!");
        await svc.SeedAdminAsync();

        var user = await svc.ValidateAsync("admin", "MyPassword123!");

        user.Should().NotBeNull();
        user!.Username.Should().Be("admin");
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task ValidateAsync_WrongPassword_ReturnsNull()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        await svc.SeedAdminAsync();

        var user = await svc.ValidateAsync("admin", "wrong");

        user.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_IncreasesFailedAttempts()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        await svc.SeedAdminAsync();

        await svc.ValidateAsync("admin", "wrong1");
        await svc.ValidateAsync("admin", "wrong2");

        var admin = await db.StaffUsers.FirstAsync(u => u.Username == "admin");
        admin.FailedLoginAttempts.Should().Be(2);
    }

    [Fact]
    public async Task ValidateAsync_LocksAfterMaxAttempts()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db); // MaxFailedAttempts = 3
        await svc.SeedAdminAsync();

        await svc.ValidateAsync("admin", "wrong1");
        await svc.ValidateAsync("admin", "wrong2");
        await svc.ValidateAsync("admin", "wrong3"); // Triggers lockout

        var admin = await db.StaffUsers.FirstAsync(u => u.Username == "admin");
        admin.LockoutEnd.Should().NotBeNull();
        admin.LockoutEnd.Should().BeAfter(DateTime.UtcNow);

        // Even correct password should fail during lockout
        var user = await svc.ValidateAsync("admin", "TestAdmin123!");
        user.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_NonexistentUser_ReturnsNull()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        var user = await svc.ValidateAsync("nobody", "password");

        user.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_SuccessResetsFailedAttempts()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db, "Correct123!");
        await svc.SeedAdminAsync();

        await svc.ValidateAsync("admin", "wrong"); // 1 failure
        await svc.ValidateAsync("admin", "Correct123!"); // success

        var admin = await db.StaffUsers.FirstAsync(u => u.Username == "admin");
        admin.FailedLoginAttempts.Should().Be(0);
        admin.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashes()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        var hash1 = svc.HashPassword("test");
        var hash2 = svc.HashPassword("test");

        // PasswordHasher uses random salt, so hashes should differ
        hash1.Should().NotBe(hash2);
    }
}

// ═══════════════════════════════════════
// REPORT SERVICE TESTS
// ═══════════════════════════════════════
public class ReportServiceTests
{
    private ReportService CreateService(GamingDbContext db) =>
        new(db, new AuditService(db, NullLogger<AuditService>.Instance), NullLogger<ReportService>.Instance);

    [Fact]
    public async Task CreateReport_ValidData_Succeeds()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var body = new DailyReportRequest("2026-01-15", 45, 12, 15000, 8000, 7000, 320, 1200, 500, "Test");

        var result = await svc.CreateReportAsync(body, "testuser");

        result.Error.Should().BeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Date.Should().Be("2026-01-15");
    }

    [Fact]
    public async Task CreateReport_DuplicateDate_ReturnsError()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var body = new DailyReportRequest("2026-01-15", 45, 12, 15000, 8000, 7000, 320, 1200, 500, null);

        await svc.CreateReportAsync(body, "user1");
        var result = await svc.CreateReportAsync(body, "user2");

        result.Error.Should().Be("Report already exists for this date");
    }

    [Fact]
    public async Task CreateReport_InvalidDate_ReturnsError()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var body = new DailyReportRequest("not-a-date", 0, 0, 0, 0, 0, 0, 0, 0, null);

        var result = await svc.CreateReportAsync(body, "user");

        result.Error.Should().Be("Invalid date");
    }

    [Fact]
    public async Task UpdateReport_ExistingReport_Succeeds()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var create = await svc.CreateReportAsync(
            new DailyReportRequest("2026-02-01", 10, 5, 1000, 500, 500, 100, 200, 50, null), "user");

        var result = await svc.UpdateReportAsync(create.Id!.Value,
            new DailyReportRequest("2026-02-01", 99, 5, 1000, 500, 500, 100, 200, 50, "updated"), "user");

        result.Error.Should().BeNull();
        var updated = await db.DailyReports.FindAsync(create.Id);
        updated!.Registrations.Should().Be(99);
        updated.Notes.Should().Be("updated");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteReport_ExistingReport_ReturnsTrue()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var create = await svc.CreateReportAsync(
            new DailyReportRequest("2026-03-01", 0, 0, 0, 0, 0, 0, 0, 0, null), "user");

        var result = await svc.DeleteReportAsync(create.Id!.Value, "user");

        result.Should().BeTrue();
        db.DailyReports.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteReport_NonExistent_ReturnsFalse()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        var result = await svc.DeleteReportAsync(999, "user");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetDailyReports_FiltersByDate()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        await svc.CreateReportAsync(new DailyReportRequest("2026-01-10", 0, 0, 0, 0, 0, 0, 0, 0, null), "u");
        await svc.CreateReportAsync(new DailyReportRequest("2026-01-20", 0, 0, 0, 0, 0, 0, 0, 0, null), "u");
        await svc.CreateReportAsync(new DailyReportRequest("2026-02-01", 0, 0, 0, 0, 0, 0, 0, 0, null), "u");

        var filtered = await svc.GetDailyReportsAsync("2026-01-15", "2026-01-31");

        filtered.Should().HaveCount(1);
    }

    [Fact]
    public async Task ComparePeriodsAsync_ReturnsSums()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        await svc.CreateReportAsync(new DailyReportRequest("2026-01-01", 10, 5, 1000, 500, 500, 100, 200, 50, null), "u");
        await svc.CreateReportAsync(new DailyReportRequest("2026-01-02", 20, 10, 2000, 1000, 1000, 200, 400, 100, null), "u");

        var result = await svc.ComparePeriodsAsync("2026-01-01", "2026-01-01", "2026-01-02", "2026-01-02");

        result.Period1.Registrations.Should().Be(10);
        result.Period2.Registrations.Should().Be(20);
    }
}

// ═══════════════════════════════════════
// TARGET SERVICE TESTS
// ═══════════════════════════════════════
public class TargetServiceTests
{
    private TargetService CreateService(GamingDbContext db) =>
        new(db, new AuditService(db, NullLogger<AuditService>.Instance), NullLogger<TargetService>.Instance);

    [Fact]
    public async Task CreateTarget_ValidData_Succeeds()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        var body = new KpiTargetRequest("daily", "2026-03-01", "GGR", 10000);

        var result = await svc.CreateTargetAsync(body, "admin");

        result.Error.Should().BeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTarget_InvalidDate_ReturnsError()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        var result = await svc.CreateTargetAsync(new KpiTargetRequest("daily", "bad", "GGR", 100), "user");

        result.Error.Should().Be("Invalid date");
    }

    [Fact]
    public async Task DeleteTarget_NonExistent_ReturnsFalse()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);

        (await svc.DeleteTargetAsync(999, "user")).Should().BeFalse();
    }

    [Fact]
    public async Task GetProgress_WithTargetAndReport_CalculatesPercentage()
    {
        using var db = TestDbFactory.Create();
        var svc = CreateService(db);
        // Add a daily report
        db.DailyReports.Add(new DailyReport
        {
            Date = new DateOnly(2026, 3, 1), Registrations = 75, FTDs = 0, Deposits = 0,
            Withdrawals = 0, GGR = 0, ActivePlayers = 0, Sessions = 0, BonusCost = 0,
            CreatedBy = "test", CreatedAt = DateTime.UtcNow
        });
        // Add a target
        db.KpiTargets.Add(new KpiTarget
        {
            Period = "daily", PeriodStart = new DateOnly(2026, 3, 1),
            MetricName = "Registrations", TargetValue = 100, CreatedBy = "test", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.GetProgressAsync("2026-03-01");

        var progressArr = result.Progress.First();
        progressArr.Actual.Should().Be(75);
        progressArr.ProgressPct.Should().Be(75.0m);
    }
}

// ═══════════════════════════════════════
// VALIDATION TESTS
// ═══════════════════════════════════════
public class ValidatorTests
{
    [Fact]
    public void DailyReportValidator_NegativeDeposits_Fails()
    {
        var validator = new GamingDW.WebApp.Validation.DailyReportRequestValidator();
        var body = new DailyReportRequest("2026-01-01", 0, 0, -100, 0, 0, 0, 0, 0, null);

        var result = validator.Validate(body);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Deposits");
    }

    [Fact]
    public void KpiTargetValidator_InvalidPeriod_Fails()
    {
        var validator = new GamingDW.WebApp.Validation.KpiTargetRequestValidator();
        var body = new KpiTargetRequest("yearly", "2026-01-01", "GGR", 100);

        var result = validator.Validate(body);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Period");
    }

    [Fact]
    public void LoginValidator_EmptyUsername_Fails()
    {
        var validator = new GamingDW.WebApp.Validation.LoginRequestValidator();
        var body = new LoginRequest("", "password");

        var result = validator.Validate(body);

        result.IsValid.Should().BeFalse();
    }
}
