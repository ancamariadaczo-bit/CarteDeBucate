public interface IRecipeImporterService
{
    Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url);
}
