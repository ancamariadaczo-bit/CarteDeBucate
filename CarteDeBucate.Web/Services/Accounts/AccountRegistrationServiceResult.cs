public enum AccountRegistrationServiceResultCode
{
    Success,
    InvalidEmail,
    PasswordRequired,
    PasswordTooShort,
    PasswordConfirmationRequired,
    PasswordConfirmationMismatch,
    DuplicateEmail,
    DeliveryFailed
}

public sealed record AccountRegistrationServiceResult(
    AccountRegistrationServiceResultCode Code)
{
    public bool IsSuccess => Code == AccountRegistrationServiceResultCode.Success;

    public bool RegistrationWasPersisted =>
        Code is AccountRegistrationServiceResultCode.Success
            or AccountRegistrationServiceResultCode.DeliveryFailed;
}
