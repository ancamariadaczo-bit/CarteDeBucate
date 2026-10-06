public sealed class RecipePhotosResult
{
    private RecipePhotosResult(
        bool isSuccess,
        RecipePhotoResultCode code,
        string message,
        IReadOnlyList<RecipePhotoInfo> photos)
    {
        IsSuccess = isSuccess;
        Code = code;
        Message = message;
        Photos = photos;
    }

    public bool IsSuccess { get; }

    public RecipePhotoResultCode Code { get; }

    public string Message { get; }

    public IReadOnlyList<RecipePhotoInfo> Photos { get; }

    public static RecipePhotosResult Success(
        IReadOnlyList<RecipePhotoInfo> photos,
        string message)
    {
        ArgumentNullException.ThrowIfNull(photos);

        return new RecipePhotosResult(
            true,
            RecipePhotoResultCode.Success,
            message,
            photos.ToArray());
    }

    public static RecipePhotosResult Failure(
        RecipePhotoResultCode code,
        string message)
    {
        EnsureFailureCode(code);

        return new RecipePhotosResult(
            false,
            code,
            message,
            Array.Empty<RecipePhotoInfo>());
    }

    private static void EnsureFailureCode(RecipePhotoResultCode code)
    {
        if (code is RecipePhotoResultCode.Success
            or RecipePhotoResultCode.DeletedMetadataFileWasMissing
            or RecipePhotoResultCode.CleanupPending)
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "A success code cannot be used for a failure result.");
        }
    }
}
