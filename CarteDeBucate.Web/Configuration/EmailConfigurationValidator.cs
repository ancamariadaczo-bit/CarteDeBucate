public static class EmailConfigurationValidator
{
    public static void Validate(
        ApplicationOptions applicationOptions,
        ResendOptions resendOptions,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);
        ArgumentNullException.ThrowIfNull(resendOptions);

        Uri publicBaseUri = ParsePublicBaseUri(applicationOptions.PublicBaseUrl);

        if (isProduction && publicBaseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Application:PublicBaseUrl must use HTTPS in production.");
        }

        if (isProduction && !resendOptions.Enabled)
        {
            throw new InvalidOperationException(
                "Resend:Enabled must be true in production.");
        }

        if (!resendOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resendOptions.ApiToken))
        {
            throw new InvalidOperationException("Resend:ApiToken is not configured.");
        }

        if (string.IsNullOrWhiteSpace(resendOptions.FromAddress))
        {
            throw new InvalidOperationException("Resend:FromAddress is not configured.");
        }

        if (resendOptions.DeliveryTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Resend:DeliveryTimeout must be greater than zero.");
        }
    }

    internal static Uri ParsePublicBaseUri(string? publicBaseUrl)
    {
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                "Application:PublicBaseUrl must be an absolute HTTP or HTTPS origin.");
        }

        return uri;
    }
}
