public static class TextHelper
{
    public static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        return text
            .ToLowerInvariant()
            .Replace("ă", "a")
            .Replace("â", "a")
            .Replace("î", "i")
            .Replace("ș", "s")
            .Replace("ş", "s")
            .Replace("ț", "t")
            .Replace("ţ", "t");
    }
}