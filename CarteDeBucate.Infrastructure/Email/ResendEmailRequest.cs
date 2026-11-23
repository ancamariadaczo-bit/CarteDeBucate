internal sealed record ResendEmailRequest(
    string FromAddress,
    string Recipient,
    string Subject,
    string HtmlBody,
    string PlainTextBody,
    string IdempotencyKey);
