using CarteDeBucate.Web.Models;
using CarteDeBucate.Web.Services.Photos;

public sealed class FakeRecipePhotoUrlImporter : IRecipePhotoUrlImporter
{
    public RecipePhotoUrlImportResult ResultToReturn { get; set; } =
        RecipePhotoUrlImportResult.Success("Photo imported.");

    public bool ImportAsyncWasCalled { get; private set; }

    public int? RecipeIdPassedToImport { get; private set; }

    public string? ImageUrlPassedToImport { get; private set; }

    public CancellationToken CancellationTokenPassedToImport { get; private set; }

    public Task<RecipePhotoUrlImportResult> ImportAsync(
        int recipeId,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        ImportAsyncWasCalled = true;
        RecipeIdPassedToImport = recipeId;
        ImageUrlPassedToImport = imageUrl;
        CancellationTokenPassedToImport = cancellationToken;

        return Task.FromResult(ResultToReturn);
    }
}
