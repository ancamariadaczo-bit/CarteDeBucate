public interface IAuthenticationService
{
    AuthenticationResult Login(string email, string password);

    void Logout();

    bool IsLoggedIn { get; }
}
