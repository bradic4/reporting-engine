using GamingDW.WebApp.Services;
using GamingDW.WebApp.Auth;

namespace GamingDW.WebApp.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/staff", async (HttpContext ctx, IStaffService svc) =>
        {
            if (!ctx.User.HasClaim("CanManageStaff", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var staff = await svc.GetAllStaffAsync();
            return Results.Ok(staff);
        }).RequireAuthorization();

        app.MapPost("/api/staff", async (HttpContext ctx, IStaffService svc) =>
        {
            if (!ctx.User.HasClaim("CanManageStaff", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var body = await ctx.Request.ReadFromJsonAsync<StaffRequest>();
            if (body == null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
                return Results.BadRequest(new { error = "Username and password are required" });

            var result = await svc.CreateStaffAsync(body);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Id, result.Username });
        }).RequireAuthorization();

        app.MapPut("/api/staff/{id:int}", async (int id, HttpContext ctx, IStaffService svc) =>
        {
            if (!ctx.User.HasClaim("CanManageStaff", "True"))
                return Results.Json(new { error = "Access denied" }, statusCode: 403);

            var body = await ctx.Request.ReadFromJsonAsync<StaffRequest>();
            if (body == null) return Results.BadRequest(new { error = "Invalid body" });

            var result = await svc.UpdateStaffAsync(id, body);
            return result.Error is not null
                ? Results.NotFound(new { error = result.Error })
                : Results.Ok(new { result.Id, result.Username });
        }).RequireAuthorization();

        // ── Stats ──
        app.MapGet("/api/stats", async (IStaffService svc) =>
        {
            var stats = await svc.GetStatsAsync();
            return Results.Ok(stats);
        }).RequireAuthorization();
    }
}
