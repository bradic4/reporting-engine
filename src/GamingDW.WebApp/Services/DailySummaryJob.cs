using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

/// <summary>
/// Background service that generates a daily summary when the date changes.
/// Demonstrates IHostedService pattern for enterprise-grade batch processing.
/// </summary>
public class DailySummaryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySummaryJob> _logger;

    public DailySummaryJob(IServiceScopeFactory scopeFactory, ILogger<DailySummaryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailySummaryJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDailySummaryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailySummaryJob");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessDailySummaryAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GamingDbContext>();

        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        // Check if yesterday's report already exists
        var exists = await db.DailyReports.AnyAsync(r => r.Date == yesterday);
        if (exists)
        {
            _logger.LogDebug("Daily summary for {Date} already exists, skipping", yesterday);
            return;
        }

        // Auto-generate from live data if available
        var start = yesterday.ToDateTime(TimeOnly.MinValue);
        var end = yesterday.ToDateTime(TimeOnly.MaxValue);

        var sessionsCount = await db.UserSessions.CountAsync(s => s.LoginTime >= start && s.LoginTime <= end);
        if (sessionsCount == 0)
        {
            _logger.LogDebug("No session data for {Date}, skipping auto-summary", yesterday);
            return;
        }

        var activePlayers = await db.UserSessions
            .Where(s => s.LoginTime >= start && s.LoginTime <= end)
            .Select(s => s.UserId).Distinct().CountAsync();

        var deposits = await db.Transactions
            .Where(t => t.Timestamp >= start && t.Timestamp <= end && t.Type == TransactionType.Deposit)
            .SumAsync(t => t.Amount);
        var withdrawals = await db.Transactions
            .Where(t => t.Timestamp >= start && t.Timestamp <= end && t.Type == TransactionType.Withdrawal)
            .SumAsync(t => t.Amount);

        var bets = await db.GameplayLogs
            .Where(g => g.Timestamp >= start && g.Timestamp <= end)
            .SumAsync(g => g.BetAmount);
        var wins = await db.GameplayLogs
            .Where(g => g.Timestamp >= start && g.Timestamp <= end)
            .SumAsync(g => g.WinAmount);

        var report = new DailyReport
        {
            Date = yesterday,
            Registrations = await db.Users.CountAsync(u => u.RegistrationDate >= start && u.RegistrationDate <= end),
            FTDs = 0, // Would need first deposit tracking
            Deposits = deposits,
            Withdrawals = withdrawals,
            GGR = bets - wins,
            ActivePlayers = activePlayers,
            Sessions = sessionsCount,
            BonusCost = 0,
            Notes = "[Auto-generated from live data]",
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        db.DailyReports.Add(report);
        await db.SaveChangesAsync();
        _logger.LogInformation("Auto-generated daily summary for {Date}: {Registrations} regs, {GGR:C} GGR",
            yesterday, report.Registrations, report.GGR);
    }
}
