using Microsoft.Extensions.Logging;

public sealed class RecipePhotoDeletionCoordinatorTests
{
    [Fact]
    public void PrepareRecipeDeletion_WhenRecipeHasNoPhotos_ShouldReturnEmptyBatch()
    {
        CoordinatorPhotoRepository photoRepository = new();
        CoordinatorPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        PreparedRecipePhotoDeletionBatch? batch =
            service.PrepareRecipeDeletion(10);

        Assert.NotNull(batch);
        Assert.Equal(10, batch.RecipeId);
        Assert.Empty(batch.StagedDeletions);
        Assert.Empty(storage.Operations);
    }

    [Fact]
    public void PrepareRecipeDeletion_WithOnePhoto_ShouldQuarantineItsContent()
    {
        RecipePhoto photo = CreatePhoto(20, "first.jpg");
        CoordinatorPhotoRepository photoRepository = new()
        {
            Photos = [photo]
        };
        CoordinatorPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        PreparedRecipePhotoDeletionBatch? batch =
            service.PrepareRecipeDeletion(10);

        Assert.NotNull(batch);
        Assert.Single(batch.StagedDeletions);
        Assert.Equal(
            new[] { "move:first.jpg" },
            storage.Operations);
        Assert.True(photoRepository.GetByRecipeIdWasCalled);
    }

    [Fact]
    public void PrepareRecipeDeletion_WithMultiplePhotos_ShouldQuarantineEveryContentFile()
    {
        CoordinatorPhotoRepository photoRepository = new()
        {
            Photos =
            [
                CreatePhoto(20, "first.jpg"),
                CreatePhoto(21, "second.png"),
                CreatePhoto(22, "third.webp")
            ]
        };
        CoordinatorPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage);

        PreparedRecipePhotoDeletionBatch? batch =
            service.PrepareRecipeDeletion(10);

        Assert.NotNull(batch);
        Assert.Equal(3, batch.StagedDeletions.Count);
        Assert.Equal(
            new[]
            {
                "move:first.jpg",
                "move:second.png",
                "move:third.webp"
            },
            storage.Operations);
    }

    [Fact]
    public void PrepareRecipeDeletion_WhenContentIsMissing_ShouldContinueAndLogWarning()
    {
        CoordinatorPhotoRepository photoRepository = new()
        {
            Photos =
            [
                CreatePhoto(20, "missing.jpg"),
                CreatePhoto(21, "existing.png")
            ]
        };
        CoordinatorPhotoStorage storage = new();
        storage.MissingStorageFileNames.Add("missing.jpg");
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger);

        PreparedRecipePhotoDeletionBatch? batch =
            service.PrepareRecipeDeletion(10);

        Assert.NotNull(batch);
        Assert.Single(batch.StagedDeletions);
        Assert.Equal(
            new[]
            {
                "move:missing.jpg",
                "move:existing.png"
            },
            storage.Operations);
        Assert.Contains(LogLevel.Warning, logger.Levels);
    }

    [Fact]
    public void PrepareRecipeDeletion_WhenMoveFails_ShouldRestoreMovedContentAndReturnNull()
    {
        CoordinatorPhotoRepository photoRepository = new()
        {
            Photos =
            [
                CreatePhoto(20, "first.jpg"),
                CreatePhoto(21, "second.png"),
                CreatePhoto(22, "third.webp")
            ]
        };
        CoordinatorPhotoStorage storage = new()
        {
            MoveFailureStorageFileName = "second.png"
        };
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger);

        PreparedRecipePhotoDeletionBatch? batch =
            service.PrepareRecipeDeletion(10);
        string firstRestoreOperation =
            $"restore:{CoordinatorPhotoStorage.TokenFor("first.jpg")}";

        Assert.Null(batch);
        Assert.Equal(
            new[]
            {
                "move:first.jpg",
                "move:second.png",
                firstRestoreOperation
            },
            storage.Operations);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    [Fact]
    public void PrepareRecipeDeletion_WhenRollbackPartiallyFails_ShouldAttemptEveryRestore()
    {
        CoordinatorPhotoRepository photoRepository = new()
        {
            Photos =
            [
                CreatePhoto(20, "first.jpg"),
                CreatePhoto(21, "second.png"),
                CreatePhoto(22, "failure.webp")
            ]
        };
        CoordinatorPhotoStorage storage = new()
        {
            MoveFailureStorageFileName = "failure.webp"
        };
        storage.RestoreFailureTokens.Add(
            CoordinatorPhotoStorage.TokenFor("first.jpg"));
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            photoRepository,
            storage,
            logger);

        PreparedRecipePhotoDeletionBatch? batch =
            service.PrepareRecipeDeletion(10);

        Assert.Null(batch);
        Assert.Contains(
            $"restore:{CoordinatorPhotoStorage.TokenFor("first.jpg")}",
            storage.Operations);
        Assert.Contains(
            $"restore:{CoordinatorPhotoStorage.TokenFor("second.png")}",
            storage.Operations);
        Assert.Contains(LogLevel.Warning, logger.Levels);
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    [Fact]
    public void ConfirmRecipeDeletion_ShouldDeleteEveryQuarantinedItem()
    {
        CoordinatorPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            new CoordinatorPhotoRepository(),
            storage);
        PreparedRecipePhotoDeletionBatch batch = CreateBatch();

        service.ConfirmRecipeDeletion(batch);

        Assert.Equal(
            new[] { "delete:token-one", "delete:token-two" },
            storage.Operations);
    }

    [Fact]
    public void ConfirmRecipeDeletion_WhenOneDeleteFails_ShouldContinueAndLogWarning()
    {
        CoordinatorPhotoStorage storage = new();
        storage.DeleteFailureTokens.Add("token-one");
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            new CoordinatorPhotoRepository(),
            storage,
            logger);

        service.ConfirmRecipeDeletion(CreateBatch());

        Assert.Equal(
            new[] { "delete:token-one", "delete:token-two" },
            storage.Operations);
        Assert.Contains(LogLevel.Warning, logger.Levels);
    }

    [Fact]
    public void RestoreRecipeDeletion_WhenOneRestoreFails_ShouldContinueAndLogWarning()
    {
        CoordinatorPhotoStorage storage = new();
        storage.RestoreFailureTokens.Add("token-one");
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            new CoordinatorPhotoRepository(),
            storage,
            logger);

        service.RestoreRecipeDeletion(CreateBatch());

        Assert.Equal(
            new[] { "restore:token-one", "restore:token-two" },
            storage.Operations);
        Assert.Contains(LogLevel.Warning, logger.Levels);
    }

    private static RecipePhotoService CreateService(
        CoordinatorPhotoRepository photoRepository,
        CoordinatorPhotoStorage storage,
        RecordingLogger? logger = null)
    {
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes =
            [
                new Recipe
                {
                    Id = 10,
                    Name = "Recipe 10"
                }
            ]
        };

        return new RecipePhotoService(
            storage,
            photoRepository,
            recipeRepository,
            null,
            new RecipePhotoValidator(),
            new RecipePhotoFileNameNormalizer(),
            new RecipePhotoOptions(),
            TimeProvider.System,
            logger ?? new RecordingLogger());
    }

    private static RecipePhoto CreatePhoto(
        int id,
        string storageFileName)
    {
        return new RecipePhoto
        {
            Id = id,
            RecipeId = 10,
            StorageFileName = storageFileName
        };
    }

    private static PreparedRecipePhotoDeletionBatch CreateBatch()
    {
        return new PreparedRecipePhotoDeletionBatch(
            10,
            [
                new StagedRecipePhotoDeletion("token-one"),
                new StagedRecipePhotoDeletion("token-two")
            ]);
    }

    private sealed class CoordinatorPhotoRepository : IRecipePhotoRepository
    {
        public List<RecipePhoto> Photos { get; set; } = [];

        public bool GetByRecipeIdWasCalled { get; private set; }

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
            GetByRecipeIdWasCalled = true;

            return Photos
                .Where(photo => photo.RecipeId == recipeId)
                .ToList();
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

    private sealed class CoordinatorPhotoStorage : IRecipePhotoStorage
    {
        public List<string> Operations { get; } = [];

        public HashSet<string> MissingStorageFileNames { get; } = [];

        public HashSet<string> DeleteFailureTokens { get; } = [];

        public HashSet<string> RestoreFailureTokens { get; } = [];

        public string? MoveFailureStorageFileName { get; set; }

        public static string TokenFor(string storageFileName)
        {
            return $"token-{storageFileName}";
        }

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
            Operations.Add($"move:{storageFileName}");

            if (storageFileName == MoveFailureStorageFileName)
            {
                throw new IOException("move failed");
            }

            if (MissingStorageFileNames.Contains(storageFileName))
            {
                return null;
            }

            return new StagedRecipePhotoDeletion(
                TokenFor(storageFileName));
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            Operations.Add($"restore:{deletion.Token}");

            if (RestoreFailureTokens.Contains(deletion.Token))
            {
                throw new IOException("restore failed");
            }
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            Operations.Add($"delete:{deletion.Token}");

            if (DeleteFailureTokens.Contains(deletion.Token))
            {
                throw new IOException("delete failed");
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
