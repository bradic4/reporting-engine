using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using GamingDW.WebApp.Auth;

namespace GamingDW.WebApp.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<LoginRequest>();
            if (body == null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
                return Results.BadRequest(new { error = "Username and password are required" });

            var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();
            var user = await auth.ValidateAsync(body.Username, body.Password, ipAddress);
            if (user == null)
                return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);

            var principal = auth.CreatePrincipal(user);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Ok(new { user.Username, user.Title });
        }).RequireRateLimiting("login");

        app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        });

        app.MapGet("/api/auth/me", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Json(new { error = "Not authenticated" }, statusCode: 401);

            return Results.Ok(new
            {
                username = ctx.User.Identity.Name,
                title = ctx.User.FindFirst("Title")?.Value ?? "",
                permissions = new
                {
                    viewReports = ctx.User.HasClaim("CanViewReports", "True"),
                    editReports = ctx.User.HasClaim("CanEditReports", "True"),
                    setTargets = ctx.User.HasClaim("CanSetTargets", "True"),
                    viewLive = ctx.User.HasClaim("CanViewLive", "True"),
                    manageStaff = ctx.User.HasClaim("CanManageStaff", "True"),
                }
            });
        });
    }
}
