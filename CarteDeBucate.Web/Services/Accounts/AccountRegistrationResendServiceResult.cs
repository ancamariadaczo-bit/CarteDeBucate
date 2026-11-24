public enum AccountRegistrationResendServiceResultCode
{
    Sent,
    NotEligible,
    Cooldown,
    DailyLimit,
    DeliveryFailed,
    InvalidEmail
}

public sealed record AccountRegistrationResendServiceResult(
    AccountRegistrationResendServiceResultCode Code)
{
    public bool TokenWasPersisted =>
        Code is AccountRegistrationResendServiceResultCode.Sent
            or AccountRegistrationResendServiceResultCode.DeliveryFailed;
}
