
namespace RecipeExtraction;

/// <summary>
/// Applies heuristics for recognizing recipe pages and common blocked-page responses.
/// </summary>
public class RecipePageDetector
{
    /// <summary>
    /// Determines whether the supplied content and extraction result resemble a recipe page.
    /// </summary>
    /// <param name="html">The HTML content to inspect.</param>
    /// <param name="url">The source URL associated with the HTML content.</param>
    /// <param name="recipe">The recipe data extracted from the page.</param>
    /// <returns><see langword="true"/> when the page meets the recipe-page heuristic threshold.</returns>
    public bool LooksLikeRecipePage(string html, string url, ExtractedRecipe recipe)
    {
        int score = 0;

        bool hasValidName = !string.IsNullOrWhiteSpace(recipe.Name);

        if (hasValidName)
        {
            score += 1;
        }

        if (recipe.Ingredients.Count >= 2)
        {
            score += 4;
        }

        if (recipe.Steps.Count >= 2)
        {
            score += 4;
        }

        string normalizedHtml = RecipeExtractionTextHelper.NormalizeForSearch(html);
        string normalizedUrl = RecipeExtractionTextHelper.NormalizeForSearch(url);

        if (ContainsAny(normalizedUrl, RecipeExtractionKeywords.RecipeUrlKeywords))
        {
            score += 2;
        }

        if (ContainsAny(normalizedHtml, RecipeExtractionKeywords.IngredientSectionStartKeywords))
        {
            score += 2;
        }

        if (ContainsAny(normalizedHtml, RecipeExtractionKeywords.StepsSectionStartKeywords))
        {
            score += 2;
        }

        bool hasJsonLdRecipe =
            normalizedHtml.Contains("application/ld+json") &&
            normalizedHtml.Contains("recipe");

        if (hasJsonLdRecipe)
        {
            score += 3;
        }

        return score >= 5;
    }

    /// <summary>
    /// Determines whether the HTML resembles an automated verification or blocked page.
    /// </summary>
    /// <param name="html">The HTML content to inspect.</param>
    /// <returns><see langword="true"/> when blocked-page indicators are detected.</returns>
    public bool LooksLikeBlockedPage(string html)
    {
        string normalizedHtml = RecipeExtractionTextHelper.NormalizeForSearch(html);

        bool hasStrongVerificationText = ContainsAny(
            normalizedHtml,
            RecipeExtractionKeywords.StrongBlockedPageKeywords);

        bool hasCloudflareRayId = normalizedHtml.Contains(
            RecipeExtractionTextHelper.NormalizeForSearch(RecipeExtractionKeywords.CloudflareRayIdKeyword));

        bool hasCloudflareBlockSignal = ContainsAny(
            normalizedHtml,
            RecipeExtractionKeywords.CloudflareBlockedPageKeywords);

        int matchedBlockedKeywordsCount =
            CountMatches(normalizedHtml, RecipeExtractionKeywords.StrongBlockedPageKeywords) +
            CountMatches(normalizedHtml, RecipeExtractionKeywords.CloudflareBlockedPageKeywords);

        return
            hasStrongVerificationText ||
            (hasCloudflareRayId && hasCloudflareBlockSignal) ||
            matchedBlockedKeywordsCount >= RecipeExtractionKeywords.BlockedPageMinimumKeywordScore;
    }

    private static bool ContainsAny(string normalizedText, string[] keywords)
    {
        return keywords.Any(keyword =>
            normalizedText.Contains(RecipeExtractionTextHelper.NormalizeForSearch(keyword)));
    }

    private static int CountMatches(string normalizedText, string[] keywords)
    {
        int count = 0;

        foreach (string keyword in keywords)
        {
            string normalizedKeyword = RecipeExtractionTextHelper.NormalizeForSearch(keyword);

            if (normalizedText.Contains(normalizedKeyword))
            {
                count++;
            }
        }

        return count;
    }
}
