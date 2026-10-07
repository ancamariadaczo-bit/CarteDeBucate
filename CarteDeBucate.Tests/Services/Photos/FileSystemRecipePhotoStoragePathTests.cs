public sealed class FileSystemRecipePhotoStoragePathTests : IDisposable
{
    private const string CanonicalStorageFileName =
        "d0b122fc466a4247896c5d864ac69711.jpg";

    private readonly string _temporaryDirectory;

    public FileSystemRecipePhotoStoragePathTests()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photo-storage-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_temporaryDirectory);
    }

    [Fact]
    public void Constructor_WithAbsoluteRoot_ShouldCreateStorageDirectories()
    {
        string rootPath = Path.Combine(_temporaryDirectory, "photos");

        _ = new FileSystemRecipePhotoStorage(rootPath);

        Assert.True(Directory.Exists(rootPath));
        Assert.True(Directory.Exists(Path.Combine(rootPath, "staging")));
        Assert.True(Directory.Exists(Path.Combine(rootPath, "quarantine")));
    }

    [Fact]
    public void Constructor_WithRelativeRoot_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new FileSystemRecipePhotoStorage("relative/photos"));
    }

    [Fact]
    public void Constructor_WhenRootIsExistingFile_ShouldThrow()
    {
        string filePath = Path.Combine(_temporaryDirectory, "photos");
        File.WriteAllText(filePath, "not a directory");

        Assert.Throws<ArgumentException>(() =>
            new FileSystemRecipePhotoStorage(filePath));
    }

    [Fact]
    public void ResolveFinalFilePath_WithCanonicalName_ShouldStayUnderRoot()
    {
        string rootPath = Path.Combine(_temporaryDirectory, "photos");
        FileSystemRecipePhotoStorage storage = new(rootPath);

        string result = storage.ResolveFinalFilePath(
            CanonicalStorageFileName);

        Assert.Equal(
            Path.Combine(rootPath, CanonicalStorageFileName),
            result);
    }

    [Fact]
    public void ResolveFinalFilePath_WithPathTraversal_ShouldThrow()
    {
        FileSystemRecipePhotoStorage storage = CreateStorage();

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(
                $"../outside/{CanonicalStorageFileName}"));
    }

    [Fact]
    public void ResolveFinalFilePath_WithDeceptiveRootPrefix_ShouldThrow()
    {
        string rootPath = Path.Combine(_temporaryDirectory, "photos");
        FileSystemRecipePhotoStorage storage = new(rootPath);

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(
                $"../photos-other/{CanonicalStorageFileName}"));
    }

    [Fact]
    public void ResolveFinalFilePath_WithWindowsSeparator_ShouldThrow()
    {
        FileSystemRecipePhotoStorage storage = CreateStorage();

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(
                $"folder\\{CanonicalStorageFileName}"));
    }

    [Fact]
    public void ResolveFinalFilePath_WithUnixSeparator_ShouldThrow()
    {
        FileSystemRecipePhotoStorage storage = CreateStorage();

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(
                $"folder/{CanonicalStorageFileName}"));
    }

    [Fact]
    public void ResolveFinalFilePath_WithAbsoluteName_ShouldThrow()
    {
        FileSystemRecipePhotoStorage storage = CreateStorage();
        string absolutePath = Path.Combine(
            _temporaryDirectory,
            CanonicalStorageFileName);

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(absolutePath));
    }

    [Fact]
    public void ResolveFinalFilePath_WithControlCharacter_ShouldThrow()
    {
        FileSystemRecipePhotoStorage storage = CreateStorage();

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(
                "d0b122fc466a4247896c5d864ac6971\u0001.jpg"));
    }

    [Theory]
    [InlineData("d0b122fc466a4247896c5d864ac69711.jpeg")]
    [InlineData("D0B122FC466A4247896C5D864AC69711.jpg")]
    [InlineData("not-a-guid.jpg")]
    public void ResolveFinalFilePath_WithNonCanonicalName_ShouldThrow(
        string storageFileName)
    {
        FileSystemRecipePhotoStorage storage = CreateStorage();

        Assert.Throws<ArgumentException>(() =>
            storage.ResolveFinalFilePath(storageFileName));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private FileSystemRecipePhotoStorage CreateStorage()
    {
        return new FileSystemRecipePhotoStorage(
            Path.Combine(_temporaryDirectory, "photos"));
    }
}
