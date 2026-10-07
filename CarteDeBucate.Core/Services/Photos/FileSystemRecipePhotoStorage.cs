using System.Text.Json;

internal sealed class FileSystemRecipePhotoStorage : IRecipePhotoStorage
{
    private const int CopyBufferSize = 81920;
    private const string StagedUploadFileExtension = ".upload";
    private const string QuarantineDescriptorFileName = "descriptor.json";
    private const string QuarantineDescriptorTemporaryFileName =
        "descriptor.tmp";

    internal const string StagingDirectoryName = "staging";
    internal const string QuarantineDirectoryName = "quarantine";

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static readonly JsonSerializerOptions DescriptorJsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly string _stagingPath;
    private readonly string _stagingPathWithSeparator;
    private readonly string _quarantinePath;
    private readonly string _quarantinePathWithSeparator;
    private readonly Func<Guid> _guidFactory;
    private readonly TimeProvider _timeProvider;

    public FileSystemRecipePhotoStorage(string rootPath)
        : this(rootPath, Guid.NewGuid, TimeProvider.System)
    {
    }

    internal FileSystemRecipePhotoStorage(
        string rootPath,
        Func<Guid> guidFactory)
        : this(rootPath, guidFactory, TimeProvider.System)
    {
    }

    internal FileSystemRecipePhotoStorage(
        string rootPath,
        Func<Guid> guidFactory,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(guidFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException(
                "The recipe photo storage root is required.",
                nameof(rootPath));
        }

        if (!Path.IsPathFullyQualified(rootPath))
        {
            throw new ArgumentException(
                "The recipe photo storage root must be an absolute path.",
                nameof(rootPath));
        }

        _rootPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(rootPath));

        if (File.Exists(_rootPath))
        {
            throw new ArgumentException(
                "The recipe photo storage root cannot be an existing file.",
                nameof(rootPath));
        }

        _rootPathWithSeparator = _rootPath.EndsWith(
            Path.DirectorySeparatorChar)
                ? _rootPath
                : _rootPath + Path.DirectorySeparatorChar;

        _stagingPath = Path.Combine(_rootPath, StagingDirectoryName);
        _stagingPathWithSeparator = _stagingPath
            + Path.DirectorySeparatorChar;
        _quarantinePath = Path.Combine(
            _rootPath,
            QuarantineDirectoryName);
        _quarantinePathWithSeparator = _quarantinePath
            + Path.DirectorySeparatorChar;
        _guidFactory = guidFactory;
        _timeProvider = timeProvider;

        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(_stagingPath);
        Directory.CreateDirectory(_quarantinePath);
    }

    internal string ResolveFinalFilePath(string storageFileName)
    {
        ValidateStorageFileName(storageFileName);

        string resolvedPath = Path.GetFullPath(
            Path.Combine(_rootPath, storageFileName));

        if (!resolvedPath.StartsWith(
                _rootPathWithSeparator,
                PathComparison))
        {
            throw new ArgumentException(
                "The storage file name resolves outside the photo storage root.",
                nameof(storageFileName));
        }

        return resolvedPath;
    }

    internal static void ValidateStorageFileName(string storageFileName)
    {
        if (string.IsNullOrWhiteSpace(storageFileName))
        {
            throw new ArgumentException(
                "The storage file name is required.",
                nameof(storageFileName));
        }

        if (Path.IsPathFullyQualified(storageFileName)
            || storageFileName.Contains("..", StringComparison.Ordinal)
            || storageFileName.Contains(Path.DirectorySeparatorChar)
            || storageFileName.Contains(Path.AltDirectorySeparatorChar)
            || storageFileName.Contains('/')
            || storageFileName.Contains('\\')
            || storageFileName.Any(char.IsControl))
        {
            throw new ArgumentException(
                "The storage file name is invalid.",
                nameof(storageFileName));
        }

        int extensionStart = storageFileName.IndexOf('.');

        if (extensionStart <= 0
            || storageFileName.IndexOf('.', extensionStart + 1) >= 0)
        {
            throw new ArgumentException(
                "The storage file name is not canonical.",
                nameof(storageFileName));
        }

        string guidPart = storageFileName[..extensionStart];
        string extension = storageFileName[extensionStart..];

        if (!Guid.TryParseExact(guidPart, "N", out Guid identifier)
            || identifier == Guid.Empty
            || !string.Equals(
                guidPart,
                identifier.ToString("N"),
                StringComparison.Ordinal)
            || extension is not (".jpg" or ".png" or ".webp"))
        {
            throw new ArgumentException(
                "The storage file name is not canonical.",
                nameof(storageFileName));
        }
    }

    public async Task<StagedRecipePhotoUpload> StageUploadAsync(
        Stream content,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The photo content stream must be readable.",
                nameof(content));
        }

        if (maximumBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        cancellationToken.ThrowIfCancellationRequested();

        (string token, string stagedFilePath, FileStream destination) =
            CreateStagedUploadFile();

        try
        {
            await using (destination)
            {
                byte[] buffer = new byte[CopyBufferSize];
                long totalBytesRead = 0;

                while (true)
                {
                    long remainingBytes = maximumBytes - totalBytesRead;
                    int bytesToRead = remainingBytes >= buffer.Length
                        ? buffer.Length
                        : (int)remainingBytes + 1;

                    int bytesRead = await content.ReadAsync(
                        buffer.AsMemory(0, bytesToRead),
                        cancellationToken);

                    if (bytesRead == 0)
                    {
                        return new StagedRecipePhotoUpload(
                            token,
                            totalBytesRead);
                    }

                    if (bytesRead > remainingBytes)
                    {
                        throw new RecipePhotoStorageLimitExceededException(
                            maximumBytes);
                    }

                    await destination.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken);

                    totalBytesRead += bytesRead;
                }
            }
        }
        catch
        {
            File.Delete(stagedFilePath);
            throw;
        }
    }

    private (string Token, string FilePath, FileStream Stream)
        CreateStagedUploadFile()
    {
        while (true)
        {
            string token = _guidFactory().ToString("N");
            string filePath = Path.Combine(
                _stagingPath,
                token + StagedUploadFileExtension);

            try
            {
                FileStream stream = new(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    CopyBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                return (token, filePath, stream);
            }
            catch (IOException) when (File.Exists(filePath))
            {
                // Generate another token if an extremely unlikely collision occurs.
                continue;
            }
        }
    }

    public Stream OpenStagedUpload(StagedRecipePhotoUpload upload)
    {
        string stagedFilePath = ResolveStagedUploadPath(upload);

        return new FileStream(
            stagedFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            CopyBufferSize,
            FileOptions.SequentialScan);
    }

    public string FinalizeUpload(
        StagedRecipePhotoUpload upload,
        RecipePhotoFormat format)
    {
        string stagedFilePath = ResolveStagedUploadPath(upload);
        string canonicalExtension =
            RecipePhotoFormats.GetCanonicalExtension(format)
            ?? throw new ArgumentOutOfRangeException(nameof(format));

        while (true)
        {
            string storageFileName =
                _guidFactory().ToString("N") + canonicalExtension;
            string finalFilePath = ResolveFinalFilePath(storageFileName);

            try
            {
                File.Move(
                    stagedFilePath,
                    finalFilePath,
                    overwrite: false);

                return storageFileName;
            }
            catch (IOException) when (File.Exists(finalFilePath))
            {
                // Preserve the existing file and retry with another name.
                continue;
            }
        }
    }

    public void AbandonUpload(StagedRecipePhotoUpload upload)
    {
        string stagedFilePath = ResolveStagedUploadPath(upload);
        File.Delete(stagedFilePath);
    }

    private string ResolveStagedUploadPath(StagedRecipePhotoUpload upload)
    {
        ArgumentNullException.ThrowIfNull(upload);

        string token = upload.Token;

        if (upload.Length < 0 || !IsCanonicalToken(token))
        {
            throw new ArgumentException(
                "The staged upload token is invalid.",
                nameof(upload));
        }

        string stagedFilePath = Path.GetFullPath(
            Path.Combine(
                _stagingPath,
                token + StagedUploadFileExtension));

        if (!stagedFilePath.StartsWith(
                _stagingPathWithSeparator,
                PathComparison))
        {
            throw new ArgumentException(
                "The staged upload resolves outside the staging directory.",
                nameof(upload));
        }

        return stagedFilePath;
    }

    public Stream? OpenFinalFile(string storageFileName)
    {
        string finalFilePath = ResolveFinalFilePath(storageFileName);

        try
        {
            return new FileStream(
                finalFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                FileOptions.SequentialScan);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public bool FinalFileExists(string storageFileName)
    {
        string finalFilePath = ResolveFinalFilePath(storageFileName);
        return File.Exists(finalFilePath);
    }

    public StagedRecipePhotoDeletion? MoveToQuarantine(
        string storageFileName)
    {
        string finalFilePath = ResolveFinalFilePath(storageFileName);

        if (!File.Exists(finalFilePath))
        {
            return null;
        }

        (string token, string directoryPath) =
            CreateQuarantineDirectory();
        string quarantinedFilePath = Path.Combine(
            directoryPath,
            storageFileName);

        try
        {
            File.Move(
                finalFilePath,
                quarantinedFilePath,
                overwrite: false);
        }
        catch (FileNotFoundException)
        {
            DeleteEmptyDirectory(directoryPath);
            return null;
        }
        catch
        {
            DeleteEmptyDirectory(directoryPath);
            throw;
        }

        QuarantineDescriptor descriptor = new()
        {
            Token = token,
            StorageFileName = storageFileName,
            QuarantinedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        try
        {
            WriteQuarantineDescriptor(directoryPath, descriptor);
            return new StagedRecipePhotoDeletion(token);
        }
        catch
        {
            RestoreAfterDescriptorFailure(
                quarantinedFilePath,
                finalFilePath,
                directoryPath);
            throw;
        }
    }

    public void RestoreFromQuarantine(
        StagedRecipePhotoDeletion deletion)
    {
        string directoryPath = ResolveQuarantineDirectoryPath(deletion);

        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        QuarantineDescriptor descriptor = ReadQuarantineDescriptor(
            directoryPath,
            deletion.Token);
        string quarantinedFilePath = Path.Combine(
            directoryPath,
            descriptor.StorageFileName);
        string finalFilePath = ResolveFinalFilePath(
            descriptor.StorageFileName);

        if (File.Exists(quarantinedFilePath))
        {
            File.Move(
                quarantinedFilePath,
                finalFilePath,
                overwrite: false);
        }
        else if (!File.Exists(finalFilePath))
        {
            throw new FileNotFoundException(
                "The quarantined photo content is missing.",
                quarantinedFilePath);
        }

        DeleteQuarantineDirectory(directoryPath);
    }

    public void DeleteFromQuarantine(
        StagedRecipePhotoDeletion deletion)
    {
        string directoryPath = ResolveQuarantineDirectoryPath(deletion);

        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        _ = ReadQuarantineDescriptor(directoryPath, deletion.Token);
        DeleteQuarantineDirectory(directoryPath);
    }

    private (string Token, string DirectoryPath)
        CreateQuarantineDirectory()
    {
        while (true)
        {
            string token = _guidFactory().ToString("N");
            string directoryPath = Path.Combine(_quarantinePath, token);

            if (Directory.Exists(directoryPath))
            {
                continue;
            }

            Directory.CreateDirectory(directoryPath);
            return (token, directoryPath);
        }
    }

    private string ResolveQuarantineDirectoryPath(
        StagedRecipePhotoDeletion deletion)
    {
        ArgumentNullException.ThrowIfNull(deletion);

        string token = deletion.Token;

        if (!IsCanonicalToken(token))
        {
            throw new ArgumentException(
                "The staged deletion token is invalid.",
                nameof(deletion));
        }

        string directoryPath = Path.GetFullPath(
            Path.Combine(_quarantinePath, token));

        if (!directoryPath.StartsWith(
                _quarantinePathWithSeparator,
                PathComparison))
        {
            throw new ArgumentException(
                "The staged deletion resolves outside the quarantine directory.",
                nameof(deletion));
        }

        return directoryPath;
    }

    private static void WriteQuarantineDescriptor(
        string directoryPath,
        QuarantineDescriptor descriptor)
    {
        string temporaryPath = Path.Combine(
            directoryPath,
            QuarantineDescriptorTemporaryFileName);
        string descriptorPath = Path.Combine(
            directoryPath,
            QuarantineDescriptorFileName);
        string json = JsonSerializer.Serialize(
            descriptor,
            DescriptorJsonOptions);

        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, descriptorPath, overwrite: false);
    }

    private static QuarantineDescriptor ReadQuarantineDescriptor(
        string directoryPath,
        string expectedToken)
    {
        string descriptorPath = Path.Combine(
            directoryPath,
            QuarantineDescriptorFileName);

        try
        {
            string json = File.ReadAllText(descriptorPath);
            QuarantineDescriptor? descriptor =
                JsonSerializer.Deserialize<QuarantineDescriptor>(
                    json,
                    DescriptorJsonOptions);

            if (descriptor is null
                || !string.Equals(
                    descriptor.Token,
                    expectedToken,
                    StringComparison.Ordinal)
                || descriptor.QuarantinedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new InvalidDataException(
                    "The quarantine descriptor is invalid.");
            }

            try
            {
                ValidateStorageFileName(descriptor.StorageFileName);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException(
                    "The quarantine descriptor is invalid.",
                    exception);
            }

            return descriptor;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The quarantine descriptor is invalid.",
                exception);
        }
    }

    private static void RestoreAfterDescriptorFailure(
        string quarantinedFilePath,
        string finalFilePath,
        string directoryPath)
    {
        if (!File.Exists(finalFilePath))
        {
            File.Move(
                quarantinedFilePath,
                finalFilePath,
                overwrite: false);
        }

        if (!File.Exists(quarantinedFilePath))
        {
            DeleteQuarantineDirectory(directoryPath);
        }
    }

    private static void DeleteEmptyDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath);
        }
    }

    private static void DeleteQuarantineDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    public RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
        GetStagedUploads()
    {
        List<StagedRecipePhotoUploadInventoryItem> items = [];
        int invalidEntryCount = 0;

        foreach (string entryPath in
                 Directory.GetFileSystemEntries(_stagingPath))
        {
            try
            {
                if (!File.Exists(entryPath))
                {
                    invalidEntryCount++;
                    continue;
                }

                string fileName = Path.GetFileName(entryPath);

                if (!fileName.EndsWith(
                        StagedUploadFileExtension,
                        StringComparison.Ordinal))
                {
                    invalidEntryCount++;
                    continue;
                }

                string token = fileName[
                    ..^StagedUploadFileExtension.Length];

                if (!IsCanonicalToken(token))
                {
                    invalidEntryCount++;
                    continue;
                }

                FileInfo file = new(entryPath);
                file.Refresh();

                if (!file.Exists)
                {
                    invalidEntryCount++;
                    continue;
                }

                items.Add(new StagedRecipePhotoUploadInventoryItem(
                    new StagedRecipePhotoUpload(token, file.Length),
                    file.LastWriteTimeUtc));
            }
            catch (Exception exception)
                when (IsInvalidInventoryEntryException(exception))
            {
                invalidEntryCount++;
            }
        }

        return new RecipePhotoStorageInventory<
            StagedRecipePhotoUploadInventoryItem>(
                items,
                invalidEntryCount);
    }

    public RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
        GetQuarantinedDeletions()
    {
        List<StagedRecipePhotoDeletionInventoryItem> items = [];
        int invalidEntryCount = 0;

        foreach (string entryPath in
                 Directory.GetFileSystemEntries(_quarantinePath))
        {
            try
            {
                if (!Directory.Exists(entryPath))
                {
                    invalidEntryCount++;
                    continue;
                }

                string token = Path.GetFileName(entryPath);

                if (!IsCanonicalToken(token))
                {
                    invalidEntryCount++;
                    continue;
                }

                QuarantineDescriptor descriptor =
                    ReadQuarantineDescriptor(entryPath, token);
                string expectedContentPath = Path.Combine(
                    entryPath,
                    descriptor.StorageFileName);
                string expectedDescriptorPath = Path.Combine(
                    entryPath,
                    QuarantineDescriptorFileName);
                string[] files = Directory.GetFiles(entryPath);

                if (Directory.GetDirectories(entryPath).Length != 0
                    || files.Length != 2
                    || !ContainsExactPath(files, expectedContentPath)
                    || !ContainsExactPath(files, expectedDescriptorPath))
                {
                    invalidEntryCount++;
                    continue;
                }

                items.Add(new StagedRecipePhotoDeletionInventoryItem(
                    new StagedRecipePhotoDeletion(token),
                    descriptor.StorageFileName,
                    descriptor.QuarantinedAtUtc));
            }
            catch (Exception exception)
                when (IsInvalidInventoryEntryException(exception))
            {
                invalidEntryCount++;
            }
        }

        return new RecipePhotoStorageInventory<
            StagedRecipePhotoDeletionInventoryItem>(
                items,
                invalidEntryCount);
    }

    public RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
        GetFinalFiles()
    {
        List<RecipePhotoStorageFileInventoryItem> items = [];
        int invalidEntryCount = 0;

        foreach (string entryPath in
                 Directory.GetFileSystemEntries(_rootPath))
        {
            if (string.Equals(
                    entryPath,
                    _stagingPath,
                    PathComparison)
                || string.Equals(
                    entryPath,
                    _quarantinePath,
                    PathComparison))
            {
                continue;
            }

            try
            {
                if (!File.Exists(entryPath))
                {
                    invalidEntryCount++;
                    continue;
                }

                string storageFileName = Path.GetFileName(entryPath);
                ValidateStorageFileName(storageFileName);

                FileInfo file = new(entryPath);
                file.Refresh();

                if (!file.Exists)
                {
                    invalidEntryCount++;
                    continue;
                }

                items.Add(new RecipePhotoStorageFileInventoryItem(
                    storageFileName,
                    file.LastWriteTimeUtc));
            }
            catch (Exception exception)
                when (IsInvalidInventoryEntryException(exception))
            {
                invalidEntryCount++;
            }
        }

        return new RecipePhotoStorageInventory<
            RecipePhotoStorageFileInventoryItem>(
                items,
                invalidEntryCount);
    }

    private static bool IsCanonicalToken(string? token)
    {
        return !string.IsNullOrWhiteSpace(token)
            && Guid.TryParseExact(token, "N", out Guid identifier)
            && identifier != Guid.Empty
            && string.Equals(
                token,
                identifier.ToString("N"),
                StringComparison.Ordinal);
    }

    private static bool ContainsExactPath(
        IEnumerable<string> paths,
        string expectedPath)
    {
        return paths.Any(path =>
            string.Equals(path, expectedPath, StringComparison.Ordinal));
    }

    private static bool IsInvalidInventoryEntryException(
        Exception exception)
    {
        return exception is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or ArgumentException;
    }

    private sealed class QuarantineDescriptor
    {
        public QuarantineDescriptor()
        {
        }

        public string Token { get; init; } = string.Empty;

        public string StorageFileName { get; init; } = string.Empty;

        public DateTime QuarantinedAtUtc { get; init; }
    }
}
