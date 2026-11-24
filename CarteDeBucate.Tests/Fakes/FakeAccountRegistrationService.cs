public sealed class FakeAccountRegistrationService : IAccountRegistrationService
{
    public bool RegisterWasCalled { get; private set; }

    public AccountRegistrationInput? Input { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public AccountRegistrationServiceResult Result { get; set; } =
        new(AccountRegistrationServiceResultCode.Success);

    public bool ResendWasCalled { get; private set; }

    public AccountRegistrationResendInput? ResendInput { get; private set; }

    public CancellationToken ResendCancellationToken { get; private set; }

    public AccountRegistrationResendServiceResult ResendResult { get; set; } =
        new(AccountRegistrationResendServiceResultCode.Sent);

    public bool ReviewWasCalled { get; private set; }

    public string? ReviewedToken { get; private set; }

    public AccountEmailVerificationReviewResult ReviewResult { get; set; } =
        new(AccountEmailVerificationReviewResultCode.Valid);

    public bool ConfirmWasCalled { get; private set; }

    public string? ConfirmedToken { get; private set; }

    public AccountEmailVerificationResult ConfirmationResult { get; set; } =
        new(AccountEmailVerificationResultCode.Success);

    public Task<AccountRegistrationServiceResult> RegisterAsync(
        AccountRegistrationInput input,
        CancellationToken cancellationToken = default)
    {
        RegisterWasCalled = true;
        Input = input;
        CancellationToken = cancellationToken;

        return Task.FromResult(Result);
    }

    public Task<AccountRegistrationResendServiceResult> ResendVerificationAsync(
        AccountRegistrationResendInput input,
        CancellationToken cancellationToken = default)
    {
        ResendWasCalled = true;
        ResendInput = input;
        ResendCancellationToken = cancellationToken;

        return Task.FromResult(ResendResult);
    }

    public AccountEmailVerificationReviewResult ReviewEmailVerification(
        string? token)
    {
        ReviewWasCalled = true;
        ReviewedToken = token;
        return ReviewResult;
    }

    public AccountEmailVerificationResult ConfirmEmailVerification(string? token)
    {
        ConfirmWasCalled = true;
        ConfirmedToken = token;
        return ConfirmationResult;
    }
}
