public enum RecipePhotoResultCode
{
    Success = 0,
    DeletedMetadataFileWasMissing = 1,
    CleanupPending = 2,
    InvalidRecipeId = 3,
    RecipeNotFound = 4,
    PhotoNotFound = 5,
    PhotoLimitReached = 6,
    EmptyContent = 7,
    FileTooLarge = 8,
    UnsupportedFormat = 9,
    InvalidContent = 10,
    ContentFileMissing = 11,
    StorageFailure = 12,
    PersistenceFailure = 13
}
