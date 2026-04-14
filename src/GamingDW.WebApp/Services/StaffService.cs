using GamingDW.Core.Data;
using GamingDW.Core.Models;
using GamingDW.WebApp.Auth;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public record StaffResult(int? Id = null, string? Username = null, string? Error = null);

public interface IStaffService
{
    Task<IEnumerable<object>> GetAllStaffAsync();
    Task<StaffResult> CreateStaffAsync(StaffRequest body);
    Task<StaffResult> UpdateStaffAsync(int id, StaffRequest body);
    Task<object> GetStatsAsync();
}

public class StaffService : IStaffService
{
    private readonly GamingDbContext _db;
    private readonly AuthService _auth;
    private readonly ILogger<StaffService> _logger;

    public StaffService(GamingDbContext db, AuthService auth, ILogger<StaffService> logger)
    {
        _db = db;
        _auth = auth;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetAllStaffAsync()
    {
        return await _db.StaffUsers
            .Select(s => new
            {
                s.Id, s.Username, s.Title, s.IsActive, s.CreatedAt,
                s.CanViewReports, s.CanEditReports, s.CanSetTargets,
                s.CanViewLive, s.CanManageStaff
            })
            .ToListAsync();
    }

    public async Task<StaffResult> CreateStaffAsync(StaffRequest body)
    {
        if (string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
            return new StaffResult(Error: "Username and password are required");
        if (await _db.StaffUsers.AnyAsync(u => u.Username == body.Username))
            return new StaffResult(Error: "Username already exists");

        var user = new StaffUser
        {
            Username = body.Username,
            PasswordHash = _auth.HashPassword(body.Password),
            Title = body.Title ?? "Analyst",
            CanViewReports = body.CanViewReports,
            CanEditReports = body.CanEditReports,
            CanSetTargets = body.CanSetTargets,
            CanViewLive = body.CanViewLive,
            CanManageStaff = body.CanManageStaff,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.StaffUsers.Add(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Staff user '{Username}' created", user.Username);
        return new StaffResult(user.Id, user.Username);
    }

    public async Task<StaffResult> UpdateStaffAsync(int id, StaffRequest body)
    {
        var user = await _db.StaffUsers.FindAsync(id);
        if (user == null) return new StaffResult(Error: "User not found");

        if (!string.IsNullOrEmpty(body.Username)) user.Username = body.Username;
        if (!string.IsNullOrEmpty(body.Password)) user.PasswordHash = _auth.HashPassword(body.Password);
        if (!string.IsNullOrEmpty(body.Title)) user.Title = body.Title;
        user.CanViewReports = body.CanViewReports;
        user.CanEditReports = body.CanEditReports;
        user.CanSetTargets = body.CanSetTargets;
        user.CanViewLive = body.CanViewLive;
        user.CanManageStaff = body.CanManageStaff;
        user.IsActive = body.IsActive;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Staff user '{Username}' updated", user.Username);
        return new StaffResult(user.Id, user.Username);
    }

    public async Task<object> GetStatsAsync()
    {
        var reports = await _db.DailyReports.CountAsync();
        var targets = await _db.KpiTargets.CountAsync();
        var staff = await _db.StaffUsers.CountAsync();
        return new { reports, targets, staff };
    }
}
