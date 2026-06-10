public class RecipePageDetector
{
    public bool LooksLikeRecipePage(string html, string url, Recipe recipe)
    {
        int score = 0;

        bool hasValidName =
            !string.IsNullOrWhiteSpace(recipe.Name) &&
            recipe.Name != AppTexts.ImportedTitleNotFound;

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

        string normalizedHtml = TextHelper.NormalizeForSearch(html);
        string normalizedUrl = TextHelper.NormalizeForSearch(url);

        if (ContainsAny(normalizedUrl, RecipeImportKeywords.RecipeUrlKeywords))
        {
            score += 2;
        }

        if (ContainsAny(normalizedHtml, RecipeImportKeywords.IngredientSectionStartKeywords))
        {
            score += 2;
        }

        if (ContainsAny(normalizedHtml, RecipeImportKeywords.StepsSectionStartKeywords))
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

    public bool LooksLikeBlockedPage(string html)
    {
        string normalizedHtml = TextHelper.NormalizeForSearch(html);

        bool hasStrongVerificationText = ContainsAny(
            normalizedHtml,
            RecipeImportKeywords.StrongBlockedPageKeywords);

        bool hasCloudflareRayId = normalizedHtml.Contains(
            TextHelper.NormalizeForSearch(RecipeImportKeywords.CloudflareRayIdKeyword));

        bool hasCloudflareBlockSignal = ContainsAny(
            normalizedHtml,
            RecipeImportKeywords.CloudflareBlockedPageKeywords);

        int matchedBlockedKeywordsCount =
            CountMatches(normalizedHtml, RecipeImportKeywords.StrongBlockedPageKeywords) +
            CountMatches(normalizedHtml, RecipeImportKeywords.CloudflareBlockedPageKeywords);

        return
            hasStrongVerificationText ||
            (hasCloudflareRayId && hasCloudflareBlockSignal) ||
            matchedBlockedKeywordsCount >= RecipeImportKeywords.BlockedPageMinimumKeywordScore;
    }

    private static bool ContainsAny(string normalizedText, string[] keywords)
    {
        return keywords.Any(keyword =>
            normalizedText.Contains(TextHelper.NormalizeForSearch(keyword)));
    }

    private static int CountMatches(string normalizedText, string[] keywords)
    {
        int count = 0;

        foreach (string keyword in keywords)
        {
            string normalizedKeyword = TextHelper.NormalizeForSearch(keyword);

            if (normalizedText.Contains(normalizedKeyword))
            {
                count++;
            }
        }

        return count;
    }
}
