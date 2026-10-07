using System.Text.Json;

public sealed class FileSystemRecipePhotoStorageQuarantineTests : IDisposable
{
    private static readonly DateTimeOffset QuarantinedAt =
        new(2026, 10, 7, 10, 30, 0, TimeSpan.Zero);

    private readonly string _rootPath;
    private readonly string _quarantinePath;
    private readonly FileSystemRecipePhotoStorage _storage;

    public FileSystemRecipePhotoStorageQuarantineTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photo-quarantine-tests-{Guid.NewGuid():N}");
        _quarantinePath = Path.Combine(
            _rootPath,
            FileSystemRecipePhotoStorage.QuarantineDirectoryName);
        _storage = new FileSystemRecipePhotoStorage(
            _rootPath,
            Guid.NewGuid,
            new FixedTimeProvider(QuarantinedAt));
    }

    [Fact]
    public async Task MoveToQuarantine_ShouldMoveFileAndPersistDescriptor()
    {
        byte[] content = [1, 2, 3];
        string storageFileName = await CreateFinalFileAsync(content);

        StagedRecipePhotoDeletion? deletion =
            _storage.MoveToQuarantine(storageFileName);

        Assert.NotNull(deletion);
        Assert.False(_storage.FinalFileExists(storageFileName));

        string directoryPath = Assert.Single(
            Directory.GetDirectories(_quarantinePath));
        Assert.Equal(deletion.Token, Path.GetFileName(directoryPath));
        Assert.Equal(
            content,
            File.ReadAllBytes(
                Path.Combine(directoryPath, storageFileName)));

        using JsonDocument descriptor = JsonDocument.Parse(
            File.ReadAllText(
                Path.Combine(directoryPath, "descriptor.json")));
        JsonElement root = descriptor.RootElement;

        Assert.Equal(
            deletion.Token,
            root.GetProperty("token").GetString());
        Assert.Equal(
            storageFileName,
            root.GetProperty("storageFileName").GetString());
        Assert.Equal(
            QuarantinedAt.UtcDateTime,
            root.GetProperty("quarantinedAtUtc").GetDateTime());
    }

    [Fact]
    public async Task RestoreFromQuarantine_ShouldRestoreOriginalStorageFileName()
    {
        byte[] content = [1, 2, 3];
        string storageFileName = await CreateFinalFileAsync(content);
        StagedRecipePhotoDeletion deletion =
            _storage.MoveToQuarantine(storageFileName)!;

        _storage.RestoreFromQuarantine(deletion);

        Assert.True(_storage.FinalFileExists(storageFileName));
        Assert.Equal(
            content,
            File.ReadAllBytes(Path.Combine(_rootPath, storageFileName)));
        Assert.Empty(Directory.GetDirectories(_quarantinePath));
    }

    [Fact]
    public async Task RestoreFromQuarantine_WithNewStorageInstance_ShouldUsePersistedDescriptor()
    {
        byte[] content = [1, 2, 3];
        string storageFileName = await CreateFinalFileAsync(content);
        StagedRecipePhotoDeletion deletion =
            _storage.MoveToQuarantine(storageFileName)!;
        FileSystemRecipePhotoStorage restartedStorage = new(_rootPath);

        restartedStorage.RestoreFromQuarantine(deletion);

        Assert.Equal(
            content,
            File.ReadAllBytes(Path.Combine(_rootPath, storageFileName)));
        Assert.Empty(Directory.GetDirectories(_quarantinePath));
    }

    [Fact]
    public async Task DeleteFromQuarantine_ShouldDeleteContentPermanently()
    {
        string storageFileName = await CreateFinalFileAsync([1]);
        StagedRecipePhotoDeletion deletion =
            _storage.MoveToQuarantine(storageFileName)!;

        _storage.DeleteFromQuarantine(deletion);

        Assert.False(_storage.FinalFileExists(storageFileName));
        Assert.Empty(Directory.GetDirectories(_quarantinePath));
    }

    [Fact]
    public void MoveToQuarantine_WhenFinalFileIsMissing_ShouldReturnNull()
    {
        const string missingStorageFileName =
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";

        StagedRecipePhotoDeletion? result =
            _storage.MoveToQuarantine(missingStorageFileName);

        Assert.Null(result);
        Assert.Empty(Directory.GetDirectories(_quarantinePath));
    }

    [Fact]
    public void QuarantineOperations_WithInvalidToken_ShouldThrow()
    {
        StagedRecipePhotoDeletion invalidDeletion = new("../outside");

        Assert.Throws<ArgumentException>(() =>
            _storage.RestoreFromQuarantine(invalidDeletion));
        Assert.Throws<ArgumentException>(() =>
            _storage.DeleteFromQuarantine(invalidDeletion));
    }

    [Fact]
    public async Task RestoreFromQuarantine_WhenFinalFileExists_ShouldNotOverwrite()
    {
        byte[] quarantinedContent = [1, 2, 3];
        string storageFileName = await CreateFinalFileAsync(
            quarantinedContent);
        StagedRecipePhotoDeletion deletion =
            _storage.MoveToQuarantine(storageFileName)!;
        byte[] existingContent = [9, 9, 9];
        File.WriteAllBytes(
            Path.Combine(_rootPath, storageFileName),
            existingContent);

        Assert.Throws<IOException>(() =>
            _storage.RestoreFromQuarantine(deletion));

        Assert.Equal(
            existingContent,
            File.ReadAllBytes(Path.Combine(_rootPath, storageFileName)));
        string directoryPath = Assert.Single(
            Directory.GetDirectories(_quarantinePath));
        Assert.Equal(
            quarantinedContent,
            File.ReadAllBytes(
                Path.Combine(directoryPath, storageFileName)));
    }

    [Fact]
    public async Task RestoreFromQuarantine_WhenRepeated_ShouldRemainSuccessful()
    {
        string storageFileName = await CreateFinalFileAsync([1]);
        StagedRecipePhotoDeletion deletion =
            _storage.MoveToQuarantine(storageFileName)!;

        _storage.RestoreFromQuarantine(deletion);
        _storage.RestoreFromQuarantine(deletion);

        Assert.True(_storage.FinalFileExists(storageFileName));
    }

    [Fact]
    public async Task DeleteFromQuarantine_WhenRepeated_ShouldRemainSuccessful()
    {
        string storageFileName = await CreateFinalFileAsync([1]);
        StagedRecipePhotoDeletion deletion =
            _storage.MoveToQuarantine(storageFileName)!;

        _storage.DeleteFromQuarantine(deletion);
        _storage.DeleteFromQuarantine(deletion);

        Assert.False(_storage.FinalFileExists(storageFileName));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private async Task<string> CreateFinalFileAsync(byte[] content)
    {
        using MemoryStream source = new(content);
        StagedRecipePhotoUpload upload = await _storage.StageUploadAsync(
            source,
            maximumBytes: content.Length);

        return _storage.FinalizeUpload(
            upload,
            RecipePhotoFormat.Jpeg);
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
}
