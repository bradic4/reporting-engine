using GamingDW.WebApp.Services;

namespace GamingDW.WebApp.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/daily/export", async (string? from, string? to, string format, IExportService svc) =>
        {
            if (format?.ToLower() != "csv")
                return Results.BadRequest(new { error = "Supported formats: csv" });

            var bytes = await svc.ExportDailyReportsCsvAsync(from, to);
            return Results.File(bytes, "text/csv", $"daily_reports_{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization();
    }
}
