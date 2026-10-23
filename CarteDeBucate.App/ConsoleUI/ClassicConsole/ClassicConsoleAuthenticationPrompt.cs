public class ClassicConsoleAuthenticationPrompt : IAuthenticationPrompt
{
    private readonly IAuthenticationService _authenticationService;

    public ClassicConsoleAuthenticationPrompt(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public AuthenticationResult Run()
    {
        while (true)
        {
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Register");
            Console.WriteLine("3. Exit");
            Console.Write("Choose an option: ");

            string? option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    AuthenticationResult loginResult = Login();

                    if (loginResult.IsSuccess)
                    {
                        return loginResult;
                    }

                    break;

                case "2":
                    AuthenticationResult registerResult = Register();

                    if (registerResult.IsSuccess)
                    {
                        WaitForContinue();
                        return registerResult;
                    }

                    break;

                case "3":
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Exit requested."
                    };

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private AuthenticationResult Login()
    {
        Console.Write("Username: ");
        string username = Console.ReadLine() ?? string.Empty;

        Console.Write("Password: ");
        string password = Console.ReadLine() ?? string.Empty;

        AuthenticationResult result = _authenticationService.Login(username, password);
        Console.WriteLine(result.Message);

        return result;
    }

    private AuthenticationResult Register()
    {
        Console.Write("Username: ");
        string username = Console.ReadLine() ?? string.Empty;

        Console.Write("Password: ");
        string password = Console.ReadLine() ?? string.Empty;

        AuthenticationResult result = _authenticationService.Register(username, password);
        Console.WriteLine(result.Message);

        return result;
    }

    private static void WaitForContinue()
    {
        Console.WriteLine("Press Enter to continue.");
        Console.ReadLine();
    }
}
