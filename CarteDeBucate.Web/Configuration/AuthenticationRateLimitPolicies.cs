using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

public static class AuthenticationRateLimitPolicies
{
    public const string Registration = "authentication-registration";
    public const string ResendVerification = "authentication-resend-verification";
    public const string EmailVerification = "authentication-email-verification";
    public const string EmailChange = "authentication-email-change";
    public const string Login = "authentication-login";
}

public static class AuthenticationRateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationEndpointRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, cancellationToken) =>
            {
                HttpResponse response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "text/plain; charset=utf-8";

                await response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken);
            };

            options.AddPolicy(
                AuthenticationRateLimitPolicies.Registration,
                context => CreateIpPolicy(context, 5, TimeSpan.FromHours(1)));
            options.AddPolicy(
                AuthenticationRateLimitPolicies.ResendVerification,
                context => CreateIpPolicy(context, 10, TimeSpan.FromHours(1)));
            options.AddPolicy(
                AuthenticationRateLimitPolicies.EmailVerification,
                context => CreateIpPolicy(context, 10, TimeSpan.FromMinutes(10)));
            options.AddPolicy(
                AuthenticationRateLimitPolicies.EmailChange,
                CreateEmailChangePolicy);
            options.AddPolicy(
                AuthenticationRateLimitPolicies.Login,
                context => CreateIpPolicy(context, 10, TimeSpan.FromMinutes(10)));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateIpPolicy(
        HttpContext context,
        int permitLimit,
        TimeSpan window)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            GetClientIpPartitionKey(context),
            _ => CreateFixedWindowOptions(permitLimit, window));
    }

    private static RateLimitPartition<string> CreateEmailChangePolicy(
        HttpContext context)
    {
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string partitionKey = string.IsNullOrWhiteSpace(userId)
            ? GetClientIpPartitionKey(context)
            : $"user:{userId}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => CreateFixedWindowOptions(3, TimeSpan.FromHours(1)));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(
        int permitLimit,
        TimeSpan window)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }

    private static string GetClientIpPartitionKey(HttpContext context)
    {
        // Do not read X-Forwarded-For directly. A trusted proxy configuration may
        // set RemoteIpAddress after validating the forwarding proxy instead.
        var remoteIpAddress = context.Connection.RemoteIpAddress;

        if (remoteIpAddress is null)
        {
            return "unknown-client";
        }

        return remoteIpAddress.IsIPv4MappedToIPv6
            ? remoteIpAddress.MapToIPv4().ToString()
            : remoteIpAddress.ToString();
    }
}
