using CarteDeBucate.Web.Resources;
using System.Globalization;
using System.Text.Encodings.Web;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    public RenderedEmailTemplate Render(EmailTemplateRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CultureInfo culture = ResolveCulture(request.Culture);

        return request.TemplateId switch
        {
            EmailTemplateId.RegistrationVerification =>
                RenderConfirmationTemplate("RegistrationVerification", request, culture),
            EmailTemplateId.EmailChangeVerification =>
                RenderConfirmationTemplate("EmailChangeVerification", request, culture),
            EmailTemplateId.PreviousAddressChangeNotification =>
                RenderPreviousAddressChangeNotification(request, culture),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
    }

    private static RenderedEmailTemplate RenderConfirmationTemplate(
        string resourcePrefix,
        EmailTemplateRenderRequest request,
        CultureInfo culture)
    {
        string confirmationUrl = GetConfirmationUrl(request);

        return RenderTemplate(
            resourcePrefix,
            culture,
            HtmlEncoder.Default.Encode(confirmationUrl),
            confirmationUrl);
    }

    private static RenderedEmailTemplate RenderPreviousAddressChangeNotification(
        EmailTemplateRenderRequest request,
        CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(request.NewEmail))
        {
            throw new ArgumentException(
                "The new email address is required for a previous-address notification.",
                nameof(request));
        }

        return RenderTemplate(
            "PreviousAddressChangeNotification",
            culture,
            HtmlEncoder.Default.Encode(request.NewEmail),
            request.NewEmail);
    }

    private static RenderedEmailTemplate RenderTemplate(
        string resourcePrefix,
        CultureInfo culture,
        string htmlValue,
        string plainTextValue)
    {
        string subject = EmailTemplateResources.Get($"{resourcePrefix}_Subject", culture);
        string htmlTemplate = EmailTemplateResources.Get($"{resourcePrefix}_HtmlBody", culture);
        string plainTextTemplate =
            EmailTemplateResources.Get($"{resourcePrefix}_PlainTextBody", culture);

        return new RenderedEmailTemplate(
            subject,
            string.Format(CultureInfo.InvariantCulture, htmlTemplate, htmlValue),
            string.Format(CultureInfo.InvariantCulture, plainTextTemplate, plainTextValue));
    }

    private static CultureInfo ResolveCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return CultureInfo.InvariantCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(culture);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static string GetConfirmationUrl(EmailTemplateRenderRequest request)
    {
        if (!Uri.TryCreate(request.ConfirmationUrl, UriKind.Absolute, out Uri? confirmationUri) ||
            (confirmationUri.Scheme != Uri.UriSchemeHttp &&
             confirmationUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "An absolute HTTP or HTTPS confirmation URL is required.",
                nameof(request));
        }

        return confirmationUri.AbsoluteUri;
    }
}
