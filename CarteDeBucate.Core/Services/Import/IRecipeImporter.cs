public interface IRecipeImporter
{
    Task<RecipeImportResult> ImportFromUrlAsync(string url);
}