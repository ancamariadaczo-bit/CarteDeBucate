public class FakeAuthenticationService : IAuthenticationService
{
    public bool LoginWasCalled { get; private set; }

    public bool LogoutWasCalled { get; private set; }

    public string? EmailPassedToLogin { get; private set; }

    public string? PasswordPassedToLogin { get; private set; }

    public AuthenticationResult LoginResult { get; set; } = new AuthenticationResult
    {
        IsSuccess = false,
        Message = "Invalid email or password."
    };

    public bool IsLoggedIn { get; set; }

    public AuthenticationResult Login(string email, string password)
    {
        LoginWasCalled = true;
        EmailPassedToLogin = email;
        PasswordPassedToLogin = password;

        return LoginResult;
    }

    public void Logout()
    {
        LogoutWasCalled = true;
    }
}
