using System.Globalization;
using System.Text;
using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public interface IExportService
{
    Task<byte[]> ExportDailyReportsCsvAsync(string? from, string? to);
}

public class ExportService : IExportService
{
    private readonly GamingDbContext _db;

    public ExportService(GamingDbContext db) => _db = db;

    public async Task<byte[]> ExportDailyReportsCsvAsync(string? from, string? to)
    {
        var query = _db.DailyReports.AsQueryable();
        if (DateOnly.TryParse(from, out var fromDate))
            query = query.Where(r => r.Date >= fromDate);
        if (DateOnly.TryParse(to, out var toDate))
            query = query.Where(r => r.Date <= toDate);

        var data = await query.OrderByDescending(r => r.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Registrations,FTDs,Deposits,Withdrawals,GGR,ActivePlayers,Sessions,BonusCost,NetRevenue,Notes,CreatedBy,CreatedAt");
        foreach (var r in data)
        {
            sb.AppendLine(string.Join(",",
                r.Date.ToString("yyyy-MM-dd"),
                r.Registrations,
                r.FTDs,
                r.Deposits.ToString(CultureInfo.InvariantCulture),
                r.Withdrawals.ToString(CultureInfo.InvariantCulture),
                r.GGR.ToString(CultureInfo.InvariantCulture),
                r.ActivePlayers,
                r.Sessions,
                r.BonusCost.ToString(CultureInfo.InvariantCulture),
                r.NetRevenue.ToString(CultureInfo.InvariantCulture),
                $"\"{r.Notes?.Replace("\"", "\"\"")}\"",
                r.CreatedBy,
                r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
