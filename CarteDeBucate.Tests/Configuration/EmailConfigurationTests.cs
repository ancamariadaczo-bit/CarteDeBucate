public class EmailConfigurationTests
{
    [Fact]
    public void Validate_ForProductionWithoutRealDelivery_ShouldRejectConfiguration()
    {
        ApplicationOptions applicationOptions = new()
        {
            PublicBaseUrl = "https://recipes.example.com"
        };
        ResendOptions resendOptions = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EmailConfigurationValidator.Validate(
                applicationOptions,
                resendOptions,
                isProduction: true));

        Assert.Equal("Resend:Enabled must be true in production.", exception.Message);
    }

    [Fact]
    public void Validate_ForProductionWithHttpPublicUrl_ShouldRejectConfiguration()
    {
        ApplicationOptions applicationOptions = new()
        {
            PublicBaseUrl = "http://recipes.example.com"
        };
        ResendOptions resendOptions = CreateEnabledResendOptions();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EmailConfigurationValidator.Validate(
                applicationOptions,
                resendOptions,
                isProduction: true));

        Assert.Equal(
            "Application:PublicBaseUrl must use HTTPS in production.",
            exception.Message);
    }

    [Fact]
    public void Validate_WithRealDeliveryAndMissingToken_ShouldRejectConfiguration()
    {
        ApplicationOptions applicationOptions = new()
        {
            PublicBaseUrl = "https://recipes.example.com"
        };
        ResendOptions resendOptions = CreateEnabledResendOptions();
        resendOptions.ApiToken = string.Empty;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EmailConfigurationValidator.Validate(
                applicationOptions,
                resendOptions,
                isProduction: false));

        Assert.Equal("Resend:ApiToken is not configured.", exception.Message);
    }

    [Fact]
    public void BuildConfirmationUrl_ShouldUseConfiguredOriginAndEncodeTheToken()
    {
        AccountLinkBuilder linkBuilder = new(
            new ApplicationOptions
            {
                PublicBaseUrl = "https://configured.example.com"
            });

        string url = linkBuilder.BuildConfirmationUrl(
            "/Account/VerifyEmail",
            "abc+/=");

        Assert.Equal(
            "https://configured.example.com/Account/VerifyEmail?token=abc%2B%2F%3D",
            url);
    }

    [Fact]
    public void BuildConfirmationUrl_WithNetworkPath_ShouldRejectIt()
    {
        AccountLinkBuilder linkBuilder = new(
            new ApplicationOptions
            {
                PublicBaseUrl = "https://configured.example.com"
            });

        Assert.Throws<ArgumentException>(() =>
            linkBuilder.BuildConfirmationUrl("//attacker.example/path", "token"));
    }

    private static ResendOptions CreateEnabledResendOptions()
    {
        return new ResendOptions
        {
            Enabled = true,
            ApiToken = "configured-secret",
            FromAddress = "sender@example.com"
        };
    }
}
