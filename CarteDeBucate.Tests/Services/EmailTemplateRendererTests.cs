public class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new();

    [Fact]
    public void RenderRegistrationVerification_ShouldEncodeTheConfirmationUrlInHtml()
    {
        RenderedEmailTemplate template = _renderer.Render(
            new EmailTemplateRenderRequest(
                EmailTemplateId.RegistrationVerification,
                "en-US",
                "https://example.test/account/confirm?token=abc&next=home"));

        Assert.Equal("Verify your CarteDeBucate email", template.Subject);
        Assert.Contains(
            "href=\"https://example.test/account/confirm?token=abc&amp;next=home\"",
            template.HtmlBody);
        Assert.Contains(
            "https://example.test/account/confirm?token=abc&next=home",
            template.PlainTextBody);
        Assert.Contains("within 24 hours", template.PlainTextBody);
    }

    [Fact]
    public void RenderEmailChangeVerification_ShouldFallBackToEnglishForRomanian()
    {
        const string confirmationUrl = "https://example.test/settings/confirm-email?token=abc";

        RenderedEmailTemplate englishTemplate = _renderer.Render(
            new EmailTemplateRenderRequest(
                EmailTemplateId.EmailChangeVerification,
                "en-US",
                confirmationUrl));
        RenderedEmailTemplate romanianTemplate = _renderer.Render(
            new EmailTemplateRenderRequest(
                EmailTemplateId.EmailChangeVerification,
                "ro-RO",
                confirmationUrl));

        Assert.Equal(englishTemplate, romanianTemplate);
    }

    [Fact]
    public void RenderVerificationTemplate_WithNonHttpConfirmationUrl_ShouldRejectIt()
    {
        EmailTemplateRenderRequest request = new(
            EmailTemplateId.RegistrationVerification,
            "en-US",
            "javascript:alert('unsafe')");

        Assert.Throws<ArgumentException>(() => _renderer.Render(request));
    }

    [Fact]
    public void RenderPreviousAddressChangeNotification_ShouldEncodeTheNewEmailInHtml()
    {
        RenderedEmailTemplate template = _renderer.Render(
            new EmailTemplateRenderRequest(
                EmailTemplateId.PreviousAddressChangeNotification,
                "en-US",
                NewEmail: "new@example.test<script>"));

        Assert.Contains("new@example.test&lt;script&gt;", template.HtmlBody);
        Assert.DoesNotContain("new@example.test<script>", template.HtmlBody);
        Assert.Contains("new@example.test<script>", template.PlainTextBody);
    }
}
