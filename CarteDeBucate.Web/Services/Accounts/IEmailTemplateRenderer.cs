public interface IEmailTemplateRenderer
{
    RenderedEmailTemplate Render(EmailTemplateRenderRequest request);
}
