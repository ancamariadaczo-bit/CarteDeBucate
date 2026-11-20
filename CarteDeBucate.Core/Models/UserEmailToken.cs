public class UserEmailToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public EmailTokenPurpose Purpose { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public string TargetEmail { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
