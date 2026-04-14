using System.ComponentModel.DataAnnotations;

namespace GamingDW.Core.Models;

/// <summary>
/// Tracks all mutations (create, update, delete) for compliance and debugging.
/// </summary>
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>Create, Update, Delete, Login, LoginFailed, Lockout</summary>
    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>DailyReport, KpiTarget, StaffUser, Auth</summary>
    [Required, MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Primary key of the affected entity (null for auth events).</summary>
    public int? EntityId { get; set; }

    /// <summary>Staff user who performed the action.</summary>
    public int? UserId { get; set; }

    [MaxLength(50)]
    public string? Username { get; set; }

    /// <summary>JSON snapshot of old values (for updates/deletes).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of new values (for creates/updates).</summary>
    public string? NewValues { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
