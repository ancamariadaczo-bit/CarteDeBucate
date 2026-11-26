public sealed class GlobalSessionInvalidationService : IGlobalSessionInvalidationService
{
    private readonly IUserRepository _userRepository;

    public GlobalSessionInvalidationService(IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);

        _userRepository = userRepository;
    }

    public bool InvalidateAllSessions(int userId)
    {
        return _userRepository.IncrementSecurityVersion(userId);
    }
}
