public class RecipeImporter : IRecipeImporter
{
    private readonly HttpClient _httpClient;
    private readonly RecipeJsonLdImporter _jsonLdImporter;
    private readonly RecipeHtmlImporter _htmlImporter;
    private readonly RecipePageDetector _pageDetector;

    public RecipeImporter()
        : this(CreateDefaultHttpClient())
    {
    }

    public RecipeImporter(HttpClient httpClient)
        : this(
            httpClient,
            new RecipeJsonLdImporter(),
            new RecipeHtmlImporter(),
            new RecipePageDetector())
    {
    }

    public RecipeImporter(
        HttpClient httpClient,
        RecipeJsonLdImporter jsonLdImporter,
        RecipeHtmlImporter htmlImporter,
        RecipePageDetector pageDetector)
    {
        _httpClient = httpClient;
        _jsonLdImporter = jsonLdImporter;
        _htmlImporter = htmlImporter;
        _pageDetector = pageDetector;
    }

    public async Task<RecipeImportResult> ImportFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Fail(AppTexts.ImportFailedEmptyUrl);
        }

        url = url.Trim();

        if (!UrlValidator.IsValidHttpUrl(url))
        {
            return Fail(AppTexts.ImportFailedInvalidUrl);
        }

        try
        {
            string html = await _httpClient.GetStringAsync(url);

            if (_pageDetector.LooksLikeBlockedPage(html))
            {
                return Fail(AppTexts.ImportFailedBlockedPage);
            }

            Recipe? recipeFromJsonLd = _jsonLdImporter.TryImport(html, url);

            if (recipeFromJsonLd != null && _pageDetector.LooksLikeRecipePage(html, url, recipeFromJsonLd))
            {
                return Success(recipeFromJsonLd, AppTexts.ImportSuccessJsonLd);
            }

            Recipe? recipeFromVisibleHtml = _htmlImporter.TryImport(html, url);

            if (recipeFromVisibleHtml != null && _pageDetector.LooksLikeRecipePage(html, url, recipeFromVisibleHtml))
            {
                return Success(recipeFromVisibleHtml, AppTexts.ImportSuccessVisibleHtml);
            }

            Recipe titleOnlyRecipe = CreateTitleOnlyRecipe(html, url);

            if (!_pageDetector.LooksLikeRecipePage(html, url, titleOnlyRecipe))
            {
                return Fail(AppTexts.ImportFailedNotRecipePage);
            }

            return Success(titleOnlyRecipe, AppTexts.ImportSuccessTitleOnly);
        }
        catch
        {
            return Fail(AppTexts.ImportFailedCouldNotDownloadPage);
        }
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        HttpClient httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; CarteDeBucateApp/1.0)");

        return httpClient;
    }

    private static Recipe CreateTitleOnlyRecipe(string html, string url)
    {
        string title = RecipeHtmlImporter.ExtractTitle(html);

        return new Recipe
        {
            Name = string.IsNullOrWhiteSpace(title)
                ? AppTexts.ImportedTitleNotFound
                : title,

            SourceUrl = url.Trim(),
            SavedAt = DateTime.Now
        };
    }

    private static RecipeImportResult Success(Recipe recipe, string message)
    {
        return new RecipeImportResult
        {
            Success = true,
            Recipe = recipe,
            Message = message
        };
    }

    private static RecipeImportResult Fail(string message)
    {
        return new RecipeImportResult
        {
            Success = false,
            Message = message
        };
    }
}
