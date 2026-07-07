public class FakeRecipeImporterService : IRecipeImporterService
{
    public RecipeImportResult ImportResultToReturn { get; set; } = new();

    public bool ImportRecipeFromUrlAsyncWasCalled { get; private set; }

    public string? UrlPassedToImportRecipeFromUrlAsync { get; private set; }

    public Task<RecipeImportResult> ImportRecipeFromUrlAsync(string url)
    {
        ImportRecipeFromUrlAsyncWasCalled = true;
        UrlPassedToImportRecipeFromUrlAsync = url;

        return Task.FromResult(ImportResultToReturn);
    }
}
