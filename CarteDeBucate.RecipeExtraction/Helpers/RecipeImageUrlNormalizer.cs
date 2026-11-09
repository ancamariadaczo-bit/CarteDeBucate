namespace RecipeExtraction;

internal static class RecipeImageUrlNormalizer
{
    public static string? Normalize(string? candidate, string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        string value = candidate.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absolute) &&
            IsHttpDestination(absolute))
        {
            return absolute.AbsoluteUri;
        }

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? source) ||
            !Uri.TryCreate(source, value, out Uri? destination) ||
            !IsHttpDestination(destination))
        {
            return null;
        }

        return destination.AbsoluteUri;
    }

    private static bool IsHttpDestination(Uri destination)
    {
        return destination.Scheme == Uri.UriSchemeHttp ||
            destination.Scheme == Uri.UriSchemeHttps;
    }
}
