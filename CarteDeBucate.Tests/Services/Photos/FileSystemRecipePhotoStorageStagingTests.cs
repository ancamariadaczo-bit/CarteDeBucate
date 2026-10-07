public sealed class FileSystemRecipePhotoStorageStagingTests : IDisposable
{
    private readonly string _rootPath;
    private readonly FileSystemRecipePhotoStorage _storage;

    public FileSystemRecipePhotoStorageStagingTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photo-staging-tests-{Guid.NewGuid():N}");

        _storage = new FileSystemRecipePhotoStorage(_rootPath);
    }

    [Fact]
    public async Task StageUploadAsync_WithSeekableStream_ShouldCopyContent()
    {
        byte[] content = [1, 2, 3, 4];
        using MemoryStream source = new(content);

        StagedRecipePhotoUpload upload = await _storage.StageUploadAsync(
            source,
            maximumBytes: content.Length);

        Assert.Equal(content.Length, upload.Length);
        Assert.Equal(content, File.ReadAllBytes(GetSingleStagedFile()));
        Assert.True(source.CanRead);
        Assert.Empty(Directory.GetFiles(_rootPath));
    }

    [Fact]
    public async Task StageUploadAsync_WithNonSeekableStream_ShouldCopyContent()
    {
        byte[] content = [5, 6, 7];
        using NonSeekableReadStream source = new(content);

        StagedRecipePhotoUpload upload = await _storage.StageUploadAsync(
            source,
            maximumBytes: content.Length);

        Assert.Equal(content.Length, upload.Length);
        Assert.Equal(content, File.ReadAllBytes(GetSingleStagedFile()));
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task StageUploadAsync_WithEmptyContent_ShouldCreateEmptyUpload()
    {
        using MemoryStream source = new();

        StagedRecipePhotoUpload upload = await _storage.StageUploadAsync(
            source,
            maximumBytes: 10);

        Assert.Equal(0, upload.Length);
        Assert.Equal(0, new FileInfo(GetSingleStagedFile()).Length);
    }

    [Fact]
    public async Task StageUploadAsync_AtExactLimit_ShouldSucceed()
    {
        byte[] content = [1, 2, 3, 4];
        using MemoryStream source = new(content);

        StagedRecipePhotoUpload upload = await _storage.StageUploadAsync(
            source,
            maximumBytes: content.Length);

        Assert.Equal(content.Length, upload.Length);
        Assert.Single(GetStagedFiles());
    }

    [Fact]
    public async Task StageUploadAsync_OneByteOverLimit_ShouldThrowAndDeletePartialFile()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using MemoryStream source = new(content);

        RecipePhotoStorageLimitExceededException exception =
            await Assert.ThrowsAsync<RecipePhotoStorageLimitExceededException>(
                () => _storage.StageUploadAsync(
                    source,
                    maximumBytes: content.Length - 1));

        Assert.Equal(content.Length - 1, exception.MaximumBytes);
        Assert.Empty(GetStagedFiles());
    }

    [Fact]
    public async Task StageUploadAsync_WhenCancelled_ShouldDeletePartialFile()
    {
        using CancellationTokenSource cancellation = new();
        using CancelAfterFirstReadStream source = new(cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _storage.StageUploadAsync(
                source,
                maximumBytes: 10,
                cancellation.Token));

        Assert.Empty(GetStagedFiles());
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task StageUploadAsync_WhenSourceReadFails_ShouldDeletePartialFile()
    {
        using ThrowAfterFirstReadStream source = new();

        await Assert.ThrowsAsync<IOException>(() =>
            _storage.StageUploadAsync(source, maximumBytes: 10));

        Assert.Empty(GetStagedFiles());
        Assert.True(source.CanRead);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private string[] GetStagedFiles()
    {
        return Directory.GetFiles(
            Path.Combine(
                _rootPath,
                FileSystemRecipePhotoStorage.StagingDirectoryName));
    }

    private string GetSingleStagedFile()
    {
        return Assert.Single(GetStagedFiles());
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableReadStream(byte[] content)
        {
            _inner = new MemoryStream(content);
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        public override void Flush()
        {
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
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class CancelAfterFirstReadStream : Stream
    {
        private readonly CancellationTokenSource _cancellation;
        private bool _hasRead;

        public CancelAfterFirstReadStream(
            CancellationTokenSource cancellation)
        {
            _cancellation = cancellation;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_hasRead)
            {
                return ValueTask.FromResult(0);
            }

            _hasRead = true;
            buffer.Span[0] = 1;
            _cancellation.Cancel();
            return ValueTask.FromResult(1);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
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
    }

    private sealed class ThrowAfterFirstReadStream : Stream
    {
        private bool _hasRead;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_hasRead)
            {
                return ValueTask.FromException<int>(
                    new IOException("Simulated read failure."));
            }

            _hasRead = true;
            buffer.Span[0] = 1;
            return ValueTask.FromResult(1);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
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
    }
}
