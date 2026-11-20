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

    public AuthenticationResult Register(string username, string password)
    {
        username = username.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Username is required."
            };
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Password is required."
            };
        }

        User? existingUser = _userRepository.GetByUsername(username);

        if (existingUser is not null)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "This username already exists."
            };
        }

        (string hash, string salt) = _passwordHasher.HashPassword(password);

        User user = new User
        {
            Email = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.Now
        };

        _userRepository.Add(user);

        _currentUserContext?.SetCurrentUser(user);

        return new AuthenticationResult
        {
            IsSuccess = true,
            Message = "User registered successfully.",
            User = user
        };
    }

    public AuthenticationResult Login(string username, string password)
    {
        username = username.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Username is required."
            };
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Password is required."
            };
        }

        User? user = _userRepository.GetByUsername(username);

        if (user is null)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Invalid username or password."
            };
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(
            password,
            user.PasswordHash,
            user.PasswordSalt);

        if (!isPasswordValid)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Invalid username or password."
            };
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
}
