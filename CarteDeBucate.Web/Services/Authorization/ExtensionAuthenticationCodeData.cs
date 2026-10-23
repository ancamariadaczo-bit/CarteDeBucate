
public class ExtensionAuthenticationCodeData
{
    public required string UserId { get; init; }

    public required string Username { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}