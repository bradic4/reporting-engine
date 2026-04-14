using GamingDW.WebApp.Services;

namespace GamingDW.WebApp.Endpoints;

public static class LiveEndpoints
{
    public static void MapLiveEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/live/today", async (ILiveMetricsService svc) =>
        {
            var metrics = await svc.GetTodayMetricsAsync();
            return Results.Ok(metrics);
        }).RequireAuthorization();
    }
}
