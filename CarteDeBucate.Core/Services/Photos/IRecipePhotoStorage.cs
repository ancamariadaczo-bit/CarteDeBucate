internal interface IRecipePhotoStorage
{
    Task<StagedRecipePhotoUpload> StageUploadAsync(
        Stream content,
        long maximumBytes,
        CancellationToken cancellationToken = default);

    Stream OpenStagedUpload(StagedRecipePhotoUpload upload);

    string FinalizeUpload(
        StagedRecipePhotoUpload upload,
        RecipePhotoFormat format);

    void AbandonUpload(StagedRecipePhotoUpload upload);

    Stream? OpenFinalFile(string storageFileName);

    bool FinalFileExists(string storageFileName);

    StagedRecipePhotoDeletion? MoveToQuarantine(string storageFileName);

    void RestoreFromQuarantine(StagedRecipePhotoDeletion deletion);

    void DeleteFromQuarantine(StagedRecipePhotoDeletion deletion);

    RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
        GetStagedUploads();

    RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
        GetQuarantinedDeletions();

    RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
        GetFinalFiles();
}
