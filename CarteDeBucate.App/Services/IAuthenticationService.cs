public interface IAuthenticationService
{
    AuthenticationResult Register(string username, string password);

    AuthenticationResult Login(string username, string password);

    void Logout();

    bool IsLoggedIn { get; }

    User? CurrentUser { get; }
}