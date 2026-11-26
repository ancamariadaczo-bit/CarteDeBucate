public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EmailVerifiedAtUtc { get; set; }
    public string? PreferredCulture { get; set; }
    public long SecurityVersion { get; set; } = 1;
}
