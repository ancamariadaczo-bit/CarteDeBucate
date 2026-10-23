using Microsoft.Extensions.Logging;

public sealed class RecipePhotoReconciliationServiceInventoryTests
{
    [Fact]
    public void BuildKnownStorageFileNames_ShouldReadEveryRecipeAndPhoto()
    {
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes =
            [
                new Recipe { Id = 10, Name = "Recipe 10" },
                new Recipe { Id = 20, Name = "Recipe 20" }
            ]
        };
        InventoryPhotoRepository photoRepository = new();
        photoRepository.PhotosByRecipeId[10] =
        [
            CreatePhoto(10, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg"),
            CreatePhoto(10, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png")
        ];
        photoRepository.PhotosByRecipeId[20] =
        [
            CreatePhoto(20, "cccccccccccccccccccccccccccccccc.webp")
        ];
        InventoryStorage storage = new();
        RecipePhotoReconciliationService service = CreateService(
            storage,
            recipeRepository,
            photoRepository);

        HashSet<string> storageFileNames =
            service.BuildKnownStorageFileNames();

        Assert.True(recipeRepository.GetAllRecipesWasCalled);
        Assert.Equal(new[] { 10, 20 }, photoRepository.RequestedRecipeIds);
        Assert.Equal(3, storageFileNames.Count);
        Assert.Contains(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg",
            storageFileNames);
        Assert.Contains(
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png",
            storageFileNames);
        Assert.Contains(
            "cccccccccccccccccccccccccccccccc.webp",
            storageFileNames);
        Assert.False(storage.WasAccessed);
    }

    [Fact]
    public void Reconcile_WhenRecipeMetadataCannotBeRead_ShouldStopBeforeStorageAccess()
    {
        FakeRecipeRepository recipeRepository = new()
        {
            ExceptionToThrowOnGetAllRecipes =
                new InvalidOperationException("database failed")
        };
        InventoryPhotoRepository photoRepository = new();
        InventoryStorage storage = new();
        RecordingLogger logger = new();
        RecipePhotoReconciliationService service = CreateService(
            storage,
            recipeRepository,
            photoRepository,
            logger);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.FailedCount);
        Assert.True(recipeRepository.GetAllRecipesWasCalled);
        Assert.Empty(photoRepository.RequestedRecipeIds);
        Assert.False(storage.WasAccessed);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    [Fact]
    public void Reconcile_WhenPhotoMetadataCannotBeRead_ShouldStopBeforeStorageAccess()
    {
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes =
            [
                new Recipe { Id = 10, Name = "Recipe 10" },
                new Recipe { Id = 20, Name = "Recipe 20" }
            ]
        };
        InventoryPhotoRepository photoRepository = new()
        {
            FailureRecipeId = 20
        };
        photoRepository.PhotosByRecipeId[10] =
        [
            CreatePhoto(10, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg")
        ];
        InventoryStorage storage = new();
        RecordingLogger logger = new();
        RecipePhotoReconciliationService service = CreateService(
            storage,
            recipeRepository,
            photoRepository,
            logger);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(new[] { 10, 20 }, photoRepository.RequestedRecipeIds);
        Assert.False(storage.WasAccessed);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    private static RecipePhotoReconciliationService CreateService(
        InventoryStorage storage,
        FakeRecipeRepository recipeRepository,
        InventoryPhotoRepository photoRepository,
        RecordingLogger? logger = null)
    {
        return new RecipePhotoReconciliationService(
            storage,
            recipeRepository,
            photoRepository,
            new RecipePhotoOptions(),
            TimeProvider.System,
            logger ?? new RecordingLogger());
    }

    private static RecipePhoto CreatePhoto(
        int recipeId,
        string storageFileName)
    {
        return new RecipePhoto
        {
            RecipeId = recipeId,
            StorageFileName = storageFileName
        };
    }

    private sealed class InventoryPhotoRepository : IRecipePhotoRepository
    {
        public Dictionary<int, List<RecipePhoto>> PhotosByRecipeId
        {
            get;
        } = [];

        public List<int> RequestedRecipeIds { get; } = [];

        public int? FailureRecipeId { get; set; }

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
            throw new NotImplementedException();
        }

        public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
        {
            throw new NotImplementedException();
        }

        public List<RecipePhoto> GetByRecipeId(int recipeId)
        {
            RequestedRecipeIds.Add(recipeId);

            if (recipeId == FailureRecipeId)
            {
                throw new InvalidOperationException("database failed");
            }

            return PhotosByRecipeId.TryGetValue(
                recipeId,
                out List<RecipePhoto>? photos)
                    ? photos
                    : [];
        }

        public List<RecipePhoto> GetByRecipeIdAndUserId(
            int recipeId,
            int userId)
        {
            throw new NotImplementedException();
        }

        public bool Delete(int photoId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteForUser(int photoId, int userId)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class InventoryStorage : IRecipePhotoStorage
    {
        public bool WasAccessed { get; private set; }

        public Task<StagedRecipePhotoUpload> StageUploadAsync(
            Stream content,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public Stream OpenStagedUpload(StagedRecipePhotoUpload upload)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public string FinalizeUpload(
            StagedRecipePhotoUpload upload,
            RecipePhotoFormat format)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public void AbandonUpload(StagedRecipePhotoUpload upload)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public Stream? OpenFinalFile(string storageFileName)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public bool FinalFileExists(string storageFileName)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public StagedRecipePhotoDeletion? MoveToQuarantine(
            string storageFileName)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            GetStagedUploads()
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            GetQuarantinedDeletions()
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            GetFinalFiles()
        {
            WasAccessed = true;
            throw new NotImplementedException();
        }
    }

    private sealed class RecordingLogger :
        ILogger<RecipePhotoReconciliationService>
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
