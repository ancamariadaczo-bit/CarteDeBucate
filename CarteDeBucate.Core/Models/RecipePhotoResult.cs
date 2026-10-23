public sealed class RecipePhotoResult
{
    private RecipePhotoResult(
        bool isSuccess,
        RecipePhotoResultCode code,
        string message,
        RecipePhotoInfo? photo)
    {
        IsSuccess = isSuccess;
        Code = code;
        Message = message;
        Photo = photo;
    }

    public bool IsSuccess { get; }

    public RecipePhotoResultCode Code { get; }

    public string Message { get; }

    public RecipePhotoInfo? Photo { get; }

    public static RecipePhotoResult Success(
        string message,
        RecipePhotoInfo? photo = null)
    {
        return new RecipePhotoResult(
            true,
            RecipePhotoResultCode.Success,
            message,
            photo);
    }

    public static RecipePhotoResult SuccessWithWarning(
        RecipePhotoResultCode code,
        string message,
        RecipePhotoInfo? photo = null)
    {
        if (code is not RecipePhotoResultCode.DeletedMetadataFileWasMissing
            and not RecipePhotoResultCode.CleanupPending)
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "The code is not a success-with-warning code.");
        }

        return new RecipePhotoResult(true, code, message, photo);
    }

    public static RecipePhotoResult Failure(
        RecipePhotoResultCode code,
        string message)
    {
        EnsureFailureCode(code);

        return new RecipePhotoResult(false, code, message, null);
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
