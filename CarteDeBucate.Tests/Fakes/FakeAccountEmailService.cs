public sealed class FakeAccountEmailService : IAccountEmailService
{
    public AccountEmailChangeRequestResult RequestResult { get; set; } =
        new(AccountEmailChangeRequestResultCode.Sent);

    public AccountEmailChangeResendResult ResendResult { get; set; } =
        new(AccountEmailChangeResendResultCode.Sent);

    public AccountEmailChangeReviewResult ReviewResult { get; set; } =
        new(AccountEmailChangeReviewResultCode.Valid);

    public AccountEmailChangeConfirmationResult ConfirmationResult { get; set; } =
        new(AccountEmailChangeConfirmationResultCode.Success);

    public bool RequestWasCalled { get; private set; }

    public AccountEmailChangeRequestInput? RequestInput { get; private set; }

    public bool ResendWasCalled { get; private set; }

    public int? ResendUserId { get; private set; }

    public bool ReviewWasCalled { get; private set; }

    public int? ReviewedUserId { get; private set; }

    public string? ReviewedToken { get; private set; }

    public bool ConfirmWasCalled { get; private set; }

    public int? ConfirmedUserId { get; private set; }

    public string? ConfirmedToken { get; private set; }

    public AccountPasswordVerificationResult VerifyCurrentPassword(
        int userId,
        string? password) => new(AccountPasswordVerificationResultCode.Valid);

    public Task<AccountEmailChangeRequestResult> RequestEmailChangeAsync(
        AccountEmailChangeRequestInput input,
        CancellationToken cancellationToken = default)
    {
        RequestWasCalled = true;
        RequestInput = input;
        return Task.FromResult(RequestResult);
    }

    public Task<AccountEmailChangeResendResult> ResendEmailChangeAsync(
        int userId,
        string? requestCulture,
        CancellationToken cancellationToken = default)
    {
        ResendWasCalled = true;
        ResendUserId = userId;
        return Task.FromResult(ResendResult);
    }

    public AccountEmailChangeReviewResult ReviewEmailChange(int userId, string? token)
    {
        ReviewWasCalled = true;
        ReviewedUserId = userId;
        ReviewedToken = token;
        return ReviewResult;
    }

    public Task<AccountEmailChangeConfirmationResult> ConfirmEmailChangeAsync(
        int userId,
        string? token,
        CancellationToken cancellationToken = default)
    {
        ConfirmWasCalled = true;
        ConfirmedUserId = userId;
        ConfirmedToken = token;
        return Task.FromResult(ConfirmationResult);
    }
}
