using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PantioClassLibrary.Entities;

[Table("user_profiles")]
public class UserProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("household_size")]
    public int? HouseholdSize { get; set; }

    [Column("locale")]
    public string? Locale { get; set; }

    [Column("notification_prefs", TypeName = "jsonb")]
    public string? NotificationPrefs { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
