using System.ComponentModel.DataAnnotations;

namespace GamingDW.Core.Models;

/// <summary>
/// A KPI target for a specific metric and period.
/// </summary>
public class KpiTarget
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Period { get; set; } = "daily";  // "daily", "weekly", "monthly"

    public DateOnly PeriodStart { get; set; }

    [Required, MaxLength(50)]
    public string MetricName { get; set; } = string.Empty;  // e.g. "Registrations", "GGR"

    public decimal TargetValue { get; set; }

    [Required, MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Soft Delete ──
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [MaxLength(50)]
    public string? DeletedBy { get; set; }
}
