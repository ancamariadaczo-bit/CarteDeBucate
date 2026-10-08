using Microsoft.Extensions.Logging;

internal sealed class RecipePhotoService : IRecipePhotoService
{
    private readonly IRecipePhotoStorage _storage;
    private readonly IRecipePhotoRepository _photoRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly RecipePhotoValidator _validator;
    private readonly RecipePhotoFileNameNormalizer _fileNameNormalizer;
    private readonly RecipePhotoOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecipePhotoService> _logger;

    public RecipePhotoService(
        IRecipePhotoStorage storage,
        IRecipePhotoRepository photoRepository,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        RecipePhotoValidator validator,
        RecipePhotoFileNameNormalizer fileNameNormalizer,
        RecipePhotoOptions options,
        TimeProvider timeProvider,
        ILogger<RecipePhotoService> logger)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(photoRepository);
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fileNameNormalizer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _storage = storage;
        _photoRepository = photoRepository;
        _recipeRepository = recipeRepository;
        _currentUserContext = currentUserContext;
        _validator = validator;
        _fileNameNormalizer = fileNameNormalizer;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    internal int? CurrentUserId => _currentUserContext?.UserId;

    internal Recipe? GetRecipeInCurrentContext(int recipeId)
    {
        return CurrentUserId.HasValue
            ? _recipeRepository.GetRecipeByIdAndUserId(
                recipeId,
                CurrentUserId.Value)
            : _recipeRepository.GetRecipeById(recipeId);
    }

    internal RecipePhoto? GetPhotoInCurrentContext(int photoId)
    {
        return CurrentUserId.HasValue
            ? _photoRepository.GetByIdAndUserId(
                photoId,
                CurrentUserId.Value)
            : _photoRepository.GetById(photoId);
    }

    internal List<RecipePhoto> GetPhotosInCurrentContext(int recipeId)
    {
        return CurrentUserId.HasValue
            ? _photoRepository.GetByRecipeIdAndUserId(
                recipeId,
                CurrentUserId.Value)
            : _photoRepository.GetByRecipeId(recipeId);
    }

    internal static RecipePhotoInfo ToPhotoInfo(RecipePhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);

        return new RecipePhotoInfo
        {
            Id = photo.Id,
            RecipeId = photo.RecipeId,
            OriginalFileName = photo.OriginalFileName,
            ContentType = photo.ContentType,
            FileSize = photo.FileSize,
            CreatedAtUtc = photo.CreatedAtUtc,
            DisplayOrder = photo.DisplayOrder,
            Origin = photo.Origin
        };
    }

    public async Task<RecipePhotoResult> AddPhotoAsync(
        int recipeId,
        Stream content,
        string originalFileName,
        string declaredContentType,
        long? declaredLength,
        RecipePhotoOrigin origin,
        CancellationToken cancellationToken = default)
    {
        RecipePhotoResult? inputFailure = ValidateAddInput(
            recipeId,
            content,
            declaredContentType,
            declaredLength,
            origin);

        if (inputFailure is not null)
        {
            return inputFailure;
        }

        Recipe? recipe;
        List<RecipePhoto> existingPhotos;

        try
        {
            recipe = GetRecipeInCurrentContext(recipeId);

            if (recipe is null)
            {
                return RecipePhotoResult.Failure(
                    RecipePhotoResultCode.RecipeNotFound,
                    "The recipe was not found.");
            }

            existingPhotos = GetPhotosInCurrentContext(recipeId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to read recipe photo metadata for recipe {RecipeId}.",
                recipeId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The recipe photo metadata could not be read.");
        }

        // This check and the later insert are intentionally not atomic in the
        // first version. Concurrent uploads can exceed the configured limit.
        if (existingPhotos.Count >= _options.MaxPhotosPerRecipe)
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PhotoLimitReached,
                "The recipe already has the maximum number of photos.");
        }

        if (declaredLength > _options.MaxFileSizeBytes)
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.FileTooLarge,
                "The declared photo size exceeds the configured limit.");
        }

        int displayOrder = existingPhotos.Count == 0
            ? 0
            : existingPhotos.Max(photo => photo.DisplayOrder) + 1;
        StagedRecipePhotoUpload stagedUpload;

        try
        {
            stagedUpload = await _storage.StageUploadAsync(
                content,
                _options.MaxFileSizeBytes,
                cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RecipePhotoStorageLimitExceededException)
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.FileTooLarge,
                "The photo exceeds the configured size limit.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to stage a photo for recipe {RecipeId}.",
                recipeId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.StorageFailure,
                "The photo could not be stored.");
        }

        RecipePhotoValidationResult validationResult;

        try
        {
            using Stream stagedContent =
                _storage.OpenStagedUpload(stagedUpload);
            validationResult = _validator.Validate(
                stagedContent,
                declaredContentType);
        }
        catch (Exception exception)
        {
            TryAbandonStagedUpload(stagedUpload, recipeId);
            _logger.LogError(
                exception,
                "Failed to validate a staged photo for recipe {RecipeId}.",
                recipeId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.StorageFailure,
                "The staged photo could not be validated.");
        }

        if (!validationResult.IsValid)
        {
            TryAbandonStagedUpload(stagedUpload, recipeId);

            return RecipePhotoResult.Failure(
                validationResult.Code,
                "The photo content is not valid.");
        }

        string normalizedFileName;
        string storageFileName;

        try
        {
            normalizedFileName = _fileNameNormalizer.Normalize(
                originalFileName,
                validationResult.Format);
            cancellationToken.ThrowIfCancellationRequested();
            storageFileName = _storage.FinalizeUpload(
                stagedUpload,
                validationResult.Format);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            TryAbandonStagedUpload(stagedUpload, recipeId);
            throw;
        }
        catch (Exception exception)
        {
            TryAbandonStagedUpload(stagedUpload, recipeId);
            _logger.LogError(
                exception,
                "Failed to finalize a photo for recipe {RecipeId}.",
                recipeId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.StorageFailure,
                "The photo could not be finalized.");
        }

        RecipePhoto photo;

        try
        {
            photo = new RecipePhoto
            {
                RecipeId = recipeId,
                StorageFileName = storageFileName,
                OriginalFileName = normalizedFileName,
                ContentType = validationResult.ContentType,
                FileSize = stagedUpload.Length,
                CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                DisplayOrder = displayOrder,
                Origin = origin
            };

            if (CurrentUserId.HasValue)
            {
                bool wasAdded = _photoRepository.AddForUser(
                    photo,
                    CurrentUserId.Value);

                if (!wasAdded)
                {
                    TryRemoveFinalFile(storageFileName, recipeId);

                    return RecipePhotoResult.Failure(
                        RecipePhotoResultCode.RecipeNotFound,
                        "The recipe was not found.");
                }
            }
            else
            {
                _photoRepository.Add(photo);
            }
        }
        catch (Exception exception)
        {
            TryRemoveFinalFile(storageFileName, recipeId);
            _logger.LogError(
                exception,
                "Failed to persist photo metadata for recipe {RecipeId}.",
                recipeId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The photo metadata could not be saved.");
        }

        return RecipePhotoResult.Success(
            "The photo was added.",
            ToPhotoInfo(photo));
    }

    public RecipePhotosResult GetPhotosForRecipe(int recipeId)
    {
        if (recipeId <= 0)
        {
            return RecipePhotosResult.Failure(
                RecipePhotoResultCode.InvalidRecipeId,
                "The recipe id must be greater than zero.");
        }

        try
        {
            Recipe? recipe = GetRecipeInCurrentContext(recipeId);

            if (recipe is null)
            {
                return RecipePhotosResult.Failure(
                    RecipePhotoResultCode.RecipeNotFound,
                    "The recipe was not found.");
            }

            List<RecipePhotoInfo> photos =
                GetPhotosInCurrentContext(recipeId)
                    .Select(ToPhotoInfo)
                    .ToList();

            return RecipePhotosResult.Success(
                photos,
                "The recipe photos were loaded.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load photos for recipe {RecipeId}.",
                recipeId);

            return RecipePhotosResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The recipe photos could not be loaded.");
        }
    }

    public RecipePhotoContentResult GetPhotoContent(int photoId)
    {
        if (photoId <= 0)
        {
            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.PhotoNotFound,
                "The photo was not found.");
        }

        RecipePhoto? photo;

        try
        {
            photo = GetPhotoInCurrentContext(photoId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load photo metadata for photo {PhotoId}.",
                photoId);

            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The photo metadata could not be loaded.");
        }

        if (photo is null)
        {
            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.PhotoNotFound,
                "The photo was not found.");
        }

        RecipePhotoFormat format = RecipePhotoFormats
            .GetFormatFromContentType(photo.ContentType);
        string? normalizedContentType = RecipePhotoFormats
            .GetContentType(format);

        if (normalizedContentType is null)
        {
            _logger.LogError(
                "Photo {PhotoId} has an unsupported stored content type.",
                photoId);

            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The stored photo metadata is invalid.");
        }

        string normalizedFileName;

        try
        {
            normalizedFileName = _fileNameNormalizer.Normalize(
                photo.OriginalFileName,
                format);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Photo {PhotoId} has invalid stored file metadata.",
                photoId);

            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The stored photo metadata is invalid.");
        }

        Stream? content;

        try
        {
            content = _storage.OpenFinalFile(photo.StorageFileName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to open the content file for photo {PhotoId}.",
                photoId);

            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.StorageFailure,
                "The photo content could not be opened.");
        }

        if (content is null)
        {
            _logger.LogWarning(
                "The content file for photo {PhotoId} is missing.",
                photoId);

            return RecipePhotoContentResult.Failure(
                RecipePhotoResultCode.ContentFileMissing,
                "The photo content file is missing.");
        }

        return RecipePhotoContentResult.Success(
            content,
            normalizedContentType,
            normalizedFileName,
            "The photo content was loaded.");
    }

    public RecipePhotoResult DeletePhoto(int photoId)
    {
        if (photoId <= 0)
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PhotoNotFound,
                "The photo was not found.");
        }

        RecipePhoto? photo;

        try
        {
            photo = GetPhotoInCurrentContext(photoId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load photo metadata for photo {PhotoId}.",
                photoId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The photo metadata could not be loaded.");
        }

        if (photo is null)
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PhotoNotFound,
                "The photo was not found.");
        }

        StagedRecipePhotoDeletion? stagedDeletion;

        try
        {
            stagedDeletion = _storage.MoveToQuarantine(
                photo.StorageFileName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to quarantine the content for photo {PhotoId}.",
                photoId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.StorageFailure,
                "The photo content could not be prepared for deletion.");
        }

        bool wasDeleted;

        try
        {
            wasDeleted = CurrentUserId.HasValue
                ? _photoRepository.DeleteForUser(
                    photoId,
                    CurrentUserId.Value)
                : _photoRepository.Delete(photoId);
        }
        catch (Exception exception)
        {
            TryRestoreQuarantinedFile(stagedDeletion, photoId);
            _logger.LogError(
                exception,
                "Failed to delete metadata for photo {PhotoId}.",
                photoId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PersistenceFailure,
                "The photo metadata could not be deleted.");
        }

        if (!wasDeleted)
        {
            TryRestoreQuarantinedFile(stagedDeletion, photoId);

            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.PhotoNotFound,
                "The photo was not found.");
        }

        if (stagedDeletion is null)
        {
            return RecipePhotoResult.SuccessWithWarning(
                RecipePhotoResultCode.DeletedMetadataFileWasMissing,
                "The photo metadata was deleted, but its content file was missing.");
        }

        try
        {
            _storage.DeleteFromQuarantine(stagedDeletion);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cleanup is pending for deleted photo {PhotoId}.",
                photoId);

            return RecipePhotoResult.SuccessWithWarning(
                RecipePhotoResultCode.CleanupPending,
                "The photo was deleted and file cleanup is pending.");
        }

        return RecipePhotoResult.Success("The photo was deleted.");
    }

    private static RecipePhotoResult? ValidateAddInput(
        int recipeId,
        Stream? content,
        string? declaredContentType,
        long? declaredLength,
        RecipePhotoOrigin origin)
    {
        if (recipeId <= 0)
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.InvalidRecipeId,
                "The recipe id must be greater than zero.");
        }

        if (content is null
            || !content.CanRead
            || declaredLength < 0
            || origin is not (RecipePhotoOrigin.RecipeSource
                or RecipePhotoOrigin.UserUpload))
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.InvalidContent,
                "The declared photo data is not valid.");
        }

        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return RecipePhotoResult.Failure(
                RecipePhotoResultCode.UnsupportedFormat,
                "The declared photo format is not supported.");
        }

        return null;
    }

    private void TryAbandonStagedUpload(
        StagedRecipePhotoUpload stagedUpload,
        int recipeId)
    {
        try
        {
            _storage.AbandonUpload(stagedUpload);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to abandon a staged photo for recipe {RecipeId}.",
                recipeId);
        }
    }

    private void TryRemoveFinalFile(
        string storageFileName,
        int recipeId)
    {
        try
        {
            StagedRecipePhotoDeletion? deletion =
                _storage.MoveToQuarantine(storageFileName);

            if (deletion is not null)
            {
                _storage.DeleteFromQuarantine(deletion);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to remove a finalized photo for recipe {RecipeId}.",
                recipeId);
        }
    }

    private void TryRestoreQuarantinedFile(
        StagedRecipePhotoDeletion? stagedDeletion,
        int photoId)
    {
        if (stagedDeletion is null)
        {
            return;
        }

        try
        {
            _storage.RestoreFromQuarantine(stagedDeletion);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to restore quarantined content for photo {PhotoId}.",
                photoId);
        }
    }
}
