using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamingDW.Core.Models;

public class UserSession
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime LoginTime { get; set; }

    public DateTime LogoutTime { get; set; }

    [MaxLength(30)]
    public string Device { get; set; } = string.Empty;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
