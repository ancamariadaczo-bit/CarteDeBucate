public class RecipeImporter : IRecipeImporter
{
    private readonly RecipePageDownloader _pageDownloader;
    private readonly RecipeJsonLdImporter _jsonLdImporter;
    private readonly RecipeHtmlImporter _htmlImporter;
    private readonly RecipePageDetector _pageDetector;

    public RecipeImporter(
        HttpClient httpClient,
        RecipeImportDestinationPolicy destinationPolicy)
        : this(
            new RecipePageDownloader(httpClient, destinationPolicy),
            new RecipeJsonLdImporter(),
            new RecipeHtmlImporter(),
            new RecipePageDetector())
    {
    }

    private RecipeImporter(
        RecipePageDownloader pageDownloader,
        RecipeJsonLdImporter jsonLdImporter,
        RecipeHtmlImporter htmlImporter,
        RecipePageDetector pageDetector)
    {
        _pageDownloader = pageDownloader;
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
            string html = await _pageDownloader.DownloadAsync(new Uri(url));

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
