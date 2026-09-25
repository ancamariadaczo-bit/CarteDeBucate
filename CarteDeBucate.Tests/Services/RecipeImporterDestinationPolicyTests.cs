using System.Net;

public class RecipeImporterDestinationPolicyTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("2001:4860:4860::8888")]
    public async Task ValidateAsync_WithPublicAddress_ShouldAllowDestination(
        string address)
    {
        FakeHostAddressResolver resolver = new FakeHostAddressResolver(address);
        RecipeImportDestinationPolicy policy =
            new RecipeImportDestinationPolicy(resolver);

        RecipeImportDestinationValidationResult result =
            await policy.ValidateAsync(new Uri("https://recipe.example"));

        Assert.True(result.IsAllowed);
        IPAddress approvedAddress = Assert.Single(result.Addresses);
        Assert.Equal(IPAddress.Parse(address), approvedAddress);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("127.0.0.1")]
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.0.1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456:789a::1")]
    [InlineData("169.254.169.254")]
    [InlineData("fe80::1")]
    [InlineData("224.0.0.1")]
    [InlineData("ff02::1")]
    [InlineData("100.64.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("240.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("2001:db8::1")]
    [InlineData("::ffff:192.168.0.1")]
    public async Task ValidateAsync_WithNonPublicAddress_ShouldRejectDestination(
        string address)
    {
        FakeHostAddressResolver resolver = new FakeHostAddressResolver(address);
        RecipeImportDestinationPolicy policy =
            new RecipeImportDestinationPolicy(resolver);

        RecipeImportDestinationValidationResult result =
            await policy.ValidateAsync(new Uri("https://recipe.example"));

        Assert.False(result.IsAllowed);
        Assert.Empty(result.Addresses);
    }

    [Fact]
    public async Task ValidateAsync_WhenOneResolvedAddressIsNonPublic_ShouldRejectEntireHost()
    {
        FakeHostAddressResolver resolver = new FakeHostAddressResolver(
            "8.8.8.8",
            "10.0.0.1");
        RecipeImportDestinationPolicy policy =
            new RecipeImportDestinationPolicy(resolver);

        RecipeImportDestinationValidationResult result =
            await policy.ValidateAsync(new Uri("https://mixed.example"));

        Assert.Equal("mixed.example", resolver.ResolvedHost);
        Assert.False(result.IsAllowed);
        Assert.Empty(result.Addresses);
    }

    [Theory]
    [InlineData("ftp://recipe.example")]
    [InlineData("http://recipe.example:8080")]
    [InlineData("https://recipe.example:8443")]
    public async Task ValidateAsync_WithDisallowedSchemeOrPort_ShouldRejectBeforeDns(
        string destination)
    {
        FakeHostAddressResolver resolver = new FakeHostAddressResolver("8.8.8.8");
        RecipeImportDestinationPolicy policy =
            new RecipeImportDestinationPolicy(resolver);

        RecipeImportDestinationValidationResult result =
            await policy.ValidateAsync(new Uri(destination));

        Assert.False(result.IsAllowed);
        Assert.Empty(result.Addresses);
        Assert.Null(resolver.ResolvedHost);
    }

    [Fact]
    public void HttpMessageHandler_ShouldDisableAutomaticRedirectsAndProxyResolution()
    {
        FakeHostAddressResolver resolver = new FakeHostAddressResolver("8.8.8.8");
        RecipeImportDestinationPolicy policy =
            new RecipeImportDestinationPolicy(resolver);
        using RecipeImportHttpMessageHandler handler =
            new RecipeImportHttpMessageHandler(policy);

        SocketsHttpHandler socketsHandler =
            Assert.IsType<SocketsHttpHandler>(handler.InnerHandler);

        Assert.False(socketsHandler.AllowAutoRedirect);
        Assert.False(socketsHandler.UseProxy);
        Assert.NotNull(socketsHandler.ConnectCallback);
    }

    private sealed class FakeHostAddressResolver : IHostAddressResolver
    {
        private readonly IPAddress[] _addresses;

        public FakeHostAddressResolver(params string[] addresses)
        {
            _addresses = addresses
                .Select(IPAddress.Parse)
                .ToArray();
        }

        public string? ResolvedHost { get; private set; }

        public Task<IPAddress[]> GetHostAddressesAsync(
            string host,
            CancellationToken cancellationToken)
        {
            ResolvedHost = host;

            return Task.FromResult(_addresses);
        }
    }
}
