using Microsoft.Extensions.Logging;

public sealed class RecipePhotoServiceAddPhotoTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 8, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddPhotoAsync_WithValidPhoto_ShouldPersistNormalizedMetadata()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new()
        {
            Photos =
            [
                new RecipePhoto
                {
                    Id = 1,
                    RecipeId = 10,
                    DisplayOrder = 4
                }
            ]
        };
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        byte[] content = JpegContent();
        using MemoryStream source = new(content);

        RecipePhotoResult result = await service.AddPhotoAsync(
            recipeId: 10,
            source,
            originalFileName: @"C:\photos\prajitura.jpeg",
            declaredContentType: " IMAGE/JPEG ",
            declaredLength: 1,
            RecipePhotoOrigin.UserUpload);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.NotNull(result.Photo);
        Assert.True(photoRepository.AddWasCalled);
        Assert.False(photoRepository.AddForUserWasCalled);
        RecipePhoto savedPhoto = Assert.IsType<RecipePhoto>(
            photoRepository.AddedPhoto);
        Assert.Equal(10, savedPhoto.RecipeId);
        Assert.Equal(storage.FinalStorageFileName, savedPhoto.StorageFileName);
        Assert.Equal("prajitura.jpg", savedPhoto.OriginalFileName);
        Assert.Equal("image/jpeg", savedPhoto.ContentType);
        Assert.Equal(content.Length, savedPhoto.FileSize);
        Assert.Equal(CreatedAt.UtcDateTime, savedPhoto.CreatedAtUtc);
        Assert.Equal(5, savedPhoto.DisplayOrder);
        Assert.Equal(RecipePhotoOrigin.UserUpload, savedPhoto.Origin);
        Assert.Equal(savedPhoto.Id, result.Photo.Id);
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task AddPhotoAsync_WithNonSeekableStream_ShouldUseStagedContent()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using NonSeekableReadStream source = new(JpegContent());

        RecipePhotoResult result = await service.AddPhotoAsync(
            10,
            source,
            "photo.jpg",
            "image/jpeg",
            declaredLength: null,
            RecipePhotoOrigin.RecipeSource);

        Assert.True(result.IsSuccess);
        Assert.False(source.IsDisposed);
        Assert.True(storage.OpenStagedUploadWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenPhotoLimitIsReached_ShouldNotStageContent()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new()
        {
            Photos = Enumerable.Range(1, 20)
                .Select(index => new RecipePhoto
                {
                    Id = index,
                    RecipeId = 10,
                    DisplayOrder = index - 1
                })
                .ToList()
        };
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PhotoLimitReached, result.Code);
        Assert.False(storage.StageUploadWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WithAnotherUsersRecipe_ShouldReturnNotFound()
    {
        FakeRecipeRepository recipeRepository =
            CreateRecipeRepository(userId: 9);
        AddPhotoRepository photoRepository = new() { OwnerUserId = 9 };
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage,
            CreateCurrentUser(userId: 7));
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.RecipeNotFound, result.Code);
        Assert.False(storage.StageUploadWasCalled);
    }

    [Theory]
    [MemberData(nameof(InvalidPhotoContent))]
    public async Task AddPhotoAsync_WithInvalidContent_ShouldAbandonStaging(
        byte[] content,
        string declaredContentType,
        RecipePhotoResultCode expectedCode)
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(content);

        RecipePhotoResult result = await service.AddPhotoAsync(
            10,
            source,
            "photo.jpg",
            declaredContentType,
            content.Length,
            RecipePhotoOrigin.UserUpload);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Code);
        Assert.True(storage.AbandonUploadWasCalled);
        Assert.False(storage.FinalizeUploadWasCalled);
        Assert.False(photoRepository.AddWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WithDeclaredLengthOverLimit_ShouldRejectBeforeStaging()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await service.AddPhotoAsync(
            10,
            source,
            "photo.jpg",
            "image/jpeg",
            declaredLength: 6,
            RecipePhotoOrigin.UserUpload);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.FileTooLarge, result.Code);
        Assert.False(storage.StageUploadWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WithActualLengthOverLimit_ShouldUseMeasuredBytes()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        byte[] oversizedContent = [.. JpegContent(), 0x02];
        using MemoryStream source = new(oversizedContent);

        RecipePhotoResult result = await service.AddPhotoAsync(
            10,
            source,
            "photo.jpg",
            "image/jpeg",
            declaredLength: 1,
            RecipePhotoOrigin.UserUpload);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.FileTooLarge, result.Code);
        Assert.True(storage.StageUploadWasCalled);
        Assert.False(photoRepository.AddWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenCancelledDuringStaging_ShouldPropagateCancellation()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AddValidPhotoAsync(service, source, cancellation.Token));

        Assert.False(photoRepository.AddWasCalled);
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenStorageFails_ShouldReturnStorageFailure()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        AddPhotoStorage storage = new()
        {
            FinalizeException = new IOException("finalization failed")
        };
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.StorageFailure, result.Code);
        Assert.True(storage.StageUploadWasCalled);
        Assert.True(storage.AbandonUploadWasCalled);
        Assert.False(photoRepository.AddWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenPersistenceFails_ShouldRemoveFinalFile()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new()
        {
            AddException = new InvalidOperationException("database failed")
        };
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PersistenceFailure, result.Code);
        Assert.True(storage.MoveToQuarantineWasCalled);
        Assert.True(storage.DeleteFromQuarantineWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenAddForUserIsRefused_ShouldReturnRecipeNotFoundAndCleanup()
    {
        const int userId = 7;
        FakeRecipeRepository recipeRepository =
            CreateRecipeRepository(userId);
        AddPhotoRepository photoRepository = new()
        {
            OwnerUserId = userId,
            AddForUserResult = false
        };
        AddPhotoStorage storage = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage,
            CreateCurrentUser(userId));
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.RecipeNotFound, result.Code);
        Assert.True(photoRepository.AddForUserWasCalled);
        Assert.True(storage.MoveToQuarantineWasCalled);
        Assert.True(storage.DeleteFromQuarantineWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenCompensatingCleanupFails_ShouldLogWarningAndKeepPersistenceFailure()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new()
        {
            AddException = new InvalidOperationException("database failed")
        };
        AddPhotoStorage storage = new()
        {
            DeleteFromQuarantineException =
                new IOException("cleanup failed")
        };
        RecordingLogger logger = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage,
            logger: logger);
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.PersistenceFailure, result.Code);
        Assert.Contains(LogLevel.Warning, logger.Levels);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenCancelledAfterStaging_ShouldAbandonAndPropagateCancellation()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        using CancellationTokenSource cancellation = new();
        AddPhotoStorage storage = new()
        {
            AfterStage = cancellation.Cancel
        };
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => AddValidPhotoAsync(
                service,
                source,
                cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(storage.AbandonUploadWasCalled);
        Assert.False(storage.FinalizeUploadWasCalled);
        Assert.False(photoRepository.AddWasCalled);
    }

    [Fact]
    public async Task AddPhotoAsync_WhenCancelledAfterFinalization_ShouldStillPersistMetadata()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        AddPhotoRepository photoRepository = new();
        using CancellationTokenSource cancellation = new();
        AddPhotoStorage storage = new()
        {
            AfterFinalize = cancellation.Cancel
        };
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            storage);
        using MemoryStream source = new(JpegContent());

        RecipePhotoResult result = await AddValidPhotoAsync(
            service,
            source,
            cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(result.IsSuccess);
        Assert.True(photoRepository.AddWasCalled);
        Assert.False(storage.MoveToQuarantineWasCalled);
    }

    public static TheoryData<byte[], string, RecipePhotoResultCode>
        InvalidPhotoContent => new()
        {
            {
                [],
                "image/jpeg",
                RecipePhotoResultCode.EmptyContent
            },
            {
                [0x01, 0x02, 0x03],
                "image/jpeg",
                RecipePhotoResultCode.InvalidContent
            }
        };

    private static async Task<RecipePhotoResult> AddValidPhotoAsync(
        RecipePhotoService service,
        Stream source,
        CancellationToken cancellationToken = default)
    {
        return await service.AddPhotoAsync(
            10,
            source,
            "photo.jpeg",
            "image/jpeg",
            declaredLength: null,
            RecipePhotoOrigin.UserUpload,
            cancellationToken);
    }

    private static RecipePhotoService CreateService(
        FakeRecipeRepository recipeRepository,
        AddPhotoRepository photoRepository,
        AddPhotoStorage storage,
        ICurrentUserContext? currentUserContext = null,
        RecordingLogger? logger = null)
    {
        return new RecipePhotoService(
            storage,
            photoRepository,
            recipeRepository,
            currentUserContext,
            new RecipePhotoValidator(),
            new RecipePhotoFileNameNormalizer(),
            new RecipePhotoOptions
            {
                MaxFileSizeBytes = 5,
                MaxPhotosPerRecipe = 20
            },
            new FixedTimeProvider(CreatedAt),
            logger ?? new RecordingLogger());
    }

    private static FakeRecipeRepository CreateRecipeRepository(
        int? userId = null)
    {
        return new FakeRecipeRepository
        {
            Recipes =
            [
                new Recipe
                {
                    Id = 10,
                    Name = "Recipe 10",
                    UserId = userId
                }
            ]
        };
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

    private static byte[] JpegContent()
    {
        return [0xFF, 0xD8, 0xFF, 0xE0, 0x01];
    }

    private sealed class AddPhotoRepository : IRecipePhotoRepository
    {
        public List<RecipePhoto> Photos { get; set; } = [];

        public int? OwnerUserId { get; set; }

        public bool AddForUserResult { get; set; } = true;

        public Exception? AddException { get; set; }

        public bool AddWasCalled { get; private set; }

        public bool AddForUserWasCalled { get; private set; }

        public RecipePhoto? AddedPhoto { get; private set; }

        public void Add(RecipePhoto photo)
        {
            AddWasCalled = true;
            AddedPhoto = photo;

            if (AddException is not null)
            {
                throw AddException;
            }

            photo.Id = 101;
            Photos.Add(photo);
        }

        public bool AddForUser(RecipePhoto photo, int userId)
        {
            AddForUserWasCalled = true;
            AddedPhoto = photo;

            if (AddException is not null)
            {
                throw AddException;
            }

            if (!AddForUserResult || OwnerUserId != userId)
            {
                return false;
            }

            photo.Id = 101;
            Photos.Add(photo);
            return true;
        }

        public RecipePhoto? GetById(int photoId)
        {
            return Photos.FirstOrDefault(photo => photo.Id == photoId);
        }

        public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
        {
            return OwnerUserId == userId ? GetById(photoId) : null;
        }

        public List<RecipePhoto> GetByRecipeId(int recipeId)
        {
            return Photos
                .Where(photo => photo.RecipeId == recipeId)
                .ToList();
        }

        public List<RecipePhoto> GetByRecipeIdAndUserId(
            int recipeId,
            int userId)
        {
            return OwnerUserId == userId
                ? GetByRecipeId(recipeId)
                : [];
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

    private sealed class AddPhotoStorage : IRecipePhotoStorage
    {
        private static readonly string StagedToken =
            "11111111111111111111111111111111";
        private static readonly string DeletionToken =
            "22222222222222222222222222222222";

        private byte[] _stagedContent = [];

        public Exception? StageException { get; set; }

        public Exception? FinalizeException { get; set; }

        public Exception? DeleteFromQuarantineException { get; set; }

        public Action? AfterStage { get; set; }

        public Action? AfterFinalize { get; set; }

        public bool StageUploadWasCalled { get; private set; }

        public bool OpenStagedUploadWasCalled { get; private set; }

        public bool AbandonUploadWasCalled { get; private set; }

        public bool FinalizeUploadWasCalled { get; private set; }

        public bool MoveToQuarantineWasCalled { get; private set; }

        public bool DeleteFromQuarantineWasCalled { get; private set; }

        public string FinalStorageFileName { get; private set; } =
            string.Empty;

        public async Task<StagedRecipePhotoUpload> StageUploadAsync(
            Stream content,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            StageUploadWasCalled = true;
            cancellationToken.ThrowIfCancellationRequested();

            if (StageException is not null)
            {
                throw StageException;
            }

            using MemoryStream stagedContent = new();
            await content.CopyToAsync(stagedContent, cancellationToken);

            if (stagedContent.Length > maximumBytes)
            {
                throw new RecipePhotoStorageLimitExceededException(
                    maximumBytes);
            }

            _stagedContent = stagedContent.ToArray();
            AfterStage?.Invoke();
            return new StagedRecipePhotoUpload(
                StagedToken,
                _stagedContent.Length);
        }

        public Stream OpenStagedUpload(StagedRecipePhotoUpload upload)
        {
            OpenStagedUploadWasCalled = true;
            return new MemoryStream(_stagedContent, writable: false);
        }

        public string FinalizeUpload(
            StagedRecipePhotoUpload upload,
            RecipePhotoFormat format)
        {
            FinalizeUploadWasCalled = true;

            if (FinalizeException is not null)
            {
                throw FinalizeException;
            }

            string extension = RecipePhotoFormats
                .GetCanonicalExtension(format)!;
            FinalStorageFileName =
                "33333333333333333333333333333333" + extension;
            AfterFinalize?.Invoke();
            return FinalStorageFileName;
        }

        public void AbandonUpload(StagedRecipePhotoUpload upload)
        {
            AbandonUploadWasCalled = true;
            _stagedContent = [];
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
            MoveToQuarantineWasCalled = true;
            return new StagedRecipePhotoDeletion(DeletionToken);
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            DeleteFromQuarantineWasCalled = true;

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

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableReadStream(byte[] content)
        {
            _inner = new MemoryStream(content, writable: false);
        }

        public bool IsDisposed { get; private set; }

        public override bool CanRead => !IsDisposed && _inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;

            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
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
