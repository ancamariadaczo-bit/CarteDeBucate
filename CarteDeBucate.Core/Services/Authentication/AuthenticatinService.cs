public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly PasswordHasher _passwordHasher;

    public AuthenticationService(IUserRepository userRepository)
        : this(userRepository, null, new PasswordHasher())
    {
    }

    public AuthenticationService(IUserRepository userRepository, ICurrentUserContext? currentUserContext)
        : this(userRepository, currentUserContext, new PasswordHasher())
    {
    }

    public AuthenticationService(
        IUserRepository userRepository,
        ICurrentUserContext? currentUserContext,
        PasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        _userRepository = userRepository;
        _currentUserContext = currentUserContext;
        _passwordHasher = passwordHasher;
    }

    public bool IsLoggedIn => _currentUserContext?.IsAuthenticated == true;

    public AuthenticationResult Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return InvalidCredentials();
        }

        if (!EmailNormalizer.TryNormalize(email, out string normalizedEmail))
        {
            return InvalidCredentials();
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return InvalidCredentials();
        }

        User? user = _userRepository.GetByEmail(normalizedEmail);

        if (user is null)
        {
            return InvalidCredentials();
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(
            password,
            user.PasswordHash,
            user.PasswordSalt);

        if (!isPasswordValid || user.EmailVerifiedAtUtc is null)
        {
            return InvalidCredentials();
        }

        _currentUserContext?.SetCurrentUser(user);

        return new AuthenticationResult
        {
            IsSuccess = true,
            Message = "Login successful.",
            User = user
        };
    }

    public void Logout()
    {
        _currentUserContext?.Clear();
    }

    private static AuthenticationResult InvalidCredentials()
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            Message = "Invalid email or password."
        };
    }
}
