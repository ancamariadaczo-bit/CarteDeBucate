public sealed record EmailMessage(
    string Recipient,
    string Subject,
    string HtmlBody,
    string PlainTextBody,
    string IdempotencyKey);
