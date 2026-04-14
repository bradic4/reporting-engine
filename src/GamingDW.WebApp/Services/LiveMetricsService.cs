using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public interface ILiveMetricsService
{
    Task<object> GetTodayMetricsAsync();
}

public class LiveMetricsService : ILiveMetricsService
{
    private readonly GamingDbContext _db;

    public LiveMetricsService(GamingDbContext db) => _db = db;

    public async Task<object> GetTodayMetricsAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var sessions = await _db.UserSessions
            .CountAsync(s => s.LoginTime >= today && s.LoginTime < tomorrow);
        var activePlayers = await _db.UserSessions
            .Where(s => s.LoginTime >= today && s.LoginTime < tomorrow)
            .Select(s => s.UserId).Distinct().CountAsync();

        var deposits = await _db.Transactions
            .Where(t => t.Timestamp >= today && t.Timestamp < tomorrow && t.Type == TransactionType.Deposit)
            .SumAsync(t => t.Amount);
        var withdrawals = await _db.Transactions
            .Where(t => t.Timestamp >= today && t.Timestamp < tomorrow && t.Type == TransactionType.Withdrawal)
            .SumAsync(t => t.Amount);

        var bets = await _db.GameplayLogs
            .Where(g => g.Timestamp >= today && g.Timestamp < tomorrow)
            .SumAsync(g => g.BetAmount);
        var wins = await _db.GameplayLogs
            .Where(g => g.Timestamp >= today && g.Timestamp < tomorrow)
            .SumAsync(g => g.WinAmount);
        var plays = await _db.GameplayLogs
            .CountAsync(g => g.Timestamp >= today && g.Timestamp < tomorrow);

        return new
        {
            timestamp = DateTime.UtcNow ,
            sessions,
            activePlayers,
            deposits,
            withdrawals,
            bets,
            wins,
            ggr = bets - wins,
            plays
        };
    }
}
