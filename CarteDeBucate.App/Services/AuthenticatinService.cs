using System.Security.Cryptography;

public class AuthenticationService : IAuthenticationService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly IUserRepository _userRepository;

    public AuthenticationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public User? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser is not null;

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

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        User user = new User
        {
            Username = username,
            PasswordHash = Convert.ToBase64String(hash),
            PasswordSalt = Convert.ToBase64String(salt),
            CreatedAt = DateTime.Now
        };

        _userRepository.Add(user);

        CurrentUser = user;

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

        bool isPasswordValid = VerifyPassword(password, user.PasswordHash, user.PasswordSalt);

        if (!isPasswordValid)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                Message = "Invalid username or password."
            };
        }

        CurrentUser = user;

        return new AuthenticationResult
        {
            IsSuccess = true,
            Message = "Login successful.",
            User = user
        };
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    private static bool VerifyPassword(string password, string storedPasswordHash, string storedPasswordSalt)
    {
        byte[] salt = Convert.FromBase64String(storedPasswordSalt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        string passwordHash = Convert.ToBase64String(hash);

        return passwordHash == storedPasswordHash;
    }
}