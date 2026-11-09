using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using CarteDeBucate.Web.Models;

namespace CarteDeBucate.Web.Services.Photos;

public sealed class RecipePhotoUrlImporter : IRecipePhotoUrlImporter
{
    private const int MaximumRedirects = 5;

    private static readonly HashSet<string> SupportedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private readonly HttpClient _httpClient;
    private readonly RecipeImportDestinationPolicy _destinationPolicy;
    private readonly IRecipePhotoService _recipePhotoService;
    private readonly RecipePhotoOptions _options;

    public RecipePhotoUrlImporter(
        HttpClient httpClient,
        RecipeImportDestinationPolicy destinationPolicy,
        IRecipePhotoService recipePhotoService,
        RecipePhotoOptions options)
    {
        _httpClient = httpClient;
        _destinationPolicy = destinationPolicy;
        _recipePhotoService = recipePhotoService;
        _options = options;
    }

    public async Task<RecipePhotoUrlImportResult> ImportAsync(
        int recipeId,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateDestination(imageUrl, out Uri currentDestination))
        {
            return Failure(
                RecipePhotoUrlImportResultCode.InvalidUrl,
                "URL-ul fotografiei nu este valid.");
        }

        int followedRedirects = 0;

        try
        {
            while (true)
            {
                RecipeImportDestinationValidationResult validationResult =
                    await _destinationPolicy.ValidateAsync(
                        currentDestination,
                        cancellationToken);

                if (!validationResult.IsAllowed)
                {
                    return Failure(
                        RecipePhotoUrlImportResultCode.InvalidUrl,
                        "Destinația fotografiei nu este permisă.");
                }

                using HttpRequestMessage request = new(
                    HttpMethod.Get,
                    currentDestination);
                using HttpResponseMessage response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (IsRedirect(response.StatusCode))
                {
                    if (followedRedirects >= MaximumRedirects ||
                        response.Headers.Location == null)
                    {
                        return DownloadFailed();
                    }

                    Uri nextDestination = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(currentDestination, response.Headers.Location);

                    if (currentDestination.Scheme == Uri.UriSchemeHttps &&
                        nextDestination.Scheme == Uri.UriSchemeHttp)
                    {
                        return Failure(
                            RecipePhotoUrlImportResultCode.InvalidUrl,
                            "Redirecționarea fotografiei către HTTP nu este permisă.");
                    }

                    currentDestination = nextDestination;
                    followedRedirects++;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return DownloadFailed();
                }

                long? declaredLength = response.Content.Headers.ContentLength;

                if (declaredLength > _options.MaxFileSizeBytes)
                {
                    return Failure(
                        RecipePhotoUrlImportResultCode.ResponseTooLarge,
                        "Fotografia depășește limita permisă de 10 MiB.");
                }

                string? contentType = response.Content.Headers.ContentType?.MediaType;

                if (string.IsNullOrWhiteSpace(contentType) ||
                    !SupportedContentTypes.Contains(contentType))
                {
                    return Failure(
                        RecipePhotoUrlImportResultCode.UnsupportedContentType,
                        "Fotografia externă nu are un format JPEG, PNG sau WebP acceptat.");
                }

                await using Stream content =
                    await response.Content.ReadAsStreamAsync(cancellationToken);
                RecipePhotoResult photoResult =
                    await _recipePhotoService.AddPhotoAsync(
                        recipeId,
                        content,
                        GetOriginalFileName(
                            response.Content.Headers.ContentDisposition,
                            currentDestination),
                        contentType,
                        declaredLength,
                        RecipePhotoOrigin.RecipeSource,
                        cancellationToken);

                if (!photoResult.IsSuccess)
                {
                    return Failure(
                        RecipePhotoUrlImportResultCode.PhotoServiceFailure,
                        photoResult.Message);
                }

                return RecipePhotoUrlImportResult.Success(
                    "Fotografia rețetei a fost salvată.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DownloadFailed();
        }
        catch (HttpRequestException)
        {
            return DownloadFailed();
        }
        catch (IOException)
        {
            return DownloadFailed();
        }
        catch (SocketException)
        {
            return DownloadFailed();
        }
        catch (UriFormatException)
        {
            return DownloadFailed();
        }
    }

    private static bool TryCreateDestination(string imageUrl, out Uri destination)
    {
        if (Uri.TryCreate(imageUrl?.Trim(), UriKind.Absolute, out destination) &&
            (destination.Scheme == Uri.UriSchemeHttp ||
             destination.Scheme == Uri.UriSchemeHttps) &&
            !string.IsNullOrWhiteSpace(destination.Host))
        {
            return true;
        }

        destination = null!;
        return false;
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

    private static string GetOriginalFileName(
        ContentDispositionHeaderValue? contentDisposition,
        Uri destination)
    {
        string? contentDispositionFileName =
            contentDisposition?.FileNameStar ?? contentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(contentDispositionFileName))
        {
            return contentDispositionFileName.Trim().Trim('"');
        }

        string urlFileName = Path.GetFileName(
            Uri.UnescapeDataString(destination.AbsolutePath));

        return string.IsNullOrWhiteSpace(urlFileName)
            ? "photo"
            : urlFileName;
    }

    private static RecipePhotoUrlImportResult DownloadFailed()
    {
        return Failure(
            RecipePhotoUrlImportResultCode.DownloadFailed,
            "Fotografia externă nu a putut fi descărcată.");
    }

    private static RecipePhotoUrlImportResult Failure(
        RecipePhotoUrlImportResultCode code,
        string message)
    {
        return RecipePhotoUrlImportResult.Failure(code, message);
    }
}
