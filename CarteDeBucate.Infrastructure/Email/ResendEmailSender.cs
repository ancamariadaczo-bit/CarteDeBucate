using Microsoft.Extensions.Options;
using Resend;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly IResendEmailClient _client;
    private readonly ResendOptions _options;

    public ResendEmailSender(IResend resend, IOptions<ResendOptions> options)
        : this(
            new ResendSdkEmailClient(resend),
            options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    internal ResendEmailSender(IResendEmailClient client, ResendOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new ArgumentException("A sender address is required.", nameof(options));
        }

        if (options.DeliveryTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The delivery timeout must be greater than zero.");
        }

        _client = client;
        _options = options;
    }

    public async Task SendAsync(
        global::EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        ResendEmailRequest request = new(
            _options.FromAddress,
            message.Recipient,
            message.Subject,
            message.HtmlBody,
            message.PlainTextBody,
            message.IdempotencyKey);

        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_options.DeliveryTimeout);

        try
        {
            await _client.SendAsync(request, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmailDeliveryException(EmailDeliveryFailureCode.Timeout);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            throw new EmailDeliveryException(EmailDeliveryFailureCode.ProviderFailure);
        }
    }
}
