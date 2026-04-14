using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public record ReportResult(int? Id = null, string? Date = null, string? Error = null);

public interface IReportService
{
    Task<IEnumerable<object>> GetDailyReportsAsync(string? from, string? to);
    Task<object?> GetDailyReportByDateAsync(string date);
    Task<ReportResult> CreateReportAsync(DailyReportRequest body, string username);
    Task<ReportResult> UpdateReportAsync(int id, DailyReportRequest body);
    Task<bool> DeleteReportAsync(int id);
    Task<object> ComparePeriodsAsync(string from1, string to1, string from2, string to2);
}

public class ReportService : IReportService
{
    private readonly GamingDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(GamingDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetDailyReportsAsync(string? from, string? to)
    {
        var query = _db.DailyReports.AsQueryable();
        if (DateOnly.TryParse(from, out var fromDate))
            query = query.Where(r => r.Date >= fromDate);
        if (DateOnly.TryParse(to, out var toDate))
            query = query.Where(r => r.Date <= toDate);

        var data = await query.OrderByDescending(r => r.Date).ToListAsync();
        return data.Select(r => new
        {
            r.Id,
            date = r.Date.ToString("yyyy-MM-dd"),
            r.Registrations, r.FTDs, r.Deposits, r.Withdrawals,
            r.GGR, r.ActivePlayers, r.Sessions, r.BonusCost,
            netRevenue = r.NetRevenue, r.Notes, r.CreatedBy,
            createdAt = r.CreatedAt, updatedAt = r.UpdatedAt
        });
    }

    public async Task<object?> GetDailyReportByDateAsync(string date)
    {
        if (!DateOnly.TryParse(date, out var d)) return null;
        var report = await _db.DailyReports.FirstOrDefaultAsync(r => r.Date == d);
        if (report == null) return null;
        return new
        {
            report.Id,
            date = report.Date.ToString("yyyy-MM-dd"),
            report.Registrations, report.FTDs, report.Deposits, report.Withdrawals,
            report.GGR, report.ActivePlayers, report.Sessions, report.BonusCost,
            netRevenue = report.NetRevenue, report.Notes, report.CreatedBy,
            createdAt = report.CreatedAt, updatedAt = report.UpdatedAt
        };
    }

    public async Task<ReportResult> CreateReportAsync(DailyReportRequest body, string username)
    {
        if (!DateOnly.TryParse(body.Date, out var date))
            return new ReportResult(Error: "Invalid date");
        if (await _db.DailyReports.AnyAsync(r => r.Date == date))
            return new ReportResult(Error: "Report already exists for this date");

        var report = new DailyReport
        {
            Date = date,
            Registrations = body.Registrations,
            FTDs = body.FTDs,
            Deposits = body.Deposits,
            Withdrawals = body.Withdrawals,
            GGR = body.GGR,
            ActivePlayers = body.ActivePlayers,
            Sessions = body.Sessions,
            BonusCost = body.BonusCost,
            Notes = body.Notes,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        _db.DailyReports.Add(report);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Report created for {Date} by {User}", date, username);
        return new ReportResult(report.Id, report.Date.ToString("yyyy-MM-dd"));
    }

    public async Task<ReportResult> UpdateReportAsync(int id, DailyReportRequest body)
    {
        var report = await _db.DailyReports.FindAsync(id);
        if (report == null) return new ReportResult(Error: "Report not found");

        if (DateOnly.TryParse(body.Date, out var date)) report.Date = date;
        report.Registrations = body.Registrations;
        report.FTDs = body.FTDs;
        report.Deposits = body.Deposits;
        report.Withdrawals = body.Withdrawals;
        report.GGR = body.GGR;
        report.ActivePlayers = body.ActivePlayers;
        report.Sessions = body.Sessions;
        report.BonusCost = body.BonusCost;
        report.Notes = body.Notes;
        report.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Report {Id} updated", id);
        return new ReportResult(report.Id);
    }

    public async Task<bool> DeleteReportAsync(int id)
    {
        var report = await _db.DailyReports.FindAsync(id);
        if (report == null) return false;
        _db.DailyReports.Remove(report);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Report {Id} deleted (date: {Date})", id, report.Date);
        return true;
    }

    public async Task<object> ComparePeriodsAsync(string from1, string to1, string from2, string to2)
    {
        DateOnly.TryParse(from1, out var f1);
        DateOnly.TryParse(to1, out var t1);
        DateOnly.TryParse(from2, out var f2);
        DateOnly.TryParse(to2, out var t2);

        var range1 = await _db.DailyReports.Where(r => r.Date >= f1 && r.Date <= t1).ToListAsync();
        var range2 = await _db.DailyReports.Where(r => r.Date >= f2 && r.Date <= t2).ToListAsync();

        object Aggregate(List<DailyReport> list) => new
        {
            days = list.Count,
            registrations = list.Sum(r => r.Registrations),
            ftds = list.Sum(r => r.FTDs),
            deposits = list.Sum(r => r.Deposits),
            withdrawals = list.Sum(r => r.Withdrawals),
            ggr = list.Sum(r => r.GGR),
            activePlayers = list.Count > 0 ? (int)list.Average(r => r.ActivePlayers) : 0,
            sessions = list.Sum(r => r.Sessions),
            bonusCost = list.Sum(r => r.BonusCost),
            netRevenue = list.Sum(r => r.GGR - r.BonusCost)
        };

        return new { period1 = Aggregate(range1), period2 = Aggregate(range2) };
    }
}
