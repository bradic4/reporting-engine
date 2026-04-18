using System.Security.Claims;
using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GamingDW.WebApp.Auth;

/// <summary>
/// Configuration for admin bootstrap and lockout policy.
/// </summary>
public class AdminSettings
{
    public string DefaultPassword { get; set; } = string.Empty;
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}

public class AuthService
{
    private readonly GamingDbContext _db;
    private readonly PasswordHasher<StaffUser> _hasher;
    private readonly AdminSettings _adminSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        GamingDbContext db,
        IOptions<AdminSettings> adminSettings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _hasher = new PasswordHasher<StaffUser>();
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the admin user on first run. Password comes from configuration/env var.
    /// </summary>
    public async Task SeedAdminAsync()
    {
        var password = string.IsNullOrEmpty(_adminSettings.DefaultPassword) ? "admin" : _adminSettings.DefaultPassword;
        var existingAdmin = await _db.StaffUsers.FirstOrDefaultAsync(u => u.Username == "admin");

        if (existingAdmin != null)
        {
            existingAdmin.PasswordHash = _hasher.HashPassword(existingAdmin, password);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin user password reset to 'admin'");
            return;
        }

        var admin = new StaffUser
        {
            Username = "admin",
            Title = "System Administrator",
            CanViewReports = true,
            CanEditReports = true,
            CanSetTargets = true,
            CanViewLive = true,
            CanManageStaff = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = _hasher.HashPassword(admin, password);

        _db.StaffUsers.Add(admin);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin user seeded successfully");
    }

    /// <summary>
    /// Validates credentials with lockout protection.
    /// </summary>
    public async Task<StaffUser?> ValidateAsync(string username, string password, string? ipAddress = null)
    {
        var user = await _db.StaffUsers.FirstOrDefaultAsync(
            u => u.Username == username && u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found (IP: {IP})", username, ipAddress);
            return null;
        }

        // Check lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            var remaining = (user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            _logger.LogWarning("Login rejected: user '{Username}' is locked out for {Minutes:F0} more minutes (IP: {IP})",
                username, remaining, ipAddress);
            return null;
        }

        // Verify password
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            _logger.LogWarning("Login failed: invalid password for '{Username}' (attempt {Attempt}/{Max}, IP: {IP})",
                username, user.FailedLoginAttempts, _adminSettings.MaxFailedAttempts, ipAddress);

            if (user.FailedLoginAttempts >= _adminSettings.MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(_adminSettings.LockoutMinutes);
                _logger.LogWarning("Account LOCKED: user '{Username}' locked until {LockoutEnd} after {Attempts} failed attempts",
                    username, user.LockoutEnd, user.FailedLoginAttempts);
            }

            await _db.SaveChangesAsync();
            return null;
        }

        // Successful login — reset counters
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Re-hash if the algorithm was upgraded (PasswordHasher handles this)
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Password rehashed for user '{Username}' due to algorithm upgrade", username);
        }

        _logger.LogInformation("Login successful: user '{Username}' (IP: {IP})", username, ipAddress);
        return user;
    }

    public ClaimsPrincipal CreatePrincipal(StaffUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("Title", user.Title),
            new("CanViewReports", user.CanViewReports.ToString()),
            new("CanEditReports", user.CanEditReports.ToString()),
            new("CanSetTargets", user.CanSetTargets.ToString()),
            new("CanViewLive", user.CanViewLive.ToString()),
            new("CanManageStaff", user.CanManageStaff.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Hashes a password using ASP.NET Identity's PasswordHasher (PBKDF2).
    /// </summary>
    public string HashPassword(string password)
    {
        var dummy = new StaffUser();
        return _hasher.HashPassword(dummy, password);
    }
}
