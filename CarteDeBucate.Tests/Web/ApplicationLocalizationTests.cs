using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

public class ApplicationLocalizationTests
{
    [Fact]
    public void CreateOptions_ShouldUseEnglishFallbackAndExpectedProviderPrecedence()
    {
        RequestLocalizationOptions options = ApplicationLocalization.CreateOptions();

        Assert.Equal("en-US", options.DefaultRequestCulture.Culture.Name);
        Assert.Equal(["en-US", "ro-RO"], options.SupportedCultures.Select(c => c.Name));
        Assert.IsType<PreferredCultureRequestCultureProvider>(
            options.RequestCultureProviders[0]);
        Assert.IsType<CookieRequestCultureProvider>(
            options.RequestCultureProviders[1]);
        Assert.IsType<AcceptLanguageHeaderRequestCultureProvider>(
            options.RequestCultureProviders[2]);
    }

    [Fact]
    public async Task PreferredCultureProvider_ShouldOverrideTheBrowserCulture()
    {
        FakeUserRepository repository = new();
        repository.Users.Add(new User
        {
            Id = 7,
            Email = "anca@example.com",
            PreferredCulture = "ro-RO"
        });
        ServiceCollection services = new();
        services.AddSingleton<IUserRepository>(repository);
        DefaultHttpContext context = new()
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "7")],
                "Test"))
        };
        context.Request.Headers["Accept-Language"] = "en-US,en;q=0.9";

        ProviderCultureResult? result = await new PreferredCultureRequestCultureProvider()
            .DetermineProviderCultureResult(context);

        Assert.NotNull(result);
        Assert.Equal("ro-RO", Assert.Single(result.Cultures).ToString());
    }

    [Theory]
    [InlineData("en-US", true)]
    [InlineData("ro-RO", true)]
    [InlineData("de-DE", false)]
    [InlineData(null, false)]
    public void TryNormalizeSupportedCulture_ShouldAcceptOnlySupportedValues(
        string? culture,
        bool expectedResult)
    {
        bool result = ApplicationLocalization.TryNormalizeSupportedCulture(
            culture,
            out string normalizedCulture);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedResult ? culture : "en-US", normalizedCulture);
    }
}
