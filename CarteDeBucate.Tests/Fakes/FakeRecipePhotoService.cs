public class FakeRecipePhotoService : IRecipePhotoService
{
    public RecipePhotoResult AddPhotoResultToReturn { get; set; } =
        RecipePhotoResult.Success("Photo added.");

    public RecipePhotosResult GetPhotosResultToReturn { get; set; } =
        RecipePhotosResult.Success([], "Photos loaded.");

    public RecipePhotoContentResult GetPhotoContentResultToReturn { get; set; } =
        RecipePhotoContentResult.Failure(
            RecipePhotoResultCode.PhotoNotFound,
            "Photo not found.");

    public RecipePhotoResult DeletePhotoResultToReturn { get; set; } =
        RecipePhotoResult.Success("Photo deleted.");

    public bool AddPhotoAsyncWasCalled { get; private set; }

    public bool GetPhotosForRecipeWasCalled { get; private set; }

    public bool GetPhotoContentWasCalled { get; private set; }

    public bool DeletePhotoWasCalled { get; private set; }

    public int? RecipeIdPassedToAddPhoto { get; private set; }

    public Stream? ContentPassedToAddPhoto { get; private set; }

    public string? OriginalFileNamePassedToAddPhoto { get; private set; }

    public string? DeclaredContentTypePassedToAddPhoto { get; private set; }

    public long? DeclaredLengthPassedToAddPhoto { get; private set; }

    public RecipePhotoOrigin? OriginPassedToAddPhoto { get; private set; }

    public CancellationToken CancellationTokenPassedToAddPhoto
    {
        get;
        private set;
    }

    public int? RecipeIdPassedToGetPhotos { get; private set; }

    public int? PhotoIdPassedToGetContent { get; private set; }

    public int? PhotoIdPassedToDelete { get; private set; }

    public Task<RecipePhotoResult> AddPhotoAsync(
        int recipeId,
        Stream content,
        string originalFileName,
        string declaredContentType,
        long? declaredLength,
        RecipePhotoOrigin origin,
        CancellationToken cancellationToken = default)
    {
        AddPhotoAsyncWasCalled = true;
        RecipeIdPassedToAddPhoto = recipeId;
        ContentPassedToAddPhoto = content;
        OriginalFileNamePassedToAddPhoto = originalFileName;
        DeclaredContentTypePassedToAddPhoto = declaredContentType;
        DeclaredLengthPassedToAddPhoto = declaredLength;
        OriginPassedToAddPhoto = origin;
        CancellationTokenPassedToAddPhoto = cancellationToken;

        return Task.FromResult(AddPhotoResultToReturn);
    }

    public RecipePhotosResult GetPhotosForRecipe(int recipeId)
    {
        GetPhotosForRecipeWasCalled = true;
        RecipeIdPassedToGetPhotos = recipeId;

        return GetPhotosResultToReturn;
    }

    public RecipePhotoContentResult GetPhotoContent(int photoId)
    {
        GetPhotoContentWasCalled = true;
        PhotoIdPassedToGetContent = photoId;

        return GetPhotoContentResultToReturn;
    }

    public RecipePhotoResult DeletePhoto(int photoId)
    {
        DeletePhotoWasCalled = true;
        PhotoIdPassedToDelete = photoId;

        return DeletePhotoResultToReturn;
    }
}
