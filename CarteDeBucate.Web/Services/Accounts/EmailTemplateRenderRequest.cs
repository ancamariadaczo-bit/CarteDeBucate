public sealed record EmailTemplateRenderRequest(
    EmailTemplateId TemplateId,
    string? Culture,
    string? ConfirmationUrl = null,
    string? NewEmail = null);
