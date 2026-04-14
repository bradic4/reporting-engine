using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamingDW.Core.Models;

public enum GameType
{
    WheelOfFortune,
    BattleArena
}

public class GameplayLog
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public GameType GameType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BetAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WinAmount { get; set; }

    public DateTime Timestamp { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
