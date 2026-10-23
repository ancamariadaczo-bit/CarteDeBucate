using Microsoft.Extensions.Logging;

public sealed class RecipePhotoServiceGetPhotoContentTests
{
    [Fact]
    public void GetPhotoContent_WhenPhotoExists_ShouldReturnCallerOwnedContent()
    {
        RecipePhoto photo = CreatePhoto();
        GetPhotoContentRepository photoRepository = new(photo);
        TrackingMemoryStream storedContent = new([1, 2, 3]);
        GetPhotoContentStorage storage = new()
        {
            Content = storedContent
        };
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoContentResult result = service.GetPhotoContent(photo.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Same(storedContent, result.Content);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal("prajitura.jpg", result.OriginalFileName);
        Assert.Equal(photo.StorageFileName, storage.OpenedStorageFileName);
        Assert.False(storedContent.IsDisposed);

        result.Content!.Dispose();
        Assert.True(storedContent.IsDisposed);
    }

    [Fact]
    public void GetPhotoContent_WithAnotherUsersPhoto_ShouldReturnNotFound()
    {
        RecipePhoto photo = CreatePhoto();
        GetPhotoContentRepository photoRepository = new(photo)
        {
            OwnerUserId = 9
        };
        GetPhotoContentStorage storage = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            CreateCurrentUser(userId: 7));

        RecipePhotoContentResult result = service.GetPhotoContent(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.Null(result.Content);
        Assert.True(photoRepository.GetByIdAndUserIdWasCalled);
        Assert.False(storage.OpenFinalFileWasCalled);
    }

    [Fact]
    public void GetPhotoContent_WhenMetadataIsMissing_ShouldReturnNotFound()
    {
        GetPhotoContentRepository photoRepository = new(photo: null);
        GetPhotoContentStorage storage = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoContentResult result = service.GetPhotoContent(20);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.Null(result.Content);
        Assert.True(photoRepository.GetByIdWasCalled);
        Assert.False(storage.OpenFinalFileWasCalled);
    }

    [Fact]
    public void GetPhotoContent_WhenContentFileIsMissing_ShouldReturnControlledFailure()
    {
        RecipePhoto photo = CreatePhoto();
        GetPhotoContentRepository photoRepository = new(photo);
        GetPhotoContentStorage storage = new() { Content = null };
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger: logger);

        RecipePhotoContentResult result = service.GetPhotoContent(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.ContentFileMissing, result.Code);
        Assert.Null(result.Content);
        Assert.Contains(LogLevel.Warning, logger.Levels);
        Assert.False(photoRepository.DeleteWasCalled);
    }

    [Fact]
    public void GetPhotoContent_WhenStorageThrows_ShouldReturnStorageFailure()
    {
        RecipePhoto photo = CreatePhoto();
        GetPhotoContentRepository photoRepository = new(photo);
        GetPhotoContentStorage storage = new()
        {
            OpenException = new IOException("storage failed")
        };
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger: logger);

        RecipePhotoContentResult result = service.GetPhotoContent(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.StorageFailure, result.Code);
        Assert.Null(result.Content);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    [Fact]
    public void GetPhotoContent_WithInvalidId_ShouldReturnNotFoundWithoutRepositoryCall()
    {
        GetPhotoContentRepository photoRepository = new(CreatePhoto());
        GetPhotoContentStorage storage = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoContentResult result = service.GetPhotoContent(0);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.False(photoRepository.GetByIdWasCalled);
        Assert.False(photoRepository.GetByIdAndUserIdWasCalled);
        Assert.False(storage.OpenFinalFileWasCalled);
    }

    private static RecipePhotoService CreateService(
        GetPhotoContentRepository photoRepository,
        GetPhotoContentStorage storage,
        ICurrentUserContext? currentUserContext = null,
        RecordingLogger? logger = null)
    {
        return new RecipePhotoService(
            storage,
            photoRepository,
            new FakeRecipeRepository(),
            currentUserContext,
            new RecipePhotoValidator(),
            new RecipePhotoFileNameNormalizer(),
            new RecipePhotoOptions(),
            TimeProvider.System,
            logger ?? new RecordingLogger());
    }

    private static CurrentUserContext CreateCurrentUser(int userId)
    {
        CurrentUserContext context = new();
        context.SetCurrentUser(new User
        {
            Id = userId,
            Username = $"user-{userId}"
        });
        return context;
    }

    private static RecipePhoto CreatePhoto()
    {
        return new RecipePhoto
        {
            Id = 20,
            RecipeId = 10,
            StorageFileName =
                "33333333333333333333333333333333.jpg",
            OriginalFileName = @"C:\photos\prajitura.jpeg",
            ContentType = " IMAGE/JPEG ",
            FileSize = 3,
            CreatedAtUtc = new DateTime(
                2026,
                10,
                8,
                10,
                0,
                0,
                DateTimeKind.Utc),
            DisplayOrder = 0,
            Origin = RecipePhotoOrigin.UserUpload
        };
    }

    private sealed class GetPhotoContentRepository : IRecipePhotoRepository
    {
        private readonly RecipePhoto? _photo;

        public GetPhotoContentRepository(RecipePhoto? photo)
        {
            _photo = photo;
        }

        public int? OwnerUserId { get; set; }

        public bool GetByIdWasCalled { get; private set; }

        public bool GetByIdAndUserIdWasCalled { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public void Add(RecipePhoto photo)
        {
            throw new NotImplementedException();
        }

        public bool AddForUser(RecipePhoto photo, int userId)
        {
            throw new NotImplementedException();
        }

        public RecipePhoto? GetById(int photoId)
        {
            GetByIdWasCalled = true;
            return _photo?.Id == photoId ? _photo : null;
        }

        public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
        {
            GetByIdAndUserIdWasCalled = true;

            return _photo?.Id == photoId && OwnerUserId == userId
                ? _photo
                : null;
        }

        public List<RecipePhoto> GetByRecipeId(int recipeId)
        {
            throw new NotImplementedException();
        }

        public List<RecipePhoto> GetByRecipeIdAndUserId(
            int recipeId,
            int userId)
        {
            throw new NotImplementedException();
        }

        public bool Delete(int photoId)
        {
            DeleteWasCalled = true;
            throw new NotImplementedException();
        }

        public bool DeleteForUser(int photoId, int userId)
        {
            DeleteWasCalled = true;
            throw new NotImplementedException();
        }
    }

    private sealed class GetPhotoContentStorage : IRecipePhotoStorage
    {
        public Stream? Content { get; set; }

        public Exception? OpenException { get; set; }

        public bool OpenFinalFileWasCalled { get; private set; }

        public string? OpenedStorageFileName { get; private set; }

        public Task<StagedRecipePhotoUpload> StageUploadAsync(
            Stream content,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Stream OpenStagedUpload(StagedRecipePhotoUpload upload)
        {
            throw new NotImplementedException();
        }

        public string FinalizeUpload(
            StagedRecipePhotoUpload upload,
            RecipePhotoFormat format)
        {
            throw new NotImplementedException();
        }

        public void AbandonUpload(StagedRecipePhotoUpload upload)
        {
            throw new NotImplementedException();
        }

        public Stream? OpenFinalFile(string storageFileName)
        {
            OpenFinalFileWasCalled = true;
            OpenedStorageFileName = storageFileName;

            if (OpenException is not null)
            {
                throw OpenException;
            }

            return Content;
        }

        public bool FinalFileExists(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public StagedRecipePhotoDeletion? MoveToQuarantine(
            string storageFileName)
        {
            throw new NotImplementedException();
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            GetStagedUploads()
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            GetQuarantinedDeletions()
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            GetFinalFiles()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public TrackingMemoryStream(byte[] content)
            : base(content, writable: false)
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class RecordingLogger : ILogger<RecipePhotoService>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
        }
    }
}
