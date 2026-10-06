public sealed class RecipePhotoOptions
{
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public int MaxPhotosPerRecipe { get; set; } = 20;

    public TimeSpan ReconciliationMinimumAge { get; set; } = TimeSpan.FromHours(24);
}
