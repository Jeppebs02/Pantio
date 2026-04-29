using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PantioAPI.Enums;

namespace PantioAPI.Entities;

[Table("store_connections")]
public class StoreConnection
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("chain")]
    public StoreChain Chain { get; set; }

    [Column("gigya_session_token")]
    public string? GigyaSessionToken { get; set; }

    [Column("access_token")]
    public string? AccessToken { get; set; }

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("id_token")]
    public string? IdToken { get; set; }

    [Column("token_expires_at")]
    public DateTime? TokenExpiresAt { get; set; }

    [Column("last_polled_at")]
    public DateTime? LastPolledAt { get; set; }

    [Column("connected_at")]
    public DateTime ConnectedAt { get; set; }

    [Column("disconnected_at")]
    public DateTime? DisconnectedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<Receipt> Receipts { get; set; } = [];
}
