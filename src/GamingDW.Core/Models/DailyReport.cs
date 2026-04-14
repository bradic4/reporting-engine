using System.ComponentModel.DataAnnotations;

namespace GamingDW.Core.Models;

/// <summary>
/// One row = one day's KPIs, entered by a staff member.
/// </summary>
public class DailyReport
{
    [Key]
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    // ── Core KPIs ──
    public int Registrations { get; set; }
    public int FTDs { get; set; }                // First-time depositors
    public decimal Deposits { get; set; }
    public decimal Withdrawals { get; set; }
    public decimal GGR { get; set; }             // Gross Gaming Revenue
    public int ActivePlayers { get; set; }
    public int Sessions { get; set; }
    public decimal BonusCost { get; set; }        // Marketing bonus spend

    // ── Derived ──
    public decimal NetRevenue => GGR - BonusCost;

    // ── Meta ──
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required, MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Soft Delete ──
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [MaxLength(50)]
    public string? DeletedBy { get; set; }
}
