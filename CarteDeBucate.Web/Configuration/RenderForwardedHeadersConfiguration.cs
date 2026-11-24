using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

public static class RenderForwardedHeadersConfiguration
{
    public static ForwardedHeadersOptions? CreateOptions(string? renderEnvironmentValue)
    {
        if (!string.Equals(renderEnvironmentValue, "true", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        ForwardedHeadersOptions options = new()
        {
            ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto,
            // Render can add more than one trusted hop (Cloudflare and its own
            // load balancers). Process the complete chain to reach the original
            // client address at the left side of X-Forwarded-For.
            ForwardLimit = null
        };

        // Render's ingress addresses are not published as a stable allowlist.
        // Clearing these collections is safe only after Render's own RENDER=true
        // runtime marker has established that the public port is behind its
        // managed ingress.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        return options;
    }
}
