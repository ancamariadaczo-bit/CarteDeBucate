public interface IAccountEmailService
{
    AccountPasswordVerificationResult VerifyCurrentPassword(int userId, string? password);

    Task<AccountEmailChangeRequestResult> RequestEmailChangeAsync(
        AccountEmailChangeRequestInput input,
        CancellationToken cancellationToken = default);

    Task<AccountEmailChangeResendResult> ResendEmailChangeAsync(
        int userId,
        string? requestCulture,
        CancellationToken cancellationToken = default);

    AccountEmailChangeReviewResult ReviewEmailChange(int userId, string? token);

    Task<AccountEmailChangeConfirmationResult> ConfirmEmailChangeAsync(
        int userId,
        string? token,
        CancellationToken cancellationToken = default);
}
