public interface IAccountRepository
{
    AccountRegistrationResult Register(AccountRegistrationRequest request);

    EmailTokenReplacementResult ReplaceEmailToken(int userId, EmailTokenIssuance token);

    EmailVerificationResult ConfirmRegistrationEmail(byte[] tokenHash, DateTime confirmedAtUtc);

    EmailChangeRequestResult RequestEmailChange(int userId, EmailTokenIssuance token);

    EmailChangeConfirmationResult ConfirmEmailChange(byte[] tokenHash, DateTime confirmedAtUtc);
}
