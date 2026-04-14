using Microsoft.EntityFrameworkCore;
using GamingDW.Core.Models;

namespace GamingDW.Core.Data;

public class GamingDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<GameplayLog> GameplayLogs => Set<GameplayLog>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();
    public DbSet<KpiTarget> KpiTargets => Set<KpiTarget>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Used by DataGenerator to set the DB path directly.
    /// </summary>
    public static string? DefaultDbPath { get; set; }

    public GamingDbContext() { }

    public GamingDbContext(DbContextOptions<GamingDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            var path = DefaultDbPath ?? Path.Combine(AppContext.BaseDirectory, "GamingDW.db");
            options.UseSqlite($"Data Source={path}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Gaming data indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.RegistrationDate);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.UserId);
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Timestamp);

        modelBuilder.Entity<GameplayLog>()
            .HasIndex(g => g.UserId);
        modelBuilder.Entity<GameplayLog>()
            .HasIndex(g => g.Timestamp);

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => s.UserId);
        modelBuilder.Entity<UserSession>()
            .HasIndex(s => s.LoginTime);

        // Enum-to-string
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<GameplayLog>()
            .Property(g => g.GameType)
            .HasConversion<string>();

        // StaffUser unique username
        modelBuilder.Entity<StaffUser>()
            .HasIndex(s => s.Username)
            .IsUnique();

        // DailyReport unique date
        modelBuilder.Entity<DailyReport>()
            .HasIndex(d => d.Date)
            .IsUnique();

        // KpiTarget composite index
        modelBuilder.Entity<KpiTarget>()
            .HasIndex(t => new { t.Period, t.PeriodStart, t.MetricName });

        // AuditLog indexes for querying
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.EntityType);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityType, a.EntityId });

        // Global query filters for soft delete
        modelBuilder.Entity<DailyReport>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<KpiTarget>().HasQueryFilter(x => !x.IsDeleted);
    }
}
