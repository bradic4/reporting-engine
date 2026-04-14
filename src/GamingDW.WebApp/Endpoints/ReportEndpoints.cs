using System.Security.Claims;
using ClosedXML.Excel;
using GamingDW.Core.Data;
using GamingDW.Core.Models;
using GamingDW.WebApp.Services;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/daily", async (string? from, string? to, IReportService svc) =>
        {
            var reports = await svc.GetDailyReportsAsync(from, to);
            return Results.Ok(reports);
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
            if (!ctx.User.HasClaim("CanEditReports", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var username = ctx.User.Identity?.Name ?? "unknown";
            var result = await svc.CreateReportAsync(body, username);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Id, result.Date });
        }).RequireAuthorization();

        app.MapPut("/api/reports/daily/{id:int}", async (int id, DailyReportRequest body, HttpContext ctx, IReportService svc) =>
        {
            if (!ctx.User.HasClaim("CanEditReports", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var result = await svc.UpdateReportAsync(id, body);
            return result.Error is not null
                ? Results.NotFound(new { error = result.Error })
                : Results.Ok(new { result.Id });
        }).RequireAuthorization();

        app.MapDelete("/api/reports/daily/{id:int}", async (int id, HttpContext ctx, IReportService svc) =>
        {
            if (!ctx.User.HasClaim("CanEditReports", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var success = await svc.DeleteReportAsync(id);
            return success ? Results.Ok() : Results.NotFound(new { error = "Report not found" });
        }).RequireAuthorization();

        // ── Excel Upload ──
        app.MapPost("/api/reports/upload", async (HttpContext ctx, IExcelImportService importSvc) =>
        {
            if (!ctx.User.HasClaim("CanEditReports", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var form = await ctx.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            var username = ctx.User.FindFirstValue(ClaimTypes.Name) ?? "system";

            var result = await importSvc.ImportAsync(file, username);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Imported, result.Skipped, result.Errors, result.Total });
        }).RequireAuthorization().DisableAntiforgery();

        // ── Compare two date ranges ──
        app.MapGet("/api/reports/compare", async (string from1, string to1, string from2, string to2, IReportService svc) =>
        {
            var result = await svc.ComparePeriodsAsync(from1, to1, from2, to2);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
