using System.Security.Claims;
using GamingDW.WebApp.Auth;
using GamingDW.WebApp.Services;

namespace GamingDW.WebApp.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/daily", async (string? from, string? to, int? page, int? pageSize,
            string? sortBy, bool? sortDesc, IReportService svc) =>
        {
            var p = Math.Max(page ?? 1, 1);
            var ps = Math.Clamp(pageSize ?? 50, 1, 200);
            var reports = await svc.GetDailyReportsAsync(from, to, p, ps, sortBy, sortDesc ?? true);
            var totalCount = await svc.GetDailyReportsCountAsync(from, to);
            var totalPages = (int)Math.Ceiling(totalCount / (double)ps);
            return Results.Ok(new { data = reports, meta = new { page = p, pageSize = ps, totalCount, totalPages } });
        }).RequireAuthorization();

        app.MapGet("/api/reports/daily/{date}", async (string date, IReportService svc) =>
        {
            var result = await svc.GetDailyReportByDateAsync(date);
            return result is null
                ? Results.NotFound(new { error = "No report for this date" })
                : Results.Ok(result);
        }).RequireAuthorization();

        app.MapPost("/api/reports/daily", async (DailyReportRequest body, HttpContext ctx, IReportService svc) =>
        {
            var username = ctx.User.Identity?.Name ?? "unknown";
            var result = await svc.CreateReportAsync(body, username);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Id, result.Date });
        }).RequireAuthorization("CanEditReports")
          .AddEndpointFilter<GamingDW.WebApp.Validation.ValidationFilter<DailyReportRequest>>();

        app.MapPut("/api/reports/daily/{id:int}", async (int id, DailyReportRequest body, HttpContext ctx, IReportService svc) =>
        {
            var username = ctx.User.Identity?.Name ?? "unknown";
            var result = await svc.UpdateReportAsync(id, body, username);
            return result.Error is not null
                ? Results.NotFound(new { error = result.Error })
                : Results.Ok(new { result.Id });
        }).RequireAuthorization("CanEditReports")
          .AddEndpointFilter<GamingDW.WebApp.Validation.ValidationFilter<DailyReportRequest>>();

        app.MapDelete("/api/reports/daily/{id:int}", async (int id, HttpContext ctx, IReportService svc) =>
        {
            var username = ctx.User.Identity?.Name ?? "unknown";
            var success = await svc.DeleteReportAsync(id, username);
            return success ? Results.Ok() : Results.NotFound(new { error = "Report not found" });
        }).RequireAuthorization("CanEditReports");

        // ── Excel Upload ──
        app.MapPost("/api/reports/upload", async (HttpContext ctx, IExcelImportService importSvc) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            var username = ctx.User.FindFirstValue(ClaimTypes.Name) ?? "system";

            var result = await importSvc.ImportAsync(file, username);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Imported, result.Skipped, result.Errors, result.Total });
        }).RequireAuthorization("CanEditReports").DisableAntiforgery();

        // ── Compare two date ranges ──
        app.MapGet("/api/reports/compare", async (string from1, string to1, string from2, string to2, IReportService svc) =>
        {
            var result = await svc.ComparePeriodsAsync(from1, to1, from2, to2);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
