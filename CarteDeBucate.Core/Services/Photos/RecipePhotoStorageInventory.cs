internal sealed record StagedRecipePhotoUploadInventoryItem(
    StagedRecipePhotoUpload Upload,
    DateTime LastModifiedAtUtc);

internal sealed record StagedRecipePhotoDeletionInventoryItem(
    StagedRecipePhotoDeletion Deletion,
    string StorageFileName,
    DateTime QuarantinedAtUtc);

internal sealed record RecipePhotoStorageFileInventoryItem(
    string StorageFileName,
    DateTime LastModifiedAtUtc);

internal sealed class RecipePhotoStorageInventory<TItem>
{
    public RecipePhotoStorageInventory(
        IReadOnlyList<TItem> items,
        int invalidEntryCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (invalidEntryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invalidEntryCount));
        }

        Items = items;
        InvalidEntryCount = invalidEntryCount;
    }

    public IReadOnlyList<TItem> Items { get; }

    public int InvalidEntryCount { get; }
}
