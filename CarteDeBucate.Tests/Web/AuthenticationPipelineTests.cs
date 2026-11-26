using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarteDeBucate.Web.Models.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
    public void Services_WithoutRedisConnectionString_ShouldNotRegisterRecipeCache()
    {
        using IServiceScope scope = _factory.Services.CreateScope();

        IRecipeCacheService? recipeCacheService =
            scope.ServiceProvider.GetService<IRecipeCacheService>();

        Assert.Null(recipeCacheService);
    }

    [Fact]
    public void Services_InTestingEnvironment_ShouldUseNonDeliveringEmailSender()
    {
        IEmailSender emailSender =
            _factory.Services.GetRequiredService<IEmailSender>();

        Assert.IsType<InMemoryEmailSender>(emailSender);
        Assert.IsType<EmailTemplateRenderer>(
            _factory.Services.GetRequiredService<IEmailTemplateRenderer>());
        Assert.IsType<AccountLinkBuilder>(
            _factory.Services.GetRequiredService<IAccountLinkBuilder>());
    }

    [Fact]
    public void AuthenticationCookie_ShouldUseTheEmailAuthenticationVersion()
    {
        IOptionsMonitor<CookieAuthenticationOptions> optionsMonitor =
            _factory.Services.GetRequiredService<
                IOptionsMonitor<CookieAuthenticationOptions>>();

        CookieAuthenticationOptions options = optionsMonitor.Get(
            CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("CarteDeBucate.Auth.v2", options.Cookie.Name);
    }

    [Fact]
    public void Services_WithRedisConnectionString_ShouldRegisterRecipeCache()
    {
        using AuthenticationApiWebApplicationFactory factory =
            new("localhost:6379");
        using IServiceScope scope = factory.Services.CreateScope();

        IRecipeCacheService recipeCacheService =
            scope.ServiceProvider.GetRequiredService<IRecipeCacheService>();

        Assert.IsType<RedisRecipeCacheService>(recipeCacheService);
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
                _factory.CreateToken("1", "chef@example.com"));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRecipeRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(_factory.SaveProbe.IsAuthenticated);
        Assert.Equal(1, _factory.SaveProbe.UserId);
        Assert.Equal("chef@example.com", _factory.SaveProbe.Email);
    }

    [Fact]
    public async Task RecipeExists_WithoutToken_ShouldReturnUnauthorized()
    {
        using HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            CreateRecipeExistsRequestUri());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RecipeExists_WithInvalidToken_ShouldReturnUnauthorized()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        HttpResponseMessage response = await client.GetAsync(
            CreateRecipeExistsRequestUri());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RecipeExists_WithValidToken_ShouldReturnBooleanAndReachCurrentUserContext()
    {
        _factory.ExistsProbe.Reset();
        _factory.ExistsProbe.ResultToReturn = true;
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken("1", "chef@example.com"));

        HttpResponseMessage response = await client.GetAsync(
            CreateRecipeExistsRequestUri());
        RecipeExistsResponse? result =
            await response.Content.ReadFromJsonAsync<RecipeExistsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Equal(1, _factory.ExistsProbe.CallCount);
        Assert.Equal(
            "https://recipes.example.test/pipeline-soup",
            _factory.ExistsProbe.SourceUrl);
        Assert.True(_factory.ExistsProbe.IsAuthenticated);
        Assert.Equal(1, _factory.ExistsProbe.UserId);
        Assert.Equal("chef@example.com", _factory.ExistsProbe.Email);
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
                _factory.CreateToken("1", "chef@example.com"));

        HttpResponseMessage response = await client.GetAsync(
            "/api/authentication/me");
        CurrentUserResponse? currentUser =
            await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(currentUser);
        Assert.Equal("1", currentUser.UserId);
        Assert.Equal("chef@example.com", currentUser.Email);
    }

    [Fact]
    public async Task InvalidateAllSessions_ShouldRejectOnlyTheTargetUsersOldCredentials()
    {
        using AuthenticationApiWebApplicationFactory factory = new();
        string userACookie = factory.CreateCookie(
            AuthenticationApiWebApplicationFactory.UserAId,
            AuthenticationApiWebApplicationFactory.UserAEmail,
            securityVersion: 1);
        string userAToken = factory.CreateToken(
            AuthenticationApiWebApplicationFactory.UserAId.ToString(),
            AuthenticationApiWebApplicationFactory.UserAEmail,
            securityVersion: 1);
        string userBToken = factory.CreateToken(
            AuthenticationApiWebApplicationFactory.UserBId.ToString(),
            AuthenticationApiWebApplicationFactory.UserBEmail,
            securityVersion: 1);

        using HttpClient currentCookieClient = CreateClient(factory);
        currentCookieClient.DefaultRequestHeaders.Add("Cookie", userACookie);
        HttpResponseMessage currentCookieResponse = await currentCookieClient.GetAsync(
            "/api/authentication/extension-complete");

        Assert.Equal(HttpStatusCode.Found, currentCookieResponse.StatusCode);

        using HttpClient currentTokenClient = CreateClient(factory);
        currentTokenClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);
        HttpResponseMessage currentTokenResponse = await currentTokenClient.GetAsync(
            "/api/authentication/me");

        Assert.Equal(HttpStatusCode.OK, currentTokenResponse.StatusCode);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            IGlobalSessionInvalidationService invalidationService =
                scope.ServiceProvider.GetRequiredService<
                    IGlobalSessionInvalidationService>();

            Assert.True(invalidationService.InvalidateAllSessions(
                AuthenticationApiWebApplicationFactory.UserAId));
        }

        HttpResponseMessage staleCookieResponse = await currentCookieClient.GetAsync(
            "/api/authentication/extension-complete");

        Assert.Equal(HttpStatusCode.Unauthorized, staleCookieResponse.StatusCode);
        Assert.Contains(
            staleCookieResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(
                "CarteDeBucate.Auth.v2=",
                StringComparison.Ordinal));

        HttpResponseMessage staleTokenCurrentUserResponse =
            await currentTokenClient.GetAsync("/api/authentication/me");
        HttpResponseMessage staleTokenProtectedApiResponse =
            await currentTokenClient.GetAsync(CreateRecipeExistsRequestUri());

        Assert.Equal(HttpStatusCode.Unauthorized, staleTokenCurrentUserResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staleTokenProtectedApiResponse.StatusCode);

        using HttpClient userBClient = CreateClient(factory);
        userBClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);
        HttpResponseMessage userBResponse = await userBClient.GetAsync(
            "/api/authentication/me");

        Assert.Equal(HttpStatusCode.OK, userBResponse.StatusCode);

        using HttpClient renewedTokenClient = CreateClient(factory);
        renewedTokenClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                factory.CreateToken(
                    AuthenticationApiWebApplicationFactory.UserAId.ToString(),
                    AuthenticationApiWebApplicationFactory.UserAEmail,
                    securityVersion: 2));
        HttpResponseMessage renewedTokenResponse = await renewedTokenClient.GetAsync(
            "/api/authentication/me");

        Assert.Equal(HttpStatusCode.OK, renewedTokenResponse.StatusCode);

        using HttpClient renewedCookieClient = CreateClient(factory);
        renewedCookieClient.DefaultRequestHeaders.Add(
            "Cookie",
            factory.CreateCookie(
                AuthenticationApiWebApplicationFactory.UserAId,
                AuthenticationApiWebApplicationFactory.UserAEmail,
                securityVersion: 2));
        HttpResponseMessage renewedCookieResponse = await renewedCookieClient.GetAsync(
            "/api/authentication/extension-complete");

        Assert.Equal(HttpStatusCode.Found, renewedCookieResponse.StatusCode);
    }

    [Fact]
    public async Task MissingSecurityVersionCredentials_ShouldBeRejected()
    {
        using AuthenticationApiWebApplicationFactory factory = new();

        using HttpClient missingVersionTokenClient = CreateClient(factory);
        missingVersionTokenClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                factory.CreateToken(
                    AuthenticationApiWebApplicationFactory.UserAId.ToString(),
                    AuthenticationApiWebApplicationFactory.UserAEmail,
                    includeSecurityVersion: false));
        HttpResponseMessage missingVersionTokenResponse =
            await missingVersionTokenClient.GetAsync("/api/authentication/me");

        Assert.Equal(HttpStatusCode.Unauthorized, missingVersionTokenResponse.StatusCode);

        using HttpClient missingVersionCookieClient = CreateClient(factory);
        missingVersionCookieClient.DefaultRequestHeaders.Add(
            "Cookie",
            factory.CreateCookie(
                AuthenticationApiWebApplicationFactory.UserAId,
                AuthenticationApiWebApplicationFactory.UserAEmail,
                securityVersion: 1,
                includeSecurityVersion: false));
        HttpResponseMessage missingVersionCookieResponse =
            await missingVersionCookieClient.GetAsync(
                "/api/authentication/extension-complete");

        Assert.Equal(HttpStatusCode.Unauthorized, missingVersionCookieResponse.StatusCode);
        Assert.Contains(
            missingVersionCookieResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(
                "CarteDeBucate.Auth.v2=",
                StringComparison.Ordinal));
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
        return CreateClient(_factory);
    }

    private static HttpClient CreateClient(AuthenticationApiWebApplicationFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
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

    private static string CreateRecipeExistsRequestUri()
    {
        string sourceUrl = Uri.EscapeDataString(
            "https://recipes.example.test/pipeline-soup");

        return $"/api/recipes/exists?sourceUrl={sourceUrl}";
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

        public string? Email { get; init; }
    }

    private sealed class RecipeExistsResponse
    {
        public bool Exists { get; init; }
    }
}

public sealed class AuthenticationApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public const int UserAId = 1;
    public const string UserAEmail = "pipeline-user@example.com";
    public const int UserBId = 2;
    public const string UserBEmail = "pipeline-other-user@example.com";
    public const string AllowedExtensionOrigin =
        "chrome-extension://integration-test-extension-id";

    private const string TestJwtKey =
        "integration-test-signing-key-that-is-long-enough-for-hs256-1234567890";
    private const string TestJwtIssuer = "CarteDeBucate.IntegrationTests";
    private const string TestJwtAudience = "RecipeClipper.IntegrationTests";

    private readonly string _temporaryDirectory;
    private readonly string _redisConnectionString;

    public AuthenticationApiWebApplicationFactory()
        : this(string.Empty)
    {
    }

    internal AuthenticationApiWebApplicationFactory(string redisConnectionString)
    {
        _redisConnectionString = redisConnectionString;
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "CarteDeBucate.AuthenticationPipelineTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public RecipeSaveProbe SaveProbe { get; } = new();
    public RecipeExistsProbe ExistsProbe { get; } = new();

    public string CreateToken(
        string userId,
        string email,
        long securityVersion = 1,
        bool includeSecurityVersion = true)
    {
        if (!includeSecurityVersion)
        {
            return CreateTokenWithoutSecurityVersion(userId, email);
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateSettings())
            .Build();

        return new JwtTokenService(configuration)
            .CreateToken(userId, email, securityVersion);
    }

    public string CreateCookie(
        int userId,
        string email,
        long securityVersion,
        bool includeSecurityVersion = true)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email)
        ];

        if (includeSecurityVersion)
        {
            claims.Add(new Claim(
                AuthenticationClaimTypes.SecurityVersion,
                securityVersion.ToString()));
        }

        ClaimsIdentity identity = new(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationTicket ticket = new(
            new ClaimsPrincipal(identity),
            CookieAuthenticationDefaults.AuthenticationScheme);
        IOptionsMonitor<CookieAuthenticationOptions> optionsMonitor =
            Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        CookieAuthenticationOptions options = optionsMonitor.Get(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}";
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(CreateSettings());
        });

        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();
        IUserRepository userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User userA = new()
        {
            Email = UserAEmail,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            CreatedAt = DateTime.UtcNow,
            EmailVerifiedAtUtc = DateTime.UtcNow
        };
        User userB = new()
        {
            Email = UserBEmail,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            CreatedAt = DateTime.UtcNow,
            EmailVerifiedAtUtc = DateTime.UtcNow
        };
        userRepository.Add(userA);
        userRepository.Add(userB);

        if (userA.Id != UserAId || userB.Id != UserBId)
        {
            throw new InvalidOperationException(
                "The authentication test users were not assigned the expected identifiers.");
        }

        return host;
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
            services.AddSingleton(ExistsProbe);
            services.AddScoped<IRecipeLibraryService>(serviceProvider =>
                new PipelineRecipeLibraryService(
                    serviceProvider.GetRequiredService<ICurrentUserContext>(),
                    serviceProvider.GetRequiredService<RecipeSaveProbe>(),
                    serviceProvider.GetRequiredService<RecipeExistsProbe>()));
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
            ["Application:PublicBaseUrl"] = "https://integration.example.test",
            ["Jwt:Key"] = TestJwtKey,
            ["Jwt:Issuer"] = TestJwtIssuer,
            ["Jwt:Audience"] = TestJwtAudience,
            ["Jwt:ExpirationMinutes"] = "30",
            ["ConnectionStrings:Redis"] = _redisConnectionString,
            ["Cors:AllowedOrigins:0"] = AllowedExtensionOrigin,
            ["ChromeExtension:AuthenticationRedirectUrl"] =
                "https://integration-test-extension-id.chromiumapp.org/authentication-callback"
        };
    }

    private static string CreateTokenWithoutSecurityVersion(string userId, string email)
    {
        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email)
        ];
        SigningCredentials signingCredentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)),
            SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class RecipeSaveProbe
{
    public bool IsAuthenticated { get; private set; }

    public int? UserId { get; private set; }

    public string? Email { get; private set; }

    public void Capture(ICurrentUserContext currentUserContext)
    {
        IsAuthenticated = currentUserContext.IsAuthenticated;
        UserId = currentUserContext.UserId;
        Email = currentUserContext.Email;
    }

    public void Reset()
    {
        IsAuthenticated = false;
        UserId = null;
        Email = null;
    }
}

public sealed class RecipeExistsProbe
{
    public bool ResultToReturn { get; set; }

    public int CallCount { get; private set; }

    public string? SourceUrl { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public int? UserId { get; private set; }

    public string? Email { get; private set; }

    public bool Capture(
        string sourceUrl,
        ICurrentUserContext currentUserContext)
    {
        CallCount++;
        SourceUrl = sourceUrl;
        IsAuthenticated = currentUserContext.IsAuthenticated;
        UserId = currentUserContext.UserId;
        Email = currentUserContext.Email;

        return ResultToReturn;
    }

    public void Reset()
    {
        ResultToReturn = false;
        CallCount = 0;
        SourceUrl = null;
        IsAuthenticated = false;
        UserId = null;
        Email = null;
    }
}

public sealed class PipelineRecipeLibraryService : IRecipeLibraryService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly RecipeSaveProbe _saveProbe;
    private readonly RecipeExistsProbe _existsProbe;

    public PipelineRecipeLibraryService(
        ICurrentUserContext currentUserContext,
        RecipeSaveProbe saveProbe,
        RecipeExistsProbe existsProbe)
    {
        _currentUserContext = currentUserContext;
        _saveProbe = saveProbe;
        _existsProbe = existsProbe;
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

    public bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        return _existsProbe.Capture(sourceUrl, _currentUserContext);
    }

    public RecipeSaveResult UpdateRecipe(Recipe recipe)
    {
        throw new NotSupportedException();
    }

    public RecipeSaveResult DeleteRecipe(int recipeId)
    {
        throw new NotSupportedException();
    }

    public Task<Recipe?> GetRecipeByIdAsync(int recipeId)
    {
        throw new NotSupportedException();
    }

    public Task<RecipeSaveResult> UpdateRecipeAsync(Recipe recipe)
    {
        throw new NotSupportedException();
    }
    public Task<RecipeSaveResult> DeleteRecipeAsync(int recipeId)
    {
        throw new NotSupportedException();
    }
}
