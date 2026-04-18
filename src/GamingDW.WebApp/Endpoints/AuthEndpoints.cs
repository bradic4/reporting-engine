using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using GamingDW.WebApp.Auth;
using GamingDW.WebApp.Services;
using System.Security.Claims;

namespace GamingDW.WebApp.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth, IAuditService audit) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<LoginRequest>();
            if (body == null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
                return Results.BadRequest(new { error = "Username and password are required" });

            var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();
            var user = await auth.ValidateAsync(body.Username, body.Password, ipAddress);

            if (user == null)
            {
                await audit.LogAsync("LoginFailed", "Auth", null, null, body.Username, ipAddress: ipAddress);
                return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
            }

            var principal = auth.CreatePrincipal(user);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            await audit.LogAsync("Login", "Auth", user.Id, user.Id, user.Username, ipAddress: ipAddress);

            return Results.Ok(new { user.Username, user.Title });
        }).RequireRateLimiting("login");

        app.MapPost("/api/auth/logout", async (HttpContext ctx, IAuditService audit) =>
        {
            var username = ctx.User.Identity?.Name;
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();

            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (username != null)
                await audit.LogAsync("Logout", "Auth", null, int.TryParse(userId, out var id) ? id : null, username, ipAddress: ipAddress);

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
                    viewReports = ctx.User.HasClaim("CanViewReports", "True") || ctx.User.HasClaim("CanViewReports", "true"),
                    editReports = ctx.User.HasClaim("CanEditReports", "True") || ctx.User.HasClaim("CanEditReports", "true"),
                    setTargets = ctx.User.HasClaim("CanSetTargets", "True") || ctx.User.HasClaim("CanSetTargets", "true"),
                    viewLive = ctx.User.HasClaim("CanViewLive", "True") || ctx.User.HasClaim("CanViewLive", "true"),
                    manageStaff = ctx.User.HasClaim("CanManageStaff", "True") || ctx.User.HasClaim("CanManageStaff", "true"),
                }
            });
        });
    }
}
