public sealed class RecipePhotoContentResult
{
    private RecipePhotoContentResult(
        bool isSuccess,
        RecipePhotoResultCode code,
        string message,
        Stream? content,
        string contentType,
        string originalFileName)
    {
        IsSuccess = isSuccess;
        Code = code;
        Message = message;
        Content = content;
        ContentType = contentType;
        OriginalFileName = originalFileName;
    }

    public bool IsSuccess { get; }

    public RecipePhotoResultCode Code { get; }

    public string Message { get; }

    /// <summary>
    /// On success, the caller owns this stream and is responsible for disposing it.
    /// The value is always <see langword="null"/> on failure.
    /// </summary>
    public Stream? Content { get; }

    public string ContentType { get; }

    public string OriginalFileName { get; }

    public static RecipePhotoContentResult Success(
        Stream content,
        string contentType,
        string originalFileName,
        string message)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new RecipePhotoContentResult(
            true,
            RecipePhotoResultCode.Success,
            message,
            content,
            contentType,
            originalFileName);
    }

    public static RecipePhotoContentResult Failure(
        RecipePhotoResultCode code,
        string message)
    {
        EnsureFailureCode(code);

        return new RecipePhotoContentResult(
            false,
            code,
            message,
            null,
            string.Empty,
            string.Empty);
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
