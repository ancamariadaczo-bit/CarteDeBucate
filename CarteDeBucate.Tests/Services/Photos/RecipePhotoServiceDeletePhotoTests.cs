using Microsoft.Extensions.Logging;

public sealed class RecipePhotoServiceDeletePhotoTests
{
    [Fact]
    public void DeletePhoto_WhenPhotoExists_ShouldQuarantineBeforeMetadataAndCleanupAfterward()
    {
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations);
        DeletePhotoStorage storage = new(operations);
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Equal(
            new[]
            {
                "move-to-quarantine",
                "delete-metadata",
                "delete-from-quarantine"
            },
            operations);
        Assert.Null(result.Photo);
        Assert.DoesNotContain(storage.DeletionToken, result.Message);
    }

    [Fact]
    public void DeletePhoto_WithInvalidId_ShouldNotCallStorageOrRepository()
    {
        List<string> operations = [];
        DeletePhotoRepository photoRepository = new(
            CreatePhoto(),
            operations);
        DeletePhotoStorage storage = new(operations);
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoResult result = service.DeletePhoto(0);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.False(photoRepository.GetByIdWasCalled);
        Assert.False(photoRepository.GetByIdAndUserIdWasCalled);
        Assert.Empty(operations);
    }

    [Fact]
    public void DeletePhoto_WithAnotherUsersPhoto_ShouldReturnNotFound()
    {
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations)
        {
            OwnerUserId = 9
        };
        DeletePhotoStorage storage = new(operations);
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            CreateCurrentUser(userId: 7));

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.True(photoRepository.GetByIdAndUserIdWasCalled);
        Assert.Empty(operations);
    }

    [Fact]
    public void DeletePhoto_WhenContentFileIsMissing_ShouldDeleteMetadataWithWarning()
    {
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations);
        DeletePhotoStorage storage = new(operations)
        {
            FileWasMissing = true
        };
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            RecipePhotoResultCode.DeletedMetadataFileWasMissing,
            result.Code);
        Assert.Equal(
            new[] { "move-to-quarantine", "delete-metadata" },
            operations);
    }

    [Fact]
    public void DeletePhoto_WhenPersistenceThrows_ShouldRestoreQuarantinedFile()
    {
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations)
        {
            DeleteException = new InvalidOperationException("database failed")
        };
        DeletePhotoStorage storage = new(operations);
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PersistenceFailure, result.Code);
        Assert.Equal(
            new[]
            {
                "move-to-quarantine",
                "delete-metadata",
                "restore-from-quarantine"
            },
            operations);
    }

    [Fact]
    public void DeletePhoto_WhenFilteredDeleteIsRefused_ShouldRestoreAndReturnNotFound()
    {
        const int userId = 7;
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations)
        {
            OwnerUserId = userId,
            DeleteForUserResult = false
        };
        DeletePhotoStorage storage = new(operations);
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            CreateCurrentUser(userId));

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoNotFound, result.Code);
        Assert.True(photoRepository.DeleteForUserWasCalled);
        Assert.Equal(
            new[]
            {
                "move-to-quarantine",
                "delete-metadata-for-user",
                "restore-from-quarantine"
            },
            operations);
    }

    [Fact]
    public void DeletePhoto_WhenFinalCleanupFails_ShouldReturnCleanupPending()
    {
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations);
        DeletePhotoStorage storage = new(operations)
        {
            DeleteFromQuarantineException =
                new IOException("cleanup failed")
        };
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger: logger);

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.CleanupPending, result.Code);
        Assert.Contains(LogLevel.Warning, logger.Levels);
        Assert.Equal(
            new[]
            {
                "move-to-quarantine",
                "delete-metadata",
                "delete-from-quarantine"
            },
            operations);
    }

    [Fact]
    public void DeletePhoto_WhenQuarantineFails_ShouldReturnStorageFailure()
    {
        List<string> operations = [];
        RecipePhoto photo = CreatePhoto();
        DeletePhotoRepository photoRepository = new(photo, operations);
        DeletePhotoStorage storage = new(operations)
        {
            MoveException = new IOException("storage failed")
        };
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger: logger);

        RecipePhotoResult result = service.DeletePhoto(photo.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.StorageFailure, result.Code);
        Assert.Equal(new[] { "move-to-quarantine" }, operations);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    private static RecipePhotoService CreateService(
        DeletePhotoRepository photoRepository,
        DeletePhotoStorage storage,
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
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
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

    private sealed class DeletePhotoRepository : IRecipePhotoRepository
    {
        private readonly RecipePhoto _photo;
        private readonly List<string> _operations;

        public DeletePhotoRepository(
            RecipePhoto photo,
            List<string> operations)
        {
            _photo = photo;
            _operations = operations;
        }

        public int? OwnerUserId { get; set; }

        public bool DeleteResult { get; set; } = true;

        public bool DeleteForUserResult { get; set; } = true;

        public Exception? DeleteException { get; set; }

        public bool GetByIdWasCalled { get; private set; }

        public bool GetByIdAndUserIdWasCalled { get; private set; }

        public bool DeleteForUserWasCalled { get; private set; }

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
            return _photo.Id == photoId ? _photo : null;
        }

        public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
        {
            GetByIdAndUserIdWasCalled = true;

            return _photo.Id == photoId && OwnerUserId == userId
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
            _operations.Add("delete-metadata");

            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            return DeleteResult;
        }

        public bool DeleteForUser(int photoId, int userId)
        {
            DeleteForUserWasCalled = true;
            _operations.Add("delete-metadata-for-user");

            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            return DeleteForUserResult && OwnerUserId == userId;
        }
    }

    private sealed class DeletePhotoStorage : IRecipePhotoStorage
    {
        private readonly List<string> _operations;

        public DeletePhotoStorage(List<string> operations)
        {
            _operations = operations;
        }

        public string DeletionToken { get; } =
            "22222222222222222222222222222222";

        public bool FileWasMissing { get; set; }

        public Exception? MoveException { get; set; }

        public Exception? DeleteFromQuarantineException { get; set; }

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
            throw new NotImplementedException();
        }

        public bool FinalFileExists(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public StagedRecipePhotoDeletion? MoveToQuarantine(
            string storageFileName)
        {
            _operations.Add("move-to-quarantine");

            if (MoveException is not null)
            {
                throw MoveException;
            }

            return FileWasMissing
                ? null
                : new StagedRecipePhotoDeletion(DeletionToken);
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            _operations.Add("restore-from-quarantine");
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            _operations.Add("delete-from-quarantine");

            if (DeleteFromQuarantineException is not null)
            {
                throw DeleteFromQuarantineException;
            }
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
