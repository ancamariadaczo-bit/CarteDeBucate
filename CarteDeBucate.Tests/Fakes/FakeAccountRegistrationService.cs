public sealed class FakeAccountRegistrationService : IAccountRegistrationService
{
    public bool RegisterWasCalled { get; private set; }

    public AccountRegistrationInput? Input { get; private set; }

    public CancellationToken CancellationToken { get; private set; }

    public AccountRegistrationServiceResult Result { get; set; } =
        new(AccountRegistrationServiceResultCode.Success);

    public Task<AccountRegistrationServiceResult> RegisterAsync(
        AccountRegistrationInput input,
        CancellationToken cancellationToken = default)
    {
        RegisterWasCalled = true;
        Input = input;
        CancellationToken = cancellationToken;

        return Task.FromResult(Result);
    }
}
