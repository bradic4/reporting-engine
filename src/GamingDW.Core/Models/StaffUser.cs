using System.ComponentModel.DataAnnotations;

namespace GamingDW.Core.Models;

/// <summary>
/// Represents a staff user who can log into the reporting dashboard.
/// Separate from gaming Users — these are employees/admins.
/// </summary>
public class StaffUser
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    // ── Permission flags ──
    public bool CanViewReports { get; set; }
    public bool CanEditReports { get; set; }
    public bool CanSetTargets { get; set; }
    public bool CanViewLive { get; set; }
    public bool CanManageStaff { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Security ──
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
