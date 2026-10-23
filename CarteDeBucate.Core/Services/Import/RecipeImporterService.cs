public class RecipeImporterService : IRecipeImporterService
{
    private readonly IRecipeImporter _recipeImporter;

    public RecipeImporterService(IRecipeImporter recipeImporter)
    {
        _recipeImporter = recipeImporter;
    }

    public async Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new RecipeImportResult
            {
                Success = false,
                Message = AppTexts.EmptyUrl
            };
        }

        return await _recipeImporter.ImportFromUrlAsync(url);
    }
}
