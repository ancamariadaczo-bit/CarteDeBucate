using Microsoft.Extensions.Logging;

public sealed class RecipePhotoReconciliationServiceStagingTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reconcile_WithUploadNewerThanThreshold_ShouldSkipIt()
    {
        StagingStorage storage = new();
        StagedRecipePhotoUpload upload = CreateUpload("new-upload");
        storage.StagedUploads.Add(
            new StagedRecipePhotoUploadInventoryItem(upload, UtcNow.UtcDateTime.AddHours(-23))
        );
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.SkippedTooNewCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Empty(storage.AbandonedUploads);
    }

    [Fact]
    public void Reconcile_WithUploadExactlyAtThreshold_ShouldSkipIt()
    {
        StagingStorage storage = new();
        StagedRecipePhotoUpload upload = CreateUpload("threshold-upload");
        storage.StagedUploads.Add(
            new StagedRecipePhotoUploadInventoryItem(upload, UtcNow.UtcDateTime.AddHours(-24))
        );
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.SkippedTooNewCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Empty(storage.AbandonedUploads);
    }

    [Fact]
    public void Reconcile_WithUploadOlderThanThreshold_ShouldDeleteIt()
    {
        StagingStorage storage = new();
        StagedRecipePhotoUpload upload = CreateUpload("old-upload");
        storage.StagedUploads.Add(
            new StagedRecipePhotoUploadInventoryItem(upload, UtcNow.UtcDateTime.AddHours(-25))
        );
        RecipePhotoReconciliationService service = CreateService(storage);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.DeletedCount);
        Assert.Equal(0, report.SkippedTooNewCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Same(upload, Assert.Single(storage.AbandonedUploads));
    }

    [Fact]
    public void Reconcile_WhenOneUploadCannotBeDeleted_ShouldContinue()
    {
        StagingStorage storage = new();
        StagedRecipePhotoUpload failingUpload = CreateUpload("failing-upload");
        StagedRecipePhotoUpload successfulUpload = CreateUpload("successful-upload");
        storage.StagedUploads.Add(
            new StagedRecipePhotoUploadInventoryItem(failingUpload, UtcNow.UtcDateTime.AddHours(-26))
        );
        storage.StagedUploads.Add(
            new StagedRecipePhotoUploadInventoryItem(successfulUpload, UtcNow.UtcDateTime.AddHours(-25))
        );
        storage.FailingUpload = failingUpload;
        RecordingLogger logger = new();
        RecipePhotoReconciliationService service = CreateService(storage, logger);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.DeletedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Same(successfulUpload, Assert.Single(storage.AbandonedUploads));
        Assert.Contains(LogLevel.Error, logger.Levels);
    }

    [Fact]
    public void Constructor_WithNegativeMinimumAge_ShouldThrow()
    {
        StagingStorage storage = new();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateService(storage, minimumAge: TimeSpan.FromHours(-1))
        );

        Assert.Equal("ReconciliationMinimumAge", exception.ParamName);
    }

    [Fact]
    public void Reconcile_WithMinimumAgeBeyondDateTimeRange_ShouldSkipUpload()
    {
        StagingStorage storage = new();
        StagedRecipePhotoUpload upload = CreateUpload("old-upload");
        storage.StagedUploads.Add(
            new StagedRecipePhotoUploadInventoryItem(upload, UtcNow.UtcDateTime.AddYears(-100))
        );
        RecipePhotoReconciliationService service = CreateService(storage, minimumAge: TimeSpan.MaxValue);

        RecipePhotoReconciliationReport report = service.Reconcile();

        Assert.Equal(1, report.SkippedTooNewCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Empty(storage.AbandonedUploads);
    }

    private static RecipePhotoReconciliationService CreateService(
        StagingStorage storage,
        RecordingLogger? logger = null,
        TimeSpan? minimumAge = null
    )
    {
        return new RecipePhotoReconciliationService(
            storage,
            new FakeRecipeRepository(),
            new EmptyPhotoRepository(),
            new RecipePhotoOptions { ReconciliationMinimumAge = minimumAge ?? TimeSpan.FromHours(24) },
            new FixedTimeProvider(UtcNow),
            logger ?? new RecordingLogger()
        );
    }

    private static StagedRecipePhotoUpload CreateUpload(string token)
    {
        return new StagedRecipePhotoUpload(token, length: 100);
    }

    private sealed class EmptyPhotoRepository : IRecipePhotoRepository
    {
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
            return [];
        }

        public List<RecipePhoto> GetByRecipeIdAndUserId(int recipeId, int userId)
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

    private sealed class StagingStorage : IRecipePhotoStorage
    {
        public List<StagedRecipePhotoUploadInventoryItem> StagedUploads { get; } = [];

        public List<StagedRecipePhotoUpload> AbandonedUploads { get; } = [];

        public StagedRecipePhotoUpload? FailingUpload { get; set; }

        public Task<StagedRecipePhotoUpload> StageUploadAsync(
            Stream content,
            long maximumBytes,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public Stream OpenStagedUpload(StagedRecipePhotoUpload upload)
        {
            throw new NotImplementedException();
        }

        public string FinalizeUpload(StagedRecipePhotoUpload upload, RecipePhotoFormat format)
        {
            throw new NotImplementedException();
        }

        public void AbandonUpload(StagedRecipePhotoUpload upload)
        {
            if (ReferenceEquals(upload, FailingUpload))
            {
                throw new IOException("cleanup failed");
            }

            AbandonedUploads.Add(upload);
        }

        public Stream? OpenFinalFile(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public bool FinalFileExists(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public StagedRecipePhotoDeletion? MoveToQuarantine(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public void RestoreFromQuarantine(StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public void DeleteFromQuarantine(StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem> GetStagedUploads()
        {
            return new RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>(
                StagedUploads,
                invalidEntryCount: 0
            );
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem> GetQuarantinedDeletions()
        {
            return new RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>(
                [],
                invalidEntryCount: 0
            );
        }

        public RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem> GetFinalFiles()
        {
            return new RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>(
                [],
                invalidEntryCount: 0
            );
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

    private sealed class RecordingLogger : ILogger<RecipePhotoReconciliationService>
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
            Func<TState, Exception?, string> formatter
        )
        {
            Levels.Add(logLevel);
        }
    }
}
