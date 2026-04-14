using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public record TargetResult(int? Id = null, string? Error = null);

public interface ITargetService
{
    Task<IEnumerable<KpiTargetDto>> GetTargetsAsync(string? period);
    Task<TargetResult> CreateTargetAsync(KpiTargetRequest body, string username);
    Task<TargetResult> UpdateTargetAsync(int id, KpiTargetRequest body, string username);
    Task<bool> DeleteTargetAsync(int id, string username);
    Task<TargetProgressDto> GetProgressAsync(string? date);
}

public class TargetService : ITargetService
{
    private readonly GamingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<TargetService> _logger;

    public TargetService(GamingDbContext db, IAuditService audit, ILogger<TargetService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IEnumerable<KpiTargetDto>> GetTargetsAsync(string? period)
    {
        var query = _db.KpiTargets.AsQueryable();
        if (!string.IsNullOrEmpty(period))
            query = query.Where(t => t.Period == period);
        var data = await query.OrderByDescending(t => t.PeriodStart).ToListAsync();
        return data.Select(t => new KpiTargetDto(
            t.Id, t.Period, t.PeriodStart.ToString("yyyy-MM-dd"),
            t.MetricName, t.TargetValue, t.CreatedBy, t.CreatedAt
        ));
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

        await _audit.LogAsync("Create", "KpiTarget", target.Id, null, username,
            newValues: new { target.Period, target.PeriodStart, target.MetricName, target.TargetValue });

        _logger.LogInformation("Target created: {Metric} = {Value} ({Period}, {Date}) by {User}",
            body.MetricName, body.TargetValue, body.Period, start, username);
        return new TargetResult(target.Id);
    }

    public async Task<TargetResult> UpdateTargetAsync(int id, KpiTargetRequest body, string username)
    {
        var target = await _db.KpiTargets.FindAsync(id);
        if (target == null) return new TargetResult(Error: "Target not found");

        var oldValues = new { target.Period, target.PeriodStart, target.MetricName, target.TargetValue };

        target.Period = body.Period;
        if (DateOnly.TryParse(body.PeriodStart, out var start)) target.PeriodStart = start;
        target.MetricName = body.MetricName;
        target.TargetValue = body.TargetValue;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Update", "KpiTarget", target.Id, null, username,
            oldValues: oldValues,
            newValues: new { target.Period, target.PeriodStart, target.MetricName, target.TargetValue });

        _logger.LogInformation("Target {Id} updated by {User}", id, username);
        return new TargetResult(target.Id);
    }

    public async Task<bool> DeleteTargetAsync(int id, string username)
    {
        var target = await _db.KpiTargets.FindAsync(id);
        if (target == null) return false;

        var oldValues = new { target.Period, target.PeriodStart, target.MetricName, target.TargetValue };

        // Soft delete instead of hard delete
        target.IsDeleted = true;
        target.DeletedAt = DateTime.UtcNow;
        target.DeletedBy = username;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Delete", "KpiTarget", target.Id, null, username, oldValues: oldValues);

        _logger.LogInformation("Target {Id} soft-deleted by {User}", id, username);
        return true;
    }

    public async Task<TargetProgressDto> GetProgressAsync(string? date)
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
            return new TargetProgressItemDto(t.MetricName, t.TargetValue, actual, pct);
        });

        return new TargetProgressDto(d.ToString("yyyy-MM-dd"), progress);
    }
}
