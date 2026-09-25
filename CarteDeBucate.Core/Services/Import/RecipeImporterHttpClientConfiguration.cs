public static class RecipeImporterHttpClientConfiguration
{
    public const string UserAgent =
        "Mozilla/5.0 (compatible; CarteDeBucateApp/1.0)";

    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static void Configure(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        httpClient.Timeout = Timeout;
    }
}
