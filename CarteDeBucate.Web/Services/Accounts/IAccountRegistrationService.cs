public interface IAccountRegistrationService
{
    Task<AccountRegistrationServiceResult> RegisterAsync(
        AccountRegistrationInput input,
        CancellationToken cancellationToken = default);

    AccountEmailVerificationReviewResult ReviewEmailVerification(string? token);

    AccountEmailVerificationResult ConfirmEmailVerification(string? token);
}
