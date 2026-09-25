using System.Net;
using System.Net.Http.Headers;
using System.Text;

public class RecipeImporterPageDownloaderTests
{
    private const int MaximumResponseSizeInBytes = 5 * 1024 * 1024;

    [Theory]
    [InlineData(HttpStatusCode.MovedPermanently)]
    [InlineData(HttpStatusCode.Redirect)]
    [InlineData(HttpStatusCode.RedirectMethod)]
    [InlineData(HttpStatusCode.TemporaryRedirect)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task DownloadAsync_WithPublicRedirect_ShouldFollowRedirect(
        HttpStatusCode redirectStatus)
    {
        List<Uri> requestedDestinations = [];
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(request =>
        {
            requestedDestinations.Add(request.RequestUri!);

            if (request.RequestUri!.AbsolutePath == "/start")
            {
                return CreateRedirectResponse(
                    redirectStatus,
                    "https://second.example/final");
            }

            return CreateOkResponse("recipe html");
        });
        FakeHostAddressResolver resolver = new FakeHostAddressResolver();
        RecipePageDownloader downloader = CreateDownloader(
            messageHandler,
            resolver);

        string html = await downloader.DownloadAsync(
            new Uri("https://first.example/start"));

        Assert.Equal("recipe html", html);
        Assert.Equal(
            [
                new Uri("https://first.example/start"),
                new Uri("https://second.example/final")
            ],
            requestedDestinations);
        Assert.Equal(
            ["first.example", "second.example"],
            resolver.ResolvedHosts);
    }

    [Fact]
    public async Task DownloadAsync_WithRelativeRedirect_ShouldResolveAgainstCurrentDestination()
    {
        List<Uri> requestedDestinations = [];
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(request =>
        {
            requestedDestinations.Add(request.RequestUri!);

            return requestedDestinations.Count == 1
                ? CreateRedirectResponse(HttpStatusCode.Redirect, "../final")
                : CreateOkResponse("recipe html");
        });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await downloader.DownloadAsync(
            new Uri("https://recipe.example/path/start"));

        Assert.Equal(
            new Uri("https://recipe.example/final"),
            requestedDestinations[1]);
    }

    [Fact]
    public async Task DownloadAsync_WithRedirectToLoopback_ShouldRejectBeforeRequest()
    {
        int requestCount = 0;
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
        {
            requestCount++;

            return CreateRedirectResponse(
                HttpStatusCode.Redirect,
                "https://127.0.0.1/internal");
        });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/start")));

        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task DownloadAsync_WithMoreThanFiveRedirects_ShouldRejectChain()
    {
        int requestCount = 0;
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
        {
            requestCount++;

            return CreateRedirectResponse(
                HttpStatusCode.Redirect,
                $"/redirect-{requestCount}");
        });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/start")));

        Assert.Equal(6, requestCount);
    }

    [Fact]
    public async Task DownloadAsync_WithRedirectWithoutLocation_ShouldRejectResponse()
    {
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Redirect));
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/start")));
    }

    [Fact]
    public async Task DownloadAsync_WithHttpsToHttpRedirect_ShouldRejectDowngrade()
    {
        int requestCount = 0;
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
        {
            requestCount++;

            return CreateRedirectResponse(
                HttpStatusCode.Redirect,
                "http://recipe.example/insecure");
        });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/start")));

        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task DownloadAsync_WithDeclaredContentLengthAboveLimit_ShouldRejectResponse()
    {
        ByteArrayContent oversizedContent = new ByteArrayContent(
            new byte[MaximumResponseSizeInBytes + 1]);
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = oversizedContent
            });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/large")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadAsync_WhenStreamExceedsLimit_ShouldStopReading(
        bool hasFalseContentLength)
    {
        UnknownLengthHttpContent oversizedContent = new UnknownLengthHttpContent(
            new byte[MaximumResponseSizeInBytes + 1]);

        if (hasFalseContentLength)
        {
            oversizedContent.Headers.ContentLength = 1;
        }

        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = oversizedContent
            });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/large")));
    }

    [Fact]
    public async Task DownloadAsync_WithDeclaredCharset_ShouldDecodeContentCorrectly()
    {
        Encoding encoding = Encoding.Latin1;
        ByteArrayContent content = new ByteArrayContent(
            encoding.GetBytes("Recipe café"));
        content.Headers.ContentType = new MediaTypeHeaderValue("text/html")
        {
            CharSet = "iso-8859-1"
        };
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            });
        RecipePageDownloader downloader = CreateDownloader(messageHandler);

        string downloadedContent = await downloader.DownloadAsync(
            new Uri("https://recipe.example/encoded"));

        Assert.Equal("Recipe café", downloadedContent);
    }

    [Fact]
    public async Task DownloadAsync_WhenResponseBodyStalls_ShouldCancelAtOverallDeadline()
    {
        FakeHttpMessageHandler messageHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new NeverCompletingHttpContent()
            });
        RecipePageDownloader downloader = CreateDownloader(
            messageHandler,
            downloadTimeout: TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            downloader.DownloadAsync(new Uri("https://recipe.example/slow")));
    }

    [Fact]
    public void ConfigureHttpClient_ShouldUseThirtySecondTimeout()
    {
        using HttpClient httpClient = new HttpClient();

        RecipeImporterHttpClientConfiguration.Configure(httpClient);

        Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
    }

    private static RecipePageDownloader CreateDownloader(
        HttpMessageHandler messageHandler,
        IHostAddressResolver? hostAddressResolver = null,
        TimeSpan? downloadTimeout = null)
    {
        HttpClient httpClient = new HttpClient(messageHandler);
        httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        RecipeImportDestinationPolicy policy =
            new RecipeImportDestinationPolicy(
                hostAddressResolver ?? new FakeHostAddressResolver());

        return downloadTimeout.HasValue
            ? new RecipePageDownloader(httpClient, policy, downloadTimeout.Value)
            : new RecipePageDownloader(httpClient, policy);
    }

    private static HttpResponseMessage CreateRedirectResponse(
        HttpStatusCode statusCode,
        string location)
    {
        HttpResponseMessage response = new HttpResponseMessage(statusCode);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);

        return response;
    }

    private static HttpResponseMessage CreateOkResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };
    }

    private sealed class FakeHostAddressResolver : IHostAddressResolver
    {
        public List<string> ResolvedHosts { get; } = [];

        public Task<IPAddress[]> GetHostAddressesAsync(
            string host,
            CancellationToken cancellationToken)
        {
            ResolvedHosts.Add(host);

            IPAddress address = IPAddress.TryParse(host, out IPAddress? parsedAddress)
                ? parsedAddress
                : IPAddress.Parse("8.8.8.8");

            return Task.FromResult(new[] { address });
        }
    }

    private sealed class UnknownLengthHttpContent : HttpContent
    {
        private readonly byte[] _content;

        public UnknownLengthHttpContent(byte[] content)
        {
            _content = content;
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            return stream.WriteAsync(_content, 0, _content.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;

            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            Stream stream = new MemoryStream(_content, writable: false);

            return Task.FromResult(stream);
        }
    }

    private sealed class NeverCompletingHttpContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            throw new NotSupportedException();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;

            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(new NeverCompletingStream());
        }
    }

    private sealed class NeverCompletingStream : Stream
    {
        public override bool CanRead => true;
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(
                System.Threading.Timeout.InfiniteTimeSpan,
                cancellationToken);

            return 0;
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
