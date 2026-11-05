namespace RecipeExtraction;

public class RecipeExtractor : IRecipeExtractor
{
    private readonly RecipePageDetector pageDetector = new RecipePageDetector();
    private readonly RecipeJsonLdExtractor jsonLdExtractor = new RecipeJsonLdExtractor();
    private readonly RecipeHtmlExtractor htmlExtractor = new RecipeHtmlExtractor();

    public RecipeExtractionResult Extract(string html, string url, bool allowTitleOnly = false)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return CreateFailure(FailureReason.EmptyHtml);
        }

        string normalizedUrl = url?.Trim() ?? "";

        if (pageDetector.LooksLikeBlockedPage(html))
        {
            return CreateFailure(FailureReason.BlockedPage);
        }

        ExtractedRecipe? recipe = jsonLdExtractor.TryExtract(html, normalizedUrl);

        if (recipe != null)
        {
            return CreateSuccess(recipe, ExtractionMethod.JsonLd);
        }

        recipe = htmlExtractor.TryExtract(html, normalizedUrl);

        if (recipe != null)
        {
            return CreateSuccess(recipe, ExtractionMethod.VisibleHtml);
        }

        if (allowTitleOnly)
        {
            string title = RecipeHtmlExtractor.ExtractTitle(html);

            if (!string.IsNullOrWhiteSpace(title))
            {
                ExtractedRecipe titleOnlyRecipe = new ExtractedRecipe
                {
                    Name = title,
                    SourceUrl = normalizedUrl
                };

                if (pageDetector.LooksLikeRecipePage(html, normalizedUrl, titleOnlyRecipe))
                {
                    return CreateSuccess(titleOnlyRecipe, ExtractionMethod.TitleOnly);
                }
            }
        }

        return CreateFailure(FailureReason.NotRecipePage);
    }

    private static RecipeExtractionResult CreateSuccess(
        ExtractedRecipe recipe,
        ExtractionMethod extractionMethod)
    {
        return new RecipeExtractionResult
        {
            Success = true,
            Recipe = recipe,
            ExtractionMethod = extractionMethod,
            FailureReason = FailureReason.None
        };
    }

    private static RecipeExtractionResult CreateFailure(FailureReason failureReason)
    {
        return new RecipeExtractionResult
        {
            Success = false,
            Recipe = null,
            ExtractionMethod = ExtractionMethod.None,
            FailureReason = failureReason
        };
    }
}
