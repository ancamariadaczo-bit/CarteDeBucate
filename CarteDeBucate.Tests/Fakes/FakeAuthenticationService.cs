public class FakeAuthenticationService : IAuthenticationService
{
    public bool LoginWasCalled { get; private set; }

    public bool RegisterWasCalled { get; private set; }

    public bool LogoutWasCalled { get; private set; }

    public string? UsernamePassedToLogin { get; private set; }

    public string? PasswordPassedToLogin { get; private set; }

    public string? UsernamePassedToRegister { get; private set; }

    public string? PasswordPassedToRegister { get; private set; }

    public AuthenticationResult LoginResult { get; set; } = new AuthenticationResult
    {
        IsSuccess = false,
        Message = "Invalid username or password."
    };

    public AuthenticationResult RegisterResult { get; set; } = new AuthenticationResult
    {
        IsSuccess = false,
        Message = "This username already exists."
    };

    public bool IsLoggedIn { get; set; }

    public AuthenticationResult Register(string username, string password)
    {
        RegisterWasCalled = true;
        UsernamePassedToRegister = username;
        PasswordPassedToRegister = password;

        return RegisterResult;
    }

    public AuthenticationResult Login(string username, string password)
    {
        LoginWasCalled = true;
        UsernamePassedToLogin = username;
        PasswordPassedToLogin = password;

        return LoginResult;
    }

    public void Logout()
    {
        LogoutWasCalled = true;
    }
}
