public sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> SentMessages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SentMessages.Add(message);
        return Task.CompletedTask;
    }
}
