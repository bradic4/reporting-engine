using ClosedXML.Excel;
using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GamingDW.WebApp.Services;

public record ImportResult(int Imported = 0, int Skipped = 0, int Total = 0,
    List<string>? Errors = null, string? Error = null);

public interface IExcelImportService
{
    Task<ImportResult> ImportAsync(IFormFile? file, string username);
}

public class ExcelImportService : IExcelImportService
{
    private readonly GamingDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(GamingDbContext db, IConfiguration config, ILogger<ExcelImportService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(IFormFile? file, string username)
    {
        if (file == null || file.Length == 0)
            return new ImportResult(Error: "No file uploaded");

        var maxSizeMB = _config.GetValue("UploadSettings:MaxFileSizeMB", 10);
        if (file.Length > maxSizeMB * 1024 * 1024)
            return new ImportResult(Error: $"File exceeds maximum size of {maxSizeMB}MB");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".xlsx" && ext != ".xls")
            return new ImportResult(Error: "Only Excel files (.xlsx, .xls) are supported");

        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);

        // Map header row
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (int c = 1; c <= lastCol; c++)
        {
            var header = ws.Cell(1, c).GetString().Trim();
            if (!string.IsNullOrEmpty(header)) headers[header] = c;
        }

        int? ColIdx(params string[] names) =>
            names.Select(n => headers.GetValueOrDefault(n, -1)).FirstOrDefault(i => i > 0) is int v && v > 0 ? v : null;

        var colDate = ColIdx("Date", "Datum", "Day");
        if (colDate == null)
            return new ImportResult(Error: "Excel must have a 'Date' column in the first row");

        var colReg = ColIdx("Registrations", "Reg", "Regs", "Registracije");
        var colFtd = ColIdx("FTD", "FTDs", "First Time Deposits");
        var colDep = ColIdx("Deposits", "Deposit", "Uplate");
        var colWd = ColIdx("Withdrawals", "Withdrawal", "WD", "Isplate");
        var colGgr = ColIdx("GGR", "Gross Gaming Revenue");
        var colActive = ColIdx("Active Players", "ActivePlayers", "Active", "Aktivni");
        var colSess = ColIdx("Sessions", "Session", "Sesije");
        var colBonus = ColIdx("Bonus Cost", "BonusCost", "Bonus");
        var colNotes = ColIdx("Notes", "Note", "Comment", "Napomena");

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var dateStr = ws.Cell(row, colDate.Value).GetString().Trim();
                if (string.IsNullOrEmpty(dateStr)) continue;

                DateOnly date;
                if (DateTime.TryParse(dateStr, out var dt))
                    date = DateOnly.FromDateTime(dt);
                else if (DateOnly.TryParse(dateStr, out var d2))
                    date = d2;
                else { errors.Add($"Row {row}: invalid date '{dateStr}'"); continue; }

                if (await _db.DailyReports.AnyAsync(r => r.Date == date))
                { skipped++; continue; }

                decimal GetDec(int? col) => col.HasValue ? (decimal.TryParse(ws.Cell(row, col.Value).GetString(), out var v) ? v : 0) : 0;
                int GetInt(int? col) => col.HasValue ? (int.TryParse(ws.Cell(row, col.Value).GetString(), out var v) ? v : 0) : 0;

                var report = new DailyReport
                {
                    Date = date,
                    Registrations = GetInt(colReg),
                    FTDs = GetInt(colFtd),
                    Deposits = GetDec(colDep),
                    Withdrawals = GetDec(colWd),
                    GGR = GetDec(colGgr),
                    ActivePlayers = GetInt(colActive),
                    Sessions = GetInt(colSess),
                    BonusCost = GetDec(colBonus),
                    Notes = colNotes.HasValue ? ws.Cell(row, colNotes.Value).GetString() : null,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                };
                _db.DailyReports.Add(report);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {row}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Excel import by {User}: {Imported} imported, {Skipped} skipped, {Errors} errors",
            username, imported, skipped, errors.Count);
        return new ImportResult(imported, skipped, imported + skipped, errors);
    }
}
