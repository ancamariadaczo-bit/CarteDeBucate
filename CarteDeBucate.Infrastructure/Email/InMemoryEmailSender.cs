public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly object _sync = new();
    private readonly List<EmailMessage> _sentMessages = [];

    public IReadOnlyList<EmailMessage> SentMessages
    {
        get
        {
            lock (_sync)
            {
                return _sentMessages.ToArray();
            }
        }
    }

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _sentMessages.Add(message);
        }

        return Task.CompletedTask;
    }
}
