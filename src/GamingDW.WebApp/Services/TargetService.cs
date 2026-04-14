using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public record TargetResult(int? Id = null, string? Error = null);

public interface ITargetService
{
    Task<IEnumerable<object>> GetTargetsAsync(string? period);
    Task<TargetResult> CreateTargetAsync(KpiTargetRequest body, string username);
    Task<TargetResult> UpdateTargetAsync(int id, KpiTargetRequest body);
    Task<bool> DeleteTargetAsync(int id);
    Task<object> GetProgressAsync(string? date);
}

public class TargetService : ITargetService
{
    private readonly GamingDbContext _db;
    private readonly ILogger<TargetService> _logger;

    public TargetService(GamingDbContext db, ILogger<TargetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetTargetsAsync(string? period)
    {
        var query = _db.KpiTargets.AsQueryable();
        if (!string.IsNullOrEmpty(period))
            query = query.Where(t => t.Period == period);
        var data = await query.OrderByDescending(t => t.PeriodStart).ToListAsync();
        return data.Select(t => new
        {
            t.Id, t.Period, periodStart = t.PeriodStart.ToString("yyyy-MM-dd"),
            t.MetricName, t.TargetValue, t.CreatedBy, t.CreatedAt
        });
    }

    public async Task<TargetResult> CreateTargetAsync(KpiTargetRequest body, string username)
    {
        if (!DateOnly.TryParse(body.PeriodStart, out var start))
            return new TargetResult(Error: "Invalid date");

        var target = new KpiTarget
        {
            Period = body.Period,
            PeriodStart = start,
            MetricName = body.MetricName,
            TargetValue = body.TargetValue,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        _db.KpiTargets.Add(target);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Target created: {Metric} = {Value} ({Period}, {Date}) by {User}",
            body.MetricName, body.TargetValue, body.Period, start, username);
        return new TargetResult(target.Id);
    }

    public async Task<TargetResult> UpdateTargetAsync(int id, KpiTargetRequest body)
    {
        var target = await _db.KpiTargets.FindAsync(id);
        if (target == null) return new TargetResult(Error: "Target not found");

        target.Period = body.Period;
        if (DateOnly.TryParse(body.PeriodStart, out var start)) target.PeriodStart = start;
        target.MetricName = body.MetricName;
        target.TargetValue = body.TargetValue;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Target {Id} updated", id);
        return new TargetResult(target.Id);
    }

    public async Task<bool> DeleteTargetAsync(int id)
    {
        var target = await _db.KpiTargets.FindAsync(id);
        if (target == null) return false;
        _db.KpiTargets.Remove(target);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Target {Id} deleted", id);
        return true;
    }

    public async Task<object> GetProgressAsync(string? date)
    {
        var d = DateOnly.TryParse(date, out var parsed) ? parsed : DateOnly.FromDateTime(DateTime.Today);
        var report = await _db.DailyReports.FirstOrDefaultAsync(r => r.Date == d);
        var targets = await _db.KpiTargets.Where(t => t.Period == "daily" && t.PeriodStart == d).ToListAsync();

        var progress = targets.Select(t =>
        {
            decimal actual = t.MetricName switch
            {
                "Registrations" => report?.Registrations ?? 0,
                "FTDs" => report?.FTDs ?? 0,
                "Deposits" => report?.Deposits ?? 0,
                "Withdrawals" => report?.Withdrawals ?? 0,
                "GGR" => report?.GGR ?? 0,
                "ActivePlayers" => report?.ActivePlayers ?? 0,
                "Sessions" => report?.Sessions ?? 0,
                "BonusCost" => report?.BonusCost ?? 0,
                "NetRevenue" => report?.NetRevenue ?? 0,
                _ => 0
            };
            var pct = t.TargetValue != 0 ? Math.Round(actual / t.TargetValue * 100, 1) : 0;
            return new { t.MetricName, target = t.TargetValue, actual, progressPct = pct };
        });

        return new { date = d.ToString("yyyy-MM-dd"), progress };
    }
}
