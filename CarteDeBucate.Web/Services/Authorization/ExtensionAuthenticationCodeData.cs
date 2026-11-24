
public class ExtensionAuthenticationCodeData
{
    public required int UserId { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
