public sealed class FileSystemRecipePhotoStorageInventoryTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _stagingPath;
    private readonly string _quarantinePath;
    private readonly FileSystemRecipePhotoStorage _storage;

    public FileSystemRecipePhotoStorageInventoryTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photo-inventory-tests-{Guid.NewGuid():N}");
        _stagingPath = Path.Combine(
            _rootPath,
            FileSystemRecipePhotoStorage.StagingDirectoryName);
        _quarantinePath = Path.Combine(
            _rootPath,
            FileSystemRecipePhotoStorage.QuarantineDirectoryName);
        _storage = new FileSystemRecipePhotoStorage(_rootPath);
    }

    [Fact]
    public void GetInventories_WhenStorageIsEmpty_ShouldReturnEmptyResults()
    {
        RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            staged = _storage.GetStagedUploads();
        RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            quarantined = _storage.GetQuarantinedDeletions();
        RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            finalFiles = _storage.GetFinalFiles();

        Assert.Empty(staged.Items);
        Assert.Equal(0, staged.InvalidEntryCount);
        Assert.Empty(quarantined.Items);
        Assert.Equal(0, quarantined.InvalidEntryCount);
        Assert.Empty(finalFiles.Items);
        Assert.Equal(0, finalFiles.InvalidEntryCount);
    }

    [Fact]
    public async Task GetInventories_WithMultipleItems_ShouldReturnAllItems()
    {
        _ = await StageContentAsync([1]);
        _ = await StageContentAsync([2]);
        string firstFinal = await CreateFinalFileAsync([3]);
        string secondFinal = await CreateFinalFileAsync([4]);
        string firstQuarantined = await CreateFinalFileAsync([5]);
        string secondQuarantined = await CreateFinalFileAsync([6]);
        _ = _storage.MoveToQuarantine(firstQuarantined);
        _ = _storage.MoveToQuarantine(secondQuarantined);

        RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            staged = _storage.GetStagedUploads();
        RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            quarantined = _storage.GetQuarantinedDeletions();
        RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            finalFiles = _storage.GetFinalFiles();

        Assert.Equal(2, staged.Items.Count);
        Assert.All(staged.Items, item =>
        {
            Assert.True(item.Upload.Length > 0);
            Assert.False(Path.IsPathFullyQualified(item.Upload.Token));
            Assert.DoesNotContain(_rootPath, item.Upload.Token);
        });

        Assert.Equal(2, quarantined.Items.Count);
        Assert.All(quarantined.Items, item =>
        {
            Assert.False(Path.IsPathFullyQualified(item.Deletion.Token));
            Assert.False(Path.IsPathFullyQualified(item.StorageFileName));
            Assert.DoesNotContain(_rootPath, item.Deletion.Token);
            Assert.DoesNotContain(_rootPath, item.StorageFileName);
        });
        Assert.Contains(
            quarantined.Items,
            item => item.StorageFileName == firstQuarantined);
        Assert.Contains(
            quarantined.Items,
            item => item.StorageFileName == secondQuarantined);

        Assert.Equal(2, finalFiles.Items.Count);
        Assert.All(finalFiles.Items, item =>
        {
            Assert.False(Path.IsPathFullyQualified(item.StorageFileName));
            Assert.DoesNotContain(_rootPath, item.StorageFileName);
        });
        Assert.Contains(
            finalFiles.Items,
            item => item.StorageFileName == firstFinal);
        Assert.Contains(
            finalFiles.Items,
            item => item.StorageFileName == secondFinal);
    }

    [Fact]
    public async Task GetQuarantinedDeletions_WithInvalidDescriptor_ShouldReportInvalidEntryWithoutChangingFiles()
    {
        string storageFileName = await CreateFinalFileAsync([1, 2, 3]);
        _ = _storage.MoveToQuarantine(storageFileName);
        string directoryPath = Assert.Single(
            Directory.GetDirectories(_quarantinePath));
        string descriptorPath = Path.Combine(
            directoryPath,
            "descriptor.json");
        File.WriteAllText(descriptorPath, "not-json");
        string[] filesBeforeInventory = Directory.GetFiles(directoryPath);

        RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            result = _storage.GetQuarantinedDeletions();

        Assert.Empty(result.Items);
        Assert.Equal(1, result.InvalidEntryCount);
        Assert.Equal(
            filesBeforeInventory.Order(),
            Directory.GetFiles(directoryPath).Order());
        Assert.Equal("not-json", File.ReadAllText(descriptorPath));
    }

    [Fact]
    public void GetInventories_WithInvalidEntries_ShouldReportThemWithoutPaths()
    {
        File.WriteAllText(
            Path.Combine(_stagingPath, "not-a-token.upload"),
            "invalid");
        File.WriteAllText(
            Path.Combine(_quarantinePath, "unexpected.txt"),
            "invalid");
        File.WriteAllText(
            Path.Combine(_rootPath, "readme.txt"),
            "invalid");

        RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            staged = _storage.GetStagedUploads();
        RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            quarantined = _storage.GetQuarantinedDeletions();
        RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            finalFiles = _storage.GetFinalFiles();

        Assert.Equal(1, staged.InvalidEntryCount);
        Assert.Equal(1, quarantined.InvalidEntryCount);
        Assert.Equal(1, finalFiles.InvalidEntryCount);
        Assert.DoesNotContain(
            _rootPath,
            string.Join(",", staged.Items.Select(item => item.Upload.Token)));
        Assert.DoesNotContain(
            _rootPath,
            string.Join(",", quarantined.Items.Select(
                item => item.StorageFileName)));
        Assert.DoesNotContain(
            _rootPath,
            string.Join(",", finalFiles.Items.Select(
                item => item.StorageFileName)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
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

    private async Task<string> CreateFinalFileAsync(byte[] content)
    {
        StagedRecipePhotoUpload upload = await StageContentAsync(content);

        return _storage.FinalizeUpload(
            upload,
            RecipePhotoFormat.Jpeg);
    }
}
