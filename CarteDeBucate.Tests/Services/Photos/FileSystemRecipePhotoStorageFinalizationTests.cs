public sealed class FileSystemRecipePhotoStorageFinalizationTests : IDisposable
{
    private readonly string _temporaryDirectory;
    private readonly string _storageRoot;
    private readonly FileSystemRecipePhotoStorage _storage;

    public FileSystemRecipePhotoStorageFinalizationTests()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photo-finalization-tests-{Guid.NewGuid():N}");
        _storageRoot = Path.Combine(_temporaryDirectory, "photos");
        _storage = new FileSystemRecipePhotoStorage(_storageRoot);
    }

    [Fact]
    public async Task OpenStagedUpload_ShouldReturnCompleteReadOnlyContent()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using MemoryStream source = new(content);
        StagedRecipePhotoUpload upload = await _storage.StageUploadAsync(
            source,
            maximumBytes: content.Length);

        using Stream stagedContent = _storage.OpenStagedUpload(upload);
        using MemoryStream result = new();
        stagedContent.CopyTo(result);

        Assert.True(stagedContent.CanRead);
        Assert.True(stagedContent.CanSeek);
        Assert.False(stagedContent.CanWrite);
        Assert.Equal(content, result.ToArray());
    }

    [Theory]
    [InlineData(RecipePhotoFormat.Jpeg, ".jpg")]
    [InlineData(RecipePhotoFormat.Png, ".png")]
    [InlineData(RecipePhotoFormat.WebP, ".webp")]
    public async Task FinalizeUpload_ShouldUseCanonicalExtension(
        RecipePhotoFormat format,
        string expectedExtension)
    {
        StagedRecipePhotoUpload upload = await StageContentAsync([1]);

        string storageFileName = _storage.FinalizeUpload(upload, format);

        Assert.EndsWith(expectedExtension, storageFileName);
        Assert.True(File.Exists(
            Path.Combine(_storageRoot, storageFileName)));
    }

    [Fact]
    public async Task FinalizeUpload_ForTwoUploads_ShouldUseUniqueNames()
    {
        StagedRecipePhotoUpload firstUpload =
            await StageContentAsync([1]);
        StagedRecipePhotoUpload secondUpload =
            await StageContentAsync([2]);

        string firstName = _storage.FinalizeUpload(
            firstUpload,
            RecipePhotoFormat.Jpeg);
        string secondName = _storage.FinalizeUpload(
            secondUpload,
            RecipePhotoFormat.Jpeg);

        Assert.NotEqual(firstName, secondName);
    }

    [Fact]
    public async Task FinalizeUpload_WhenNameCollides_ShouldNotOverwriteExistingFile()
    {
        Guid uploadIdentifier =
            Guid.ParseExact("11111111111111111111111111111111", "N");
        Guid collidingIdentifier =
            Guid.ParseExact("22222222222222222222222222222222", "N");
        Guid replacementIdentifier =
            Guid.ParseExact("33333333333333333333333333333333", "N");
        Queue<Guid> identifiers = new(
            [uploadIdentifier, collidingIdentifier, replacementIdentifier]);
        string controlledRoot = Path.Combine(
            _temporaryDirectory,
            "controlled-photos");
        FileSystemRecipePhotoStorage controlledStorage = new(
            controlledRoot,
            () => identifiers.Dequeue());
        string existingFileName = $"{collidingIdentifier:N}.jpg";
        string existingFilePath = Path.Combine(
            controlledRoot,
            existingFileName);
        byte[] existingContent = [9, 9, 9];
        File.WriteAllBytes(existingFilePath, existingContent);
        using MemoryStream source = new([1, 2, 3]);
        StagedRecipePhotoUpload upload =
            await controlledStorage.StageUploadAsync(
                source,
                maximumBytes: source.Length);

        string result = controlledStorage.FinalizeUpload(
            upload,
            RecipePhotoFormat.Jpeg);

        Assert.Equal($"{replacementIdentifier:N}.jpg", result);
        Assert.Equal(existingContent, File.ReadAllBytes(existingFilePath));
        Assert.True(File.Exists(Path.Combine(controlledRoot, result)));
    }

    [Fact]
    public async Task FinalizeUpload_AfterSuccess_ShouldRemoveStagedFile()
    {
        StagedRecipePhotoUpload upload = await StageContentAsync([1]);

        _storage.FinalizeUpload(upload, RecipePhotoFormat.Png);

        Assert.Empty(GetStagedFiles());
    }

    [Fact]
    public async Task AbandonUpload_WhenCalledRepeatedly_ShouldRemainSuccessful()
    {
        StagedRecipePhotoUpload upload = await StageContentAsync([1]);

        _storage.AbandonUpload(upload);
        _storage.AbandonUpload(upload);

        Assert.Empty(GetStagedFiles());
    }

    [Fact]
    public void StagedOperations_WithInvalidToken_ShouldThrow()
    {
        StagedRecipePhotoUpload invalidUpload = new(
            "../outside",
            length: 1);

        Assert.Throws<ArgumentException>(() =>
            _storage.OpenStagedUpload(invalidUpload));
        Assert.Throws<ArgumentException>(() =>
            _storage.FinalizeUpload(
                invalidUpload,
                RecipePhotoFormat.Jpeg));
        Assert.Throws<ArgumentException>(() =>
            _storage.AbandonUpload(invalidUpload));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private async Task<StagedRecipePhotoUpload> StageContentAsync(
        byte[] content)
    {
        using MemoryStream source = new(content);

        return await _storage.StageUploadAsync(
            source,
            maximumBytes: content.Length);
    }

    private string[] GetStagedFiles()
    {
        return Directory.GetFiles(
            Path.Combine(
                _storageRoot,
                FileSystemRecipePhotoStorage.StagingDirectoryName));
    }
}
