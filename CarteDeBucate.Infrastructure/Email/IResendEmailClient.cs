internal interface IResendEmailClient
{
    Task SendAsync(ResendEmailRequest request, CancellationToken cancellationToken);
}
