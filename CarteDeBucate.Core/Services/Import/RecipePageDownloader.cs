using System.Net;
using System.Text;

public sealed class RecipePageDownloader
{
    private const int MaximumRedirects = 5;
    private const int MaximumResponseSizeInBytes = 5 * 1024 * 1024;
    private const int ReadBufferSizeInBytes = 81920;

    private readonly HttpClient _httpClient;
    private readonly RecipeImportDestinationPolicy _destinationPolicy;
    private readonly TimeSpan _downloadTimeout;

    public RecipePageDownloader(
        HttpClient httpClient,
        RecipeImportDestinationPolicy destinationPolicy)
        : this(
            httpClient,
            destinationPolicy,
            RecipeImporterHttpClientConfiguration.Timeout)
    {
    }

    public RecipePageDownloader(
        HttpClient httpClient,
        RecipeImportDestinationPolicy destinationPolicy,
        TimeSpan downloadTimeout)
    {
        if (downloadTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(downloadTimeout));
        }

        _httpClient = httpClient;
        _destinationPolicy = destinationPolicy;
        _downloadTimeout = downloadTimeout;
    }

    public async Task<string> DownloadAsync(
        Uri initialDestination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(initialDestination);

        using CancellationTokenSource downloadCancellationSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        downloadCancellationSource.CancelAfter(_downloadTimeout);
        CancellationToken downloadCancellationToken =
            downloadCancellationSource.Token;

        Uri currentDestination = initialDestination;
        int followedRedirects = 0;

        while (true)
        {
            RecipeImportDestinationValidationResult validationResult =
                await _destinationPolicy.ValidateAsync(
                    currentDestination,
                    downloadCancellationToken);

            if (!validationResult.IsAllowed)
            {
                throw CreateDownloadException();
            }

            using HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                currentDestination);

            RecipeImportRequestOptions.SetApprovedDestination(
                request,
                currentDestination,
                validationResult.Addresses);

            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                downloadCancellationToken);

            if (!IsRedirect(response.StatusCode))
            {
                response.EnsureSuccessStatusCode();

                return await ReadContentAsync(
                    response.Content,
                    downloadCancellationToken);
            }

            if (followedRedirects >= MaximumRedirects)
            {
                throw CreateDownloadException();
            }

            Uri? location = response.Headers.Location;

            if (location == null)
            {
                throw CreateDownloadException();
            }

            Uri nextDestination = location.IsAbsoluteUri
                ? location
                : new Uri(currentDestination, location);

            if (currentDestination.Scheme == Uri.UriSchemeHttps &&
                nextDestination.Scheme == Uri.UriSchemeHttp)
            {
                throw CreateDownloadException();
            }

            currentDestination = nextDestination;
            followedRedirects++;
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.MovedPermanently or
            HttpStatusCode.Redirect or
            HttpStatusCode.RedirectMethod or
            HttpStatusCode.TemporaryRedirect or
            HttpStatusCode.PermanentRedirect;
    }

    private static async Task<string> ReadContentAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        long? declaredLength = content.Headers.ContentLength;

        if (declaredLength > MaximumResponseSizeInBytes)
        {
            throw CreateDownloadException();
        }

        int initialCapacity = declaredLength is > 0
            ? (int)declaredLength.Value
            : 0;

        await using Stream contentStream =
            await content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream bufferedContent = new MemoryStream(initialCapacity);
        byte[] readBuffer = new byte[ReadBufferSizeInBytes];

        while (true)
        {
            int bytesRead = await contentStream.ReadAsync(
                readBuffer.AsMemory(),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            if (bufferedContent.Length + bytesRead > MaximumResponseSizeInBytes)
            {
                throw CreateDownloadException();
            }

            await bufferedContent.WriteAsync(
                readBuffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        bufferedContent.Position = 0;

        using StreamReader reader = new StreamReader(
            bufferedContent,
            GetContentEncoding(content),
            detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static Encoding GetContentEncoding(HttpContent content)
    {
        string? charset = content.Headers.ContentType?.CharSet;

        if (string.IsNullOrWhiteSpace(charset))
        {
            return Encoding.UTF8;
        }

        return Encoding.GetEncoding(charset.Trim().Trim('"', '\''));
    }

    private static HttpRequestException CreateDownloadException()
    {
        return new HttpRequestException("Recipe page could not be downloaded.");
    }
}
