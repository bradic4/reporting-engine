using GamingDW.WebApp.Services;

namespace GamingDW.WebApp.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit", async (string? entityType, int? entityId, int page, int pageSize, HttpContext ctx, IAuditService svc) =>
        {
            if (!ctx.User.HasClaim("CanManageStaff", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var data = await svc.GetAuditLogsAsync(entityType, entityId, page, pageSize);
            return Results.Ok(new { data, meta = new { page, pageSize } });
        }).RequireAuthorization();
    }
}
