public static class UrlValidator
{
    public static bool IsValidHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        url = url.Trim();

        bool isValidUri = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);

        if (!isValidUri || uri == null)
        {
            return false;
        }

        bool isHttpOrHttps =
            uri.Scheme == Uri.UriSchemeHttp ||
            uri.Scheme == Uri.UriSchemeHttps;

        if (!isHttpOrHttps)
        {
            return false;
        }

        bool hasHost = !string.IsNullOrWhiteSpace(uri.Host);

        return hasHost;
    }
}