public enum EmailDeliveryFailureCode
{
    ProviderFailure,
    Timeout
}

public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(EmailDeliveryFailureCode failureCode)
        : base(GetSafeMessage(failureCode))
    {
        FailureCode = failureCode;
    }

    public EmailDeliveryFailureCode FailureCode { get; }

    private static string GetSafeMessage(EmailDeliveryFailureCode failureCode)
    {
        return failureCode switch
        {
            EmailDeliveryFailureCode.ProviderFailure => "Email delivery failed.",
            EmailDeliveryFailureCode.Timeout => "Email delivery timed out.",
            _ => "Email delivery failed."
        };
    }
}
