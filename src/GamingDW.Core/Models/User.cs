using System.ComponentModel.DataAnnotations;

namespace GamingDW.Core.Models;

public enum UserStatus
{
    Active,
    Banned
}

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; }

    [MaxLength(3)]
    public string Country { get; set; } = string.Empty;

    public UserStatus Status { get; set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<GameplayLog> GameplayLogs { get; set; } = new List<GameplayLog>();
    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
