using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class RenderForwardedHeadersConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("1")]
    public void CreateOptions_OutsideRender_ShouldNotEnableForwarding(
        string? renderEnvironmentValue)
    {
        ForwardedHeadersOptions? options =
            RenderForwardedHeadersConfiguration.CreateOptions(
                renderEnvironmentValue);

        Assert.Null(options);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void CreateOptions_OnRender_ShouldTrustTheCompleteIngressChain(
        string renderEnvironmentValue)
    {
        ForwardedHeadersOptions options = Assert.IsType<ForwardedHeadersOptions>(
            RenderForwardedHeadersConfiguration.CreateOptions(
                renderEnvironmentValue));

        Assert.Equal(
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto,
            options.ForwardedHeaders);
        Assert.Null(options.ForwardLimit);
        Assert.Empty(options.KnownIPNetworks);
        Assert.Empty(options.KnownProxies);
    }

    [Fact]
    public async Task Middleware_OnRender_ShouldResolveTheOriginalClient()
    {
        ForwardedHeadersOptions options = Assert.IsType<ForwardedHeadersOptions>(
            RenderForwardedHeadersConfiguration.CreateOptions("true"));
        IPAddress? observedAddress = null;
        string? observedScheme = null;
        ForwardedHeadersMiddleware middleware = new(
            context =>
            {
                observedAddress = context.Connection.RemoteIpAddress;
                observedScheme = context.Request.Scheme;
                return Task.CompletedTask;
            },
            NullLoggerFactory.Instance,
            Options.Create(options));
        DefaultHttpContext context = new();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.8");
        context.Request.Headers["X-Forwarded-For"] =
            "203.0.113.10, 198.51.100.20";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(context);

        Assert.Equal(IPAddress.Parse("203.0.113.10"), observedAddress);
        Assert.Equal("https", observedScheme);
    }

    [Fact]
    public async Task Middleware_WithoutRenderTrust_ShouldIgnoreAnUntrustedHeader()
    {
        ForwardedHeadersOptions options = new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor
        };
        IPAddress? observedAddress = null;
        ForwardedHeadersMiddleware middleware = new(
            context =>
            {
                observedAddress = context.Connection.RemoteIpAddress;
                return Task.CompletedTask;
            },
            NullLoggerFactory.Instance,
            Options.Create(options));
        DefaultHttpContext context = new();
        IPAddress proxyAddress = IPAddress.Parse("198.51.100.20");
        context.Connection.RemoteIpAddress = proxyAddress;
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10";

        await middleware.Invoke(context);

        Assert.Equal(proxyAddress, observedAddress);
    }
}
