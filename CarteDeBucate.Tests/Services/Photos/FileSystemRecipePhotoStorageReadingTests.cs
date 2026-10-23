public sealed class FileSystemRecipePhotoStorageReadingTests : IDisposable
{
    private const string MissingStorageFileName =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";

    private readonly string _rootPath;
    private readonly FileSystemRecipePhotoStorage _storage;

    public FileSystemRecipePhotoStorageReadingTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photo-reading-tests-{Guid.NewGuid():N}");
        _storage = new FileSystemRecipePhotoStorage(_rootPath);
    }

    [Fact]
    public async Task OpenFinalFile_WhenFileExists_ShouldReturnCompleteContent()
    {
        byte[] expectedContent = [1, 2, 3, 4];
        string storageFileName = await CreateFinalFileAsync(
            expectedContent);

        Stream? openedContent = _storage.OpenFinalFile(storageFileName);
        Assert.NotNull(openedContent);

        using Stream content = openedContent;
        using MemoryStream result = new();
        content.CopyTo(result);

        Assert.Equal(expectedContent, result.ToArray());
        Assert.True(_storage.FinalFileExists(storageFileName));
    }

    [Fact]
    public async Task OpenFinalFile_WhenFileExists_ShouldReturnReadOnlySeekableStream()
    {
        string storageFileName = await CreateFinalFileAsync([1]);

        Stream? openedContent = _storage.OpenFinalFile(storageFileName);
        Assert.NotNull(openedContent);

        using Stream content = openedContent;

        Assert.True(content.CanRead);
        Assert.True(content.CanSeek);
        Assert.False(content.CanWrite);
    }

    [Fact]
    public void OpenFinalFile_WhenFileDoesNotExist_ShouldReturnNull()
    {
        Stream? result = _storage.OpenFinalFile(MissingStorageFileName);

        Assert.Null(result);
        Assert.False(_storage.FinalFileExists(MissingStorageFileName));
    }

    [Fact]
    public void FinalFileOperations_WithInvalidName_ShouldThrow()
    {
        const string invalidStorageFileName = "../outside.jpg";

        Assert.Throws<ArgumentException>(() =>
            _storage.OpenFinalFile(invalidStorageFileName));
        Assert.Throws<ArgumentException>(() =>
            _storage.FinalFileExists(invalidStorageFileName));
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
}
