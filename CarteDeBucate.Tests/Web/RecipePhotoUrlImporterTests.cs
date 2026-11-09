using System.Net;
using System.Net.Http.Headers;
using CarteDeBucate.Web.Models;
using CarteDeBucate.Web.Services.Photos;

public class RecipePhotoUrlImporterTests
{
    [Fact]
    public async Task ImportAsync_WithSupportedImage_ShouldAddRecipeSourcePhoto()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService();
        HttpResponseMessage response = CreateImageResponse("image/jpeg");
        response.Content.Headers.ContentDisposition =
            new ContentDispositionHeaderValue("inline")
            {
                FileName = "recipe.jpg"
            };
        RecipePhotoUrlImporter importer = CreateImporter(
            _ => response,
            photoService);

        RecipePhotoUrlImportResult result = await importer.ImportAsync(
            42,
            "https://example.com/images/source");

        Assert.True(result.IsSuccess);
        Assert.True(photoService.AddPhotoAsyncWasCalled);
        Assert.Equal(42, photoService.RecipeIdPassedToAddPhoto);
        Assert.Equal("recipe.jpg", photoService.OriginalFileNamePassedToAddPhoto);
        Assert.Equal("image/jpeg", photoService.DeclaredContentTypePassedToAddPhoto);
        Assert.Equal(RecipePhotoOrigin.RecipeSource, photoService.OriginPassedToAddPhoto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("not-a-url")]
    public async Task ImportAsync_WithInvalidUrl_ShouldNotSendRequest(string imageUrl)
    {
        int requestCount = 0;
        RecipePhotoUrlImporter importer = CreateImporter(
            _ =>
            {
                requestCount++;
                return CreateImageResponse("image/jpeg");
            });

        RecipePhotoUrlImportResult result =
            await importer.ImportAsync(42, imageUrl);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoUrlImportResultCode.InvalidUrl, result.Code);
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task ImportAsync_WithPrivateDestination_ShouldNotSendRequest()
    {
        int requestCount = 0;
        RecipePhotoUrlImporter importer = CreateImporter(
            _ =>
            {
                requestCount++;
                return CreateImageResponse("image/jpeg");
            },
            addresses: [IPAddress.Loopback]);

        RecipePhotoUrlImportResult result = await importer.ImportAsync(
            42,
            "https://localhost/image.jpg");

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoUrlImportResultCode.InvalidUrl, result.Code);
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task ImportAsync_WithOversizedContentLength_ShouldRejectBeforeService()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService();
        HttpResponseMessage response = CreateImageResponse("image/png");
        response.Content.Headers.ContentLength = 11 * 1024 * 1024;
        RecipePhotoUrlImporter importer = CreateImporter(
            _ => response,
            photoService);

        RecipePhotoUrlImportResult result = await importer.ImportAsync(
            42,
            "https://example.com/image.png");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            RecipePhotoUrlImportResultCode.ResponseTooLarge,
            result.Code);
        Assert.False(photoService.AddPhotoAsyncWasCalled);
    }

    [Fact]
    public async Task ImportAsync_WithUnsupportedMimeType_ShouldRejectBeforeService()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService();
        RecipePhotoUrlImporter importer = CreateImporter(
            _ => CreateImageResponse("image/gif"),
            photoService);

        RecipePhotoUrlImportResult result = await importer.ImportAsync(
            42,
            "https://example.com/image.gif");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            RecipePhotoUrlImportResultCode.UnsupportedContentType,
            result.Code);
        Assert.False(photoService.AddPhotoAsyncWasCalled);
    }

    [Fact]
    public async Task ImportAsync_WithHttpsToHttpRedirect_ShouldRejectDowngrade()
    {
        RecipePhotoUrlImporter importer = CreateImporter(_ =>
        {
            HttpResponseMessage response =
                new(HttpStatusCode.Redirect);
            response.Headers.Location =
                new Uri("http://example.com/image.jpg");

            return response;
        });

        RecipePhotoUrlImportResult result = await importer.ImportAsync(
            42,
            "https://example.com/start");

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoUrlImportResultCode.InvalidUrl, result.Code);
    }

    [Fact]
    public async Task ImportAsync_WhenPhotoServiceFails_ShouldReturnControlledFailure()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService
        {
            AddPhotoResultToReturn = RecipePhotoResult.Failure(
                RecipePhotoResultCode.InvalidContent,
                "Invalid photo content.")
        };
        RecipePhotoUrlImporter importer = CreateImporter(
            _ => CreateImageResponse("image/webp"),
            photoService);

        RecipePhotoUrlImportResult result = await importer.ImportAsync(
            42,
            "https://example.com/image.webp");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            RecipePhotoUrlImportResultCode.PhotoServiceFailure,
            result.Code);
        Assert.Equal("Invalid photo content.", result.Message);
    }

    private static RecipePhotoUrlImporter CreateImporter(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        FakeRecipePhotoService? photoService = null,
        IPAddress[]? addresses = null)
    {
        HttpClient httpClient = new HttpClient(
            new FakeHttpMessageHandler(responseFactory));
        RecipeImportDestinationPolicy destinationPolicy = new(
            new FixedHostAddressResolver(
                addresses ?? [IPAddress.Parse("93.184.216.34")]));

        return new RecipePhotoUrlImporter(
            httpClient,
            destinationPolicy,
            photoService ?? new FakeRecipePhotoService(),
            new RecipePhotoOptions());
    }

    private static HttpResponseMessage CreateImageResponse(string contentType)
    {
        ByteArrayContent content = new([0xff, 0xd8, 0xff, 0xd9]);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content
        };
    }

    private sealed class FixedHostAddressResolver : IHostAddressResolver
    {
        private readonly IPAddress[] _addresses;

        public FixedHostAddressResolver(IPAddress[] addresses)
        {
            _addresses = addresses;
        }

        public Task<IPAddress[]> GetHostAddressesAsync(
            string host,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_addresses);
        }
    }
}
