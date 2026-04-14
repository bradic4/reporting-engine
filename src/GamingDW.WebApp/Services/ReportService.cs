using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public record ReportResult(int? Id = null, string? Date = null, string? Error = null);

public interface IReportService
{
    Task<IEnumerable<DailyReportDto>> GetDailyReportsAsync(string? from, string? to, int page = 1, int pageSize = 50, string? sortBy = null, bool sortDesc = true);
    Task<int> GetDailyReportsCountAsync(string? from, string? to);
    Task<DailyReportDto?> GetDailyReportByDateAsync(string date);
    Task<ReportResult> CreateReportAsync(DailyReportRequest body, string username);
    Task<ReportResult> UpdateReportAsync(int id, DailyReportRequest body, string username);
    Task<bool> DeleteReportAsync(int id, string username);
    Task<ComparePeriodsDto> ComparePeriodsAsync(string from1, string to1, string from2, string to2);
}

public class ReportService : IReportService
{
    private readonly GamingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<ReportService> _logger;

    public ReportService(GamingDbContext db, IAuditService audit, ILogger<ReportService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IEnumerable<DailyReportDto>> GetDailyReportsAsync(string? from, string? to, int page = 1, int pageSize = 50, string? sortBy = null, bool sortDesc = true)
    {
        var query = _db.DailyReports.AsQueryable();
        if (DateOnly.TryParse(from, out var fromDate))
            query = query.Where(r => r.Date >= fromDate);
        if (DateOnly.TryParse(to, out var toDate))
            query = query.Where(r => r.Date <= toDate);

        query = sortBy?.ToLower() switch
        {
            "registrations" => sortDesc ? query.OrderByDescending(r => r.Registrations) : query.OrderBy(r => r.Registrations),
            "ftds" => sortDesc ? query.OrderByDescending(r => r.FTDs) : query.OrderBy(r => r.FTDs),
            "deposits" => sortDesc ? query.OrderByDescending(r => r.Deposits) : query.OrderBy(r => r.Deposits),
            "ggr" => sortDesc ? query.OrderByDescending(r => r.GGR) : query.OrderBy(r => r.GGR),
            "activeplayers" => sortDesc ? query.OrderByDescending(r => r.ActivePlayers) : query.OrderBy(r => r.ActivePlayers),
            "sessions" => sortDesc ? query.OrderByDescending(r => r.Sessions) : query.OrderBy(r => r.Sessions),
            _ => sortDesc ? query.OrderByDescending(r => r.Date) : query.OrderBy(r => r.Date),
        };

        var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return data.Select(MapToDto);
    }

    public async Task<int> GetDailyReportsCountAsync(string? from, string? to)
    {
        var query = _db.DailyReports.AsQueryable();
        if (DateOnly.TryParse(from, out var fromDate))
            query = query.Where(r => r.Date >= fromDate);
        if (DateOnly.TryParse(to, out var toDate))
            query = query.Where(r => r.Date <= toDate);
        return await query.CountAsync();
    }

    public async Task<DailyReportDto?> GetDailyReportByDateAsync(string date)
    {
        if (!DateOnly.TryParse(date, out var d)) return null;
        var report = await _db.DailyReports.FirstOrDefaultAsync(r => r.Date == d);
        return report == null ? null : MapToDto(report);
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

        await _audit.LogAsync("Create", "DailyReport", report.Id, null, username,
            newValues: new { report.Date, report.Registrations, report.FTDs, report.Deposits, report.Withdrawals, report.GGR, report.ActivePlayers, report.Sessions, report.BonusCost, report.Notes });

        _logger.LogInformation("Report created for {Date} by {User}", date, username);
        return new ReportResult(report.Id, report.Date.ToString("yyyy-MM-dd"));
    }

    public async Task<ReportResult> UpdateReportAsync(int id, DailyReportRequest body, string username)
    {
        var report = await _db.DailyReports.FindAsync(id);
        if (report == null) return new ReportResult(Error: "Report not found");

        var oldValues = new { report.Date, report.Registrations, report.FTDs, report.Deposits, report.Withdrawals, report.GGR, report.ActivePlayers, report.Sessions, report.BonusCost, report.Notes };

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

        await _audit.LogAsync("Update", "DailyReport", report.Id, null, username,
            oldValues: oldValues,
            newValues: new { report.Date, report.Registrations, report.FTDs, report.Deposits, report.Withdrawals, report.GGR, report.ActivePlayers, report.Sessions, report.BonusCost, report.Notes });

        _logger.LogInformation("Report {Id} updated by {User}", id, username);
        return new ReportResult(report.Id);
    }

    public async Task<bool> DeleteReportAsync(int id, string username)
    {
        var report = await _db.DailyReports.FindAsync(id);
        if (report == null) return false;

        var oldValues = new { report.Date, report.Registrations, report.FTDs, report.Deposits, report.Withdrawals, report.GGR };

        // Soft delete instead of hard delete
        report.IsDeleted = true;
        report.DeletedAt = DateTime.UtcNow;
        report.DeletedBy = username;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Delete", "DailyReport", report.Id, null, username, oldValues: oldValues);

        _logger.LogInformation("Report {Id} soft-deleted by {User} (date: {Date})", id, username, report.Date);
        return true;
    }

    public async Task<ComparePeriodsDto> ComparePeriodsAsync(string from1, string to1, string from2, string to2)
    {
        DateOnly.TryParse(from1, out var f1);
        DateOnly.TryParse(to1, out var t1);
        DateOnly.TryParse(from2, out var f2);
        DateOnly.TryParse(to2, out var t2);

        var range1 = await _db.DailyReports.Where(r => r.Date >= f1 && r.Date <= t1).ToListAsync();
        var range2 = await _db.DailyReports.Where(r => r.Date >= f2 && r.Date <= t2).ToListAsync();

        return new ComparePeriodsDto(Aggregate(range1), Aggregate(range2));
    }

    private static ReportSummaryDto Aggregate(List<DailyReport> list) => new(
        Days: list.Count,
        Registrations: list.Sum(r => r.Registrations),
        FTDs: list.Sum(r => r.FTDs),
        Deposits: list.Sum(r => r.Deposits),
        Withdrawals: list.Sum(r => r.Withdrawals),
        GGR: list.Sum(r => r.GGR),
        ActivePlayers: list.Count > 0 ? (int)list.Average(r => r.ActivePlayers) : 0,
        Sessions: list.Sum(r => r.Sessions),
        BonusCost: list.Sum(r => r.BonusCost),
        NetRevenue: list.Sum(r => r.GGR - r.BonusCost)
    );

    private static DailyReportDto MapToDto(DailyReport r) => new(
        Id: r.Id,
        Date: r.Date.ToString("yyyy-MM-dd"),
        Registrations: r.Registrations,
        FTDs: r.FTDs,
        Deposits: r.Deposits,
        Withdrawals: r.Withdrawals,
        GGR: r.GGR,
        ActivePlayers: r.ActivePlayers,
        Sessions: r.Sessions,
        BonusCost: r.BonusCost,
        NetRevenue: r.NetRevenue,
        Notes: r.Notes,
        CreatedBy: r.CreatedBy,
        CreatedAt: r.CreatedAt,
        UpdatedAt: r.UpdatedAt
    );
}
