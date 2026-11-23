public class ResendEmailSenderTests
{
    [Fact]
    public async Task SendAsync_ShouldMapTheApplicationMessageAndIdempotencyKey()
    {
        RecordingResendEmailClient client = new();
        ResendEmailSender sender = CreateSender(client);
        EmailMessage message = new(
            "recipient@example.com",
            "Subject",
            "<p>HTML body</p>",
            "Plain-text body",
            "registration-verification/42");

        await sender.SendAsync(message);

        ResendEmailRequest request = Assert.IsType<ResendEmailRequest>(client.Request);
        Assert.Equal("sender@example.com", request.FromAddress);
        Assert.Equal(message.Recipient, request.Recipient);
        Assert.Equal(message.Subject, request.Subject);
        Assert.Equal(message.HtmlBody, request.HtmlBody);
        Assert.Equal(message.PlainTextBody, request.PlainTextBody);
        Assert.Equal(message.IdempotencyKey, request.IdempotencyKey);
    }

    [Fact]
    public async Task SendAsync_WhenCallerCancels_ShouldPropagateCancellation()
    {
        RecordingResendEmailClient client = new()
        {
            Send = (_, cancellationToken) =>
                Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
        };
        ResendEmailSender sender = CreateSender(client);
        using CancellationTokenSource cancellationSource = new();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sender.SendAsync(CreateMessage(), cancellationSource.Token));
    }

    [Fact]
    public async Task SendAsync_WhenDeliveryTimesOut_ShouldReturnSafeTimeoutFailure()
    {
        RecordingResendEmailClient client = new()
        {
            Send = (_, cancellationToken) =>
                Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
        };
        ResendEmailSender sender = CreateSender(client, TimeSpan.FromMilliseconds(1));

        EmailDeliveryException exception = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => sender.SendAsync(CreateMessage()));

        Assert.Equal(EmailDeliveryFailureCode.Timeout, exception.FailureCode);
        Assert.Equal("Email delivery timed out.", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_ShouldNotExposeProviderDetails()
    {
        const string providerDetails =
            "API token re_secret rejected recipient@example.com and included the email body.";
        RecordingResendEmailClient client = new()
        {
            Send = (_, _) => Task.FromException(new InvalidOperationException(providerDetails))
        };
        ResendEmailSender sender = CreateSender(client);

        EmailDeliveryException exception = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => sender.SendAsync(CreateMessage()));

        Assert.Equal(EmailDeliveryFailureCode.ProviderFailure, exception.FailureCode);
        Assert.Equal("Email delivery failed.", exception.Message);
        Assert.DoesNotContain(providerDetails, exception.ToString());
    }

    private static ResendEmailSender CreateSender(
        IResendEmailClient client,
        TimeSpan? timeout = null)
    {
        return new ResendEmailSender(
            client,
            new ResendOptions
            {
                FromAddress = "sender@example.com",
                DeliveryTimeout = timeout ?? TimeSpan.FromSeconds(10)
            });
    }

    private static EmailMessage CreateMessage()
    {
        return new EmailMessage(
            "recipient@example.com",
            "Subject",
            "<p>HTML body</p>",
            "Plain-text body",
            "registration-verification/42");
    }

    private sealed class RecordingResendEmailClient : IResendEmailClient
    {
        public ResendEmailRequest? Request { get; private set; }

        public Func<ResendEmailRequest, CancellationToken, Task>? Send { get; init; }

        public Task SendAsync(
            ResendEmailRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Send?.Invoke(request, cancellationToken) ?? Task.CompletedTask;
        }
    }
}
