using RecipeExtraction;

public static class RecipeExtractionResultMapper
{
    public static RecipeImportResult ToImportResult(RecipeExtractionResult extractionResult)
    {
        ArgumentNullException.ThrowIfNull(extractionResult);

        if (!extractionResult.Success)
        {
            return CreateFailure(GetFailureMessage(extractionResult.FailureReason));
        }

        if (extractionResult.Recipe is null)
        {
            return CreateFailure(AppTexts.RecipeImportFailed);
        }

        string? successMessage = GetSuccessMessage(extractionResult.ExtractionMethod);

        if (successMessage is null)
        {
            return CreateFailure(AppTexts.RecipeImportFailed);
        }

        return new RecipeImportResult
        {
            Success = true,
            Recipe = ExtractedRecipeMapper.ToRecipe(extractionResult.Recipe),
            ImageUrl = extractionResult.Recipe.ImageUrl,
            Message = successMessage
        };
    }

    private static string? GetSuccessMessage(ExtractionMethod extractionMethod)
    {
        return extractionMethod switch
        {
            ExtractionMethod.JsonLd => AppTexts.ImportSuccessJsonLd,
            ExtractionMethod.VisibleHtml => AppTexts.ImportSuccessVisibleHtml,
            ExtractionMethod.TitleOnly => AppTexts.ImportSuccessTitleOnly,
            _ => null
        };
    }

    private static string GetFailureMessage(FailureReason failureReason)
    {
        return failureReason switch
        {
            FailureReason.EmptyHtml => AppTexts.ImportFailedCouldNotDownloadPage,
            FailureReason.BlockedPage => AppTexts.ImportFailedBlockedPage,
            FailureReason.NotRecipePage => AppTexts.ImportFailedNotRecipePage,
            FailureReason.InvalidContent => AppTexts.RecipeImportFailed,
            _ => AppTexts.RecipeImportFailed
        };
    }

    private static RecipeImportResult CreateFailure(string message)
    {
        return new RecipeImportResult
        {
            Success = false,
            Recipe = null,
            Message = message
        };
    }
}
