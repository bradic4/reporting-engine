using GamingDW.WebApp.Services;

namespace GamingDW.WebApp.Endpoints;

public static class TargetEndpoints
{
    public static void MapTargetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/targets", async (string? period, ITargetService svc) =>
        {
            var data = await svc.GetTargetsAsync(period);
            return Results.Ok(data);
        }).RequireAuthorization();

        app.MapPost("/api/targets", async (KpiTargetRequest body, HttpContext ctx, ITargetService svc) =>
        {
            if (!ctx.User.HasClaim("CanSetTargets", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var username = ctx.User.Identity?.Name ?? "unknown";
            var result = await svc.CreateTargetAsync(body, username);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Id });
        }).RequireAuthorization();

        app.MapPut("/api/targets/{id:int}", async (int id, KpiTargetRequest body, HttpContext ctx, ITargetService svc) =>
        {
            if (!ctx.User.HasClaim("CanSetTargets", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var result = await svc.UpdateTargetAsync(id, body);
            return result.Error is not null
                ? Results.NotFound(new { error = result.Error })
                : Results.Ok(new { result.Id });
        }).RequireAuthorization();

        app.MapDelete("/api/targets/{id:int}", async (int id, HttpContext ctx, ITargetService svc) =>
        {
            if (!ctx.User.HasClaim("CanSetTargets", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var success = await svc.DeleteTargetAsync(id);
            return success ? Results.Ok() : Results.NotFound(new { error = "Target not found" });
        }).RequireAuthorization();

        app.MapGet("/api/targets/progress", async (string? date, ITargetService svc) =>
        {
            var result = await svc.GetProgressAsync(date);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
