public interface IAccountRegistrationService
{
    Task<AccountRegistrationServiceResult> RegisterAsync(
        AccountRegistrationInput input,
        CancellationToken cancellationToken = default);
}
