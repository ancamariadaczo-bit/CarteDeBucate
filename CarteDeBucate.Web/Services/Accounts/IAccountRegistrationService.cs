public interface IAccountRegistrationService
{
    Task<AccountRegistrationServiceResult> RegisterAsync(
        AccountRegistrationInput input,
        CancellationToken cancellationToken = default);

    Task<AccountRegistrationResendServiceResult> ResendVerificationAsync(
        AccountRegistrationResendInput input,
        CancellationToken cancellationToken = default);

    AccountEmailVerificationReviewResult ReviewEmailVerification(string? token);

    AccountEmailVerificationResult ConfirmEmailVerification(string? token);
}
