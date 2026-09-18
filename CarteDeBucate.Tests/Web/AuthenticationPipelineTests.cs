using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarteDeBucate.Web.Models.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public class AuthenticationPipelineTests
    : IClassFixture<AuthenticationApiWebApplicationFactory>
{
    private readonly AuthenticationApiWebApplicationFactory _factory;

    public AuthenticationPipelineTests(
        AuthenticationApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateRecipe_WithoutToken_ShouldReturnUnauthorized()
    {
        using HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRecipeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipe_WithInvalidToken_ShouldReturnUnauthorized()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRecipeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipe_WithValidToken_ShouldReachTheCurrentUserContext()
    {
        _factory.SaveProbe.Reset();
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken("42", "chef"));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRecipeRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(_factory.SaveProbe.IsAuthenticated);
        Assert.Equal(42, _factory.SaveProbe.UserId);
        Assert.Equal("chef", _factory.SaveProbe.Username);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        using HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/authentication/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturnTheTokenIdentity()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken("42", "chef"));

        HttpResponseMessage response = await client.GetAsync(
            "/api/authentication/me");
        CurrentUserResponse? currentUser =
            await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(currentUser);
        Assert.Equal("42", currentUser.UserId);
        Assert.Equal("chef", currentUser.Username);
    }

    [Fact]
    public async Task Cors_WithConfiguredExtensionOrigin_ShouldAllowTheOrigin()
    {
        using HttpClient client = CreateClient();
        using HttpRequestMessage request = CreateCorsPreflightRequest(
            AuthenticationApiWebApplicationFactory.AllowedExtensionOrigin);

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(
            response.Headers.TryGetValues(
                "Access-Control-Allow-Origin",
                out IEnumerable<string>? allowedOrigins));
        Assert.Equal(
            AuthenticationApiWebApplicationFactory.AllowedExtensionOrigin,
            Assert.Single(allowedOrigins));
    }

    [Fact]
    public async Task Cors_WithUnknownOrigin_ShouldNotAllowTheOrigin()
    {
        using HttpClient client = CreateClient();
        using HttpRequestMessage request = CreateCorsPreflightRequest(
            "chrome-extension://unknown-extension-id");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static CreateRecipeRequest CreateRecipeRequest()
    {
        return new CreateRecipeRequest
        {
            Name = "Pipeline soup",
            SourceUrl = "https://recipes.example.test/pipeline-soup",
            Ingredients = ["Water", "Salt"],
            Steps = ["Boil.", "Serve."]
        };
    }

    private static HttpRequestMessage CreateCorsPreflightRequest(string origin)
    {
        HttpRequestMessage request = new(HttpMethod.Options, "/api/recipes");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        return request;
    }

    private sealed class CurrentUserResponse
    {
        public string? UserId { get; init; }

        public string? Username { get; init; }
    }
}

public sealed class AuthenticationApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public const string AllowedExtensionOrigin =
        "chrome-extension://integration-test-extension-id";

    private const string TestJwtKey =
        "integration-test-signing-key-that-is-long-enough-for-hs256-1234567890";
    private const string TestJwtIssuer = "CarteDeBucate.IntegrationTests";
    private const string TestJwtAudience = "RecipeClipper.IntegrationTests";

    private readonly string _temporaryDirectory;

    public AuthenticationApiWebApplicationFactory()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "CarteDeBucate.AuthenticationPipelineTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public RecipeSaveProbe SaveProbe { get; } = new();

    public string CreateToken(string userId, string username)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateSettings())
            .Build();

        return new JwtTokenService(configuration)
            .CreateToken(userId, username);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(CreateSettings());
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(CreateSettings());
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRecipeLibraryService>();
            services.AddSingleton(SaveProbe);
            services.AddScoped<IRecipeLibraryService>(serviceProvider =>
                new PipelineRecipeLibraryService(
                    serviceProvider.GetRequiredService<ICurrentUserContext>(),
                    serviceProvider.GetRequiredService<RecipeSaveProbe>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private Dictionary<string, string?> CreateSettings()
    {
        return new Dictionary<string, string?>
        {
            ["DatabasePath"] = Path.Combine(_temporaryDirectory, "recipes.db"),
            ["AuthenticationEnabled"] = "true",
            ["Jwt:Key"] = TestJwtKey,
            ["Jwt:Issuer"] = TestJwtIssuer,
            ["Jwt:Audience"] = TestJwtAudience,
            ["Jwt:ExpirationMinutes"] = "30",
            ["Cors:AllowedOrigins:0"] = AllowedExtensionOrigin,
            ["ChromeExtension:AuthenticationRedirectUrl"] =
                "https://integration-test-extension-id.chromiumapp.org/authentication-callback"
        };
    }
}

public sealed class RecipeSaveProbe
{
    public bool IsAuthenticated { get; private set; }

    public int? UserId { get; private set; }

    public string? Username { get; private set; }

    public void Capture(ICurrentUserContext currentUserContext)
    {
        IsAuthenticated = currentUserContext.IsAuthenticated;
        UserId = currentUserContext.UserId;
        Username = currentUserContext.Username;
    }

    public void Reset()
    {
        IsAuthenticated = false;
        UserId = null;
        Username = null;
    }
}

public sealed class PipelineRecipeLibraryService : IRecipeLibraryService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly RecipeSaveProbe _saveProbe;

    public PipelineRecipeLibraryService(
        ICurrentUserContext currentUserContext,
        RecipeSaveProbe saveProbe)
    {
        _currentUserContext = currentUserContext;
        _saveProbe = saveProbe;
    }

    public RecipeSaveResult SaveRecipe(Recipe recipe)
    {
        _saveProbe.Capture(_currentUserContext);
        recipe.Id = 321;

        return RecipeSaveResult.Success(
            "Recipe saved successfully.",
            recipe);
    }

    public List<RecipeSummary> GetRecipeSummaries()
    {
        throw new NotSupportedException();
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPage(
        int pageNumber,
        int pageSize)
    {
        throw new NotSupportedException();
    }

    public bool HasRecipesInCurrentContext()
    {
        throw new NotSupportedException();
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        throw new NotSupportedException();
    }

    public List<RecipeSummary> SearchRecipes(string searchText)
    {
        throw new NotSupportedException();
    }

    public PagedResult<RecipeSummary> SearchRecipesPage(
        string searchText,
        int pageNumber,
        int pageSize)
    {
        throw new NotSupportedException();
    }

    public RecipeSaveResult UpdateRecipe(Recipe recipe)
    {
        throw new NotSupportedException();
    }

    public RecipeSaveResult DeleteRecipe(int recipeId)
    {
        throw new NotSupportedException();
    }
}
