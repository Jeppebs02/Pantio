using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PantioAPI.Enums;

namespace PantioAPI.Entities;

[Table("expiry_notifications")]
public class ExpiryNotification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("expiry_date_id")]
    public Guid ExpiryDateId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("days_before_expiry")]
    public int DaysBeforeExpiry { get; set; }

    [Column("channel")]
    public NotificationChannel Channel { get; set; }

    [Column("sent_at")]
    public DateTime SentAt { get; set; }

    [Column("acknowledged")]
    public bool Acknowledged { get; set; }

    [ForeignKey(nameof(ExpiryDateId))]
    public ExpiryDate ExpiryDate { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
