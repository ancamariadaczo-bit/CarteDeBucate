public interface IAccountRepository
{
    AccountRegistrationResult Register(AccountRegistrationRequest request);

    EmailTokenReplacementResult ReplaceEmailToken(int userId, EmailTokenIssuance token);

    AccountRegistrationResendResult ResendRegistrationEmail(
        AccountRegistrationResendRequest request);

    EmailVerificationReviewResult ReviewRegistrationEmail(
        byte[] tokenHash,
        DateTime reviewedAtUtc);

    EmailVerificationResult ConfirmRegistrationEmail(byte[] tokenHash, DateTime confirmedAtUtc);

    EmailChangeRequestResult RequestEmailChange(int userId, EmailTokenIssuance token);

    EmailChangeConfirmationResult ConfirmEmailChange(byte[] tokenHash, DateTime confirmedAtUtc);
}
