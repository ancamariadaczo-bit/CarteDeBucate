public class FakeRecipeImporter : IRecipeImporter
{
    public RecipeImportResult ImportResult { get; set; } = new RecipeImportResult
    {
        Success = false,
        Message = "Import result was not configured."
    };

    public Task<RecipeImportResult> ImportFromUrlAsync(string url)
    {
        return Task.FromResult(ImportResult);
    }
}
