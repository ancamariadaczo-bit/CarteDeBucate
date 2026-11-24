public sealed class AccountLinkBuilder : IAccountLinkBuilder
{
    private readonly Uri _publicBaseUri;

    public AccountLinkBuilder(ApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _publicBaseUri = EmailConfigurationValidator.ParsePublicBaseUri(options.PublicBaseUrl);
    }

    public string BuildConfirmationUrl(string relativePath, string token)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith("/", StringComparison.Ordinal) ||
            relativePath.StartsWith("//", StringComparison.Ordinal) ||
            relativePath.Contains('?') ||
            relativePath.Contains('#') ||
            relativePath.Contains('\\'))
        {
            throw new ArgumentException(
                "The confirmation path must be an absolute application path without a query or fragment.",
                nameof(relativePath));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("The confirmation token is required.", nameof(token));
        }

        Uri confirmationUri = new(_publicBaseUri, relativePath);
        return $"{confirmationUri.AbsoluteUri}?token={Uri.EscapeDataString(token)}";
    }
}
