namespace CarteDeBucate.Web.Models;

public enum RecipePhotoUrlImportResultCode
{
    Success = 0,
    InvalidUrl = 1,
    DownloadFailed = 2,
    ResponseTooLarge = 3,
    UnsupportedContentType = 4,
    PhotoServiceFailure = 5
}

public sealed class RecipePhotoUrlImportResult
{
    private RecipePhotoUrlImportResult(
        bool isSuccess,
        RecipePhotoUrlImportResultCode code,
        string message)
    {
        IsSuccess = isSuccess;
        Code = code;
        Message = message;
    }

    public bool IsSuccess { get; }

    public RecipePhotoUrlImportResultCode Code { get; }

    public string Message { get; }

    public static RecipePhotoUrlImportResult Success(string message)
    {
        return new RecipePhotoUrlImportResult(
            true,
            RecipePhotoUrlImportResultCode.Success,
            message);
    }

    public static RecipePhotoUrlImportResult Failure(
        RecipePhotoUrlImportResultCode code,
        string message)
    {
        if (code == RecipePhotoUrlImportResultCode.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        return new RecipePhotoUrlImportResult(false, code, message);
    }
}
