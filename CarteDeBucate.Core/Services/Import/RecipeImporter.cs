using RecipeExtraction;

public class RecipeImporter : IRecipeImporter
{
    private readonly RecipePageDownloader _pageDownloader;
    private readonly IRecipeExtractor _recipeExtractor;

    public RecipeImporter(
        HttpClient httpClient,
        RecipeImportDestinationPolicy destinationPolicy)
        : this(new RecipePageDownloader(httpClient, destinationPolicy), new RecipeExtractor())
    {
    }

    private RecipeImporter(RecipePageDownloader pageDownloader, IRecipeExtractor recipeExtractor)
    {
        _pageDownloader = pageDownloader;
        _recipeExtractor = recipeExtractor;
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
            RecipeExtractionResult extractionResult = _recipeExtractor.Extract(html, url, allowTitleOnly: true);

            return RecipeExtractionResultMapper.ToImportResult(extractionResult);
        }
        catch
        {
            return Fail(AppTexts.ImportFailedCouldNotDownloadPage);
        }
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
