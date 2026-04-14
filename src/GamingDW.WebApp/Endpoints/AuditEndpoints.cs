using GamingDW.WebApp.Services;

namespace GamingDW.WebApp.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit", async (string? entityType, int? entityId, string? username,
            string? from, string? to, int? page, int? pageSize, HttpContext ctx, IAuditService svc) =>
        {
            var p = Math.Max(page ?? 1, 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var result = await svc.GetAuditLogsAsync(entityType, entityId, username, from, to, p, ps);
            return Results.Ok(result);
        }).RequireAuthorization("CanManageStaff");
    }
}
