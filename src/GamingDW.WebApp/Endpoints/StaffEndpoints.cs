using GamingDW.WebApp.Services;
using GamingDW.WebApp.Auth;

namespace GamingDW.WebApp.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/staff", async (HttpContext ctx, IStaffService svc) =>
        {
            var staff = await svc.GetAllStaffAsync();
            return Results.Ok(staff);
        }).RequireAuthorization("CanManageStaff");

        app.MapPost("/api/staff", async (HttpContext ctx, IStaffService svc) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<StaffRequest>();
            if (body == null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
                return Results.BadRequest(new { error = "Username and password are required" });

            var performedBy = ctx.User.Identity?.Name ?? "unknown";
            var result = await svc.CreateStaffAsync(body, performedBy);
            return result.Error is not null
                ? Results.BadRequest(new { error = result.Error })
                : Results.Ok(new { result.Id, result.Username });
        }).RequireAuthorization("CanManageStaff")
          .AddEndpointFilter<GamingDW.WebApp.Validation.ValidationFilter<StaffRequest>>();

        app.MapPut("/api/staff/{id:int}", async (int id, HttpContext ctx, IStaffService svc) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<StaffRequest>();
            if (body == null) return Results.BadRequest(new { error = "Invalid body" });

            var performedBy = ctx.User.Identity?.Name ?? "unknown";
            var result = await svc.UpdateStaffAsync(id, body, performedBy);
            return result.Error is not null
                ? Results.NotFound(new { error = result.Error })
                : Results.Ok(new { result.Id, result.Username });
        }).RequireAuthorization("CanManageStaff")
          .AddEndpointFilter<GamingDW.WebApp.Validation.ValidationFilter<StaffRequest>>();

        // ── Stats ──
        app.MapGet("/api/stats", async (IStaffService svc) =>
        {
            var stats = await svc.GetStatsAsync();
            return Results.Ok(stats);
        }).RequireAuthorization();
    }
}
