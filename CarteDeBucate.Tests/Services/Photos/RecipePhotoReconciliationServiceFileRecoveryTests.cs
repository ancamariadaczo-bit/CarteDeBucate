using Microsoft.Extensions.Logging;

public sealed class RecipePhotoReconciliationServiceFileRecoveryTests
{
    private const string KnownStorageFileName =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";
    private const string UnreferencedStorageFileName =
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png";
    private const string OrphanedStorageFileName =
        "cccccccccccccccccccccccccccccccc.webp";
    private const string NewOrphanStorageFileName =
        "dddddddddddddddddddddddddddddddd.jpg";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reconcile_WithQuarantineEntryNewerThanThreshold_ShouldSkipIt()
    {
        ReconciliationStorage storage = new();
        StagedRecipePhotoDeletionInventoryItem item = CreateQuarantineItem(
            KnownStorageFileName,
            UtcNow.UtcDateTime.AddHours(-23));
        storage.QuarantinedDeletions.Add(item);
        RecipePhotoReconciliationService service = CreateService(
            storage,
            [KnownStorageFileName]);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.SkippedTooNewCount);
        Assert.Equal(0, report.RestoredCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Empty(storage.RestoredDeletions);
        Assert.Empty(storage.DeletedDeletions);
    }

    [Fact]
    public void Reconcile_WithReferencedQuarantineEntry_ShouldRestoreIt()
    {
        ReconciliationStorage storage = new();
        StagedRecipePhotoDeletionInventoryItem item = CreateQuarantineItem(
            KnownStorageFileName,
            UtcNow.UtcDateTime.AddHours(-25));
        storage.QuarantinedDeletions.Add(item);
        RecipePhotoReconciliationService service = CreateService(
            storage,
            [KnownStorageFileName]);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.RestoredCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Same(
            item.Deletion,
            Assert.Single(storage.RestoredDeletions));
        Assert.Empty(storage.DeletedDeletions);
    }

    [Fact]
    public void Reconcile_WithUnreferencedQuarantineEntry_ShouldDeleteIt()
    {
        ReconciliationStorage storage = new();
        StagedRecipePhotoDeletionInventoryItem item = CreateQuarantineItem(
            UnreferencedStorageFileName,
            UtcNow.UtcDateTime.AddHours(-25));
        storage.QuarantinedDeletions.Add(item);
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.DeletedCount);
        Assert.Equal(0, report.RestoredCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Same(
            item.Deletion,
            Assert.Single(storage.DeletedDeletions));
        Assert.Empty(storage.RestoredDeletions);
    }

    [Fact]
    public void Reconcile_WithInvalidQuarantineDescriptor_ShouldReportFailure()
    {
        ReconciliationStorage storage = new()
        {
            InvalidQuarantineEntryCount = 1
        };
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.RestoredCount);
        Assert.Empty(storage.RestoredDeletions);
        Assert.Empty(storage.DeletedDeletions);
    }

    [Fact]
    public void Reconcile_WhenRestoreCollidesWithFinalFile_ShouldReportFailure()
    {
        ReconciliationStorage storage = new();
        StagedRecipePhotoDeletionInventoryItem item = CreateQuarantineItem(
            KnownStorageFileName,
            UtcNow.UtcDateTime.AddHours(-25));
        storage.QuarantinedDeletions.Add(item);
        storage.RestoreFailureToken = item.Deletion.Token;
        RecordingLogger logger = new();
        RecipePhotoReconciliationService service = CreateService(
            storage,
            [KnownStorageFileName],
            logger);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.RestoredCount);
        Assert.Empty(storage.RestoredDeletions);
        Assert.Contains(LogLevel.Error, logger.Levels);
        Assert.All(logger.Exceptions, exception => Assert.Null(exception));
        Assert.DoesNotContain(
            "/private/photo-storage",
            string.Join(" ", logger.Messages));
    }

    [Fact]
    public void Reconcile_WhenOneQuarantineOperationFails_ShouldContinue()
    {
        ReconciliationStorage storage = new();
        StagedRecipePhotoDeletionInventoryItem failingItem =
            CreateQuarantineItem(
                UnreferencedStorageFileName,
                UtcNow.UtcDateTime.AddHours(-26));
        StagedRecipePhotoDeletionInventoryItem successfulItem =
            CreateQuarantineItem(
                KnownStorageFileName,
                UtcNow.UtcDateTime.AddHours(-25));
        storage.QuarantinedDeletions.Add(failingItem);
        storage.QuarantinedDeletions.Add(successfulItem);
        storage.DeleteFailureToken = failingItem.Deletion.Token;
        RecipePhotoReconciliationService service = CreateService(
            storage,
            [KnownStorageFileName]);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(1, report.RestoredCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Same(
            successfulItem.Deletion,
            Assert.Single(storage.RestoredDeletions));
    }

    [Fact]
    public void Reconcile_WithReferencedFinalFile_ShouldNotReportIt()
    {
        ReconciliationStorage storage = new();
        storage.FinalFiles.Add(CreateFinalFile(
            KnownStorageFileName,
            UtcNow.UtcDateTime.AddHours(-25)));
        RecipePhotoReconciliationService service = CreateService(
            storage,
            [KnownStorageFileName]);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(0, report.OrphanedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, storage.MutationCount);
    }

    [Fact]
    public void Reconcile_WithOrphanedFinalFile_ShouldReportWithoutChangingIt()
    {
        ReconciliationStorage storage = new();
        storage.FinalFiles.Add(CreateFinalFile(
            OrphanedStorageFileName,
            UtcNow.UtcDateTime.AddHours(-25)));
        RecordingLogger logger = new();
        RecipePhotoReconciliationService service = CreateService(
            storage,
            logger: logger);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.OrphanedCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, storage.MutationCount);
        Assert.Contains(LogLevel.Warning, logger.Levels);
    }

    [Fact]
    public void Reconcile_WithFinalFileNewerThanThreshold_ShouldSkipIt()
    {
        ReconciliationStorage storage = new();
        storage.FinalFiles.Add(CreateFinalFile(
            NewOrphanStorageFileName,
            UtcNow.UtcDateTime.AddHours(-23)));
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.SkippedTooNewCount);
        Assert.Equal(0, report.OrphanedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, storage.MutationCount);
    }

    [Fact]
    public void Reconcile_WithInvalidFinalFileName_ShouldReportFailure()
    {
        ReconciliationStorage storage = new()
        {
            InvalidFinalFileEntryCount = 1
        };
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.OrphanedCount);
        Assert.Equal(0, storage.MutationCount);
    }

    private static RecipePhotoReconciliationService CreateService(
        ReconciliationStorage storage,
        IReadOnlyList<string>? knownStorageFileNames = null,
        RecordingLogger? logger = null)
    {
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes =
            [
                new Recipe { Id = 1, Name = "Recipe" }
            ]
        };

        return new RecipePhotoReconciliationService(
            storage,
            recipeRepository,
            new ReconciliationPhotoRepository(
                knownStorageFileNames ?? []),
            new RecipePhotoOptions
            {
                ReconciliationMinimumAge = TimeSpan.FromHours(24)
            },
            new FixedTimeProvider(UtcNow),
            logger ?? new RecordingLogger());
    }

    private static StagedRecipePhotoDeletionInventoryItem
        CreateQuarantineItem(
            string storageFileName,
            DateTime quarantinedAtUtc)
    {
        return new StagedRecipePhotoDeletionInventoryItem(
            new StagedRecipePhotoDeletion(
                Guid.NewGuid().ToString("N")),
            storageFileName,
            quarantinedAtUtc);
    }

    private static RecipePhotoStorageFileInventoryItem CreateFinalFile(
        string storageFileName,
        DateTime lastModifiedAtUtc)
    {
        return new RecipePhotoStorageFileInventoryItem(
            storageFileName,
            lastModifiedAtUtc);
    }

    private sealed class ReconciliationPhotoRepository :
        IRecipePhotoRepository
    {
        private readonly IReadOnlyList<string> _storageFileNames;

        public ReconciliationPhotoRepository(
            IReadOnlyList<string> storageFileNames)
        {
            _storageFileNames = storageFileNames;
        }

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
            return _storageFileNames
                .Select(storageFileName => new RecipePhoto
                {
                    RecipeId = recipeId,
                    StorageFileName = storageFileName
                })
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

    private sealed class ReconciliationStorage : IRecipePhotoStorage
    {
        public List<StagedRecipePhotoDeletionInventoryItem>
            QuarantinedDeletions { get; } = [];

        public List<RecipePhotoStorageFileInventoryItem> FinalFiles
        {
            get;
        } = [];

        public List<StagedRecipePhotoDeletion> RestoredDeletions
        {
            get;
        } = [];

        public List<StagedRecipePhotoDeletion> DeletedDeletions
        {
            get;
        } = [];

        public int InvalidQuarantineEntryCount { get; set; }

        public int InvalidFinalFileEntryCount { get; set; }

        public string? RestoreFailureToken { get; set; }

        public string? DeleteFailureToken { get; set; }

        public int MutationCount =>
            RestoredDeletions.Count + DeletedDeletions.Count;

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
            throw new NotImplementedException();
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            if (deletion.Token == RestoreFailureToken)
            {
                throw new IOException(
                    "restore failed at /private/photo-storage/known.jpg");
            }

            RestoredDeletions.Add(deletion);
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            if (deletion.Token == DeleteFailureToken)
            {
                throw new IOException("delete failed");
            }

            DeletedDeletions.Add(deletion);
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            GetStagedUploads()
        {
            return new RecipePhotoStorageInventory<
                StagedRecipePhotoUploadInventoryItem>(
                    [],
                    invalidEntryCount: 0);
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            GetQuarantinedDeletions()
        {
            return new RecipePhotoStorageInventory<
                StagedRecipePhotoDeletionInventoryItem>(
                    QuarantinedDeletions,
                    InvalidQuarantineEntryCount);
        }

        public RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            GetFinalFiles()
        {
            return new RecipePhotoStorageInventory<
                RecipePhotoStorageFileInventoryItem>(
                    FinalFiles,
                    InvalidFinalFileEntryCount);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed class RecordingLogger :
        ILogger<RecipePhotoReconciliationService>
    {
        public List<LogLevel> Levels { get; } = [];

        public List<string> Messages { get; } = [];

        public List<Exception?> Exceptions { get; } = [];

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
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}
