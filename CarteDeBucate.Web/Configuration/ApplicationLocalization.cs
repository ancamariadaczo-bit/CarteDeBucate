using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationLocalization
{
    public const string EnglishCultureName = "en-US";
    public const string RomanianCultureName = "ro-RO";

    private static readonly CultureInfo EnglishCulture =
        CultureInfo.GetCultureInfo(EnglishCultureName);
    private static readonly CultureInfo RomanianCulture =
        CultureInfo.GetCultureInfo(RomanianCultureName);

    public static RequestLocalizationOptions CreateOptions()
    {
        return new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(EnglishCulture),
            SupportedCultures = [EnglishCulture, RomanianCulture],
            SupportedUICultures = [EnglishCulture, RomanianCulture],
            RequestCultureProviders =
            [
                new PreferredCultureRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ]
        };
    }

    public static bool TryNormalizeSupportedCulture(
        string? culture,
        out string normalizedCulture)
    {
        if (string.Equals(culture, EnglishCultureName, StringComparison.OrdinalIgnoreCase))
        {
            normalizedCulture = EnglishCultureName;
            return true;
        }

        if (string.Equals(culture, RomanianCultureName, StringComparison.OrdinalIgnoreCase))
        {
            normalizedCulture = RomanianCultureName;
            return true;
        }

        normalizedCulture = EnglishCultureName;
        return false;
    }

    public static CultureInfo GetSupportedCultureOrFallback(string? culture)
    {
        return TryNormalizeSupportedCulture(culture, out string normalizedCulture)
            ? CultureInfo.GetCultureInfo(normalizedCulture)
            : EnglishCulture;
    }
}

public sealed class PreferredCultureRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(
        HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return NullProviderResult();
        }

        string? userIdClaim = httpContext.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return NullProviderResult();
        }

        IUserRepository userRepository =
            httpContext.RequestServices.GetRequiredService<IUserRepository>();
        User? user = userRepository.GetById(userId);

        if (!ApplicationLocalization.TryNormalizeSupportedCulture(
                user?.PreferredCulture,
                out string culture))
        {
            return NullProviderResult();
        }

        return Task.FromResult<ProviderCultureResult?>(
            new ProviderCultureResult(culture, culture));
    }

    private static Task<ProviderCultureResult?> NullProviderResult()
    {
        return Task.FromResult<ProviderCultureResult?>(null);
    }
}
