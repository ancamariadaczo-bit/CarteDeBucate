using Resend;
using ProviderEmailMessage = Resend.EmailMessage;

internal sealed class ResendSdkEmailClient : IResendEmailClient
{
    private readonly IResend _resend;

    public ResendSdkEmailClient(IResend resend)
    {
        ArgumentNullException.ThrowIfNull(resend);
        _resend = resend;
    }

    public async Task SendAsync(
        ResendEmailRequest request,
        CancellationToken cancellationToken)
    {
        ProviderEmailMessage providerMessage = new()
        {
            From = request.FromAddress,
            Subject = request.Subject,
            HtmlBody = request.HtmlBody,
            TextBody = request.PlainTextBody
        };
        providerMessage.To.Add(request.Recipient);

        await _resend.EmailSendAsync(
            request.IdempotencyKey,
            providerMessage,
            cancellationToken);
    }
}
