using Spectre.Console;

public class RichConsoleAuthenticationPrompt : IAuthenticationPrompt
{
    private readonly IAuthenticationService _authenticationService;

    public RichConsoleAuthenticationPrompt(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public AuthenticationResult Run()
    {
        while (true)
        {
            string selectedOption = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold blue]Authentication[/]")
                    .AddChoices("Login", "Register", "Exit"));

            switch (selectedOption)
            {
                case "Login":
                    AuthenticationResult loginResult = Login();

                    if (loginResult.IsSuccess)
                    {
                        return loginResult;
                    }

                    break;

                case "Register":
                    AuthenticationResult registerResult = Register();

                    if (registerResult.IsSuccess)
                    {
                        WaitForContinue();
                        return registerResult;
                    }

                    break;

                case "Exit":
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Exit requested."
                    };
            }
        }
    }

    private AuthenticationResult Login()
    {
        string username = AnsiConsole.Ask<string>("Username:");

        string password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password:")
                .Secret());

        AuthenticationResult result = _authenticationService.Login(username, password);

        ShowResult(result);

        return result;
    }

    private AuthenticationResult Register()
    {
        string username = AnsiConsole.Ask<string>("Username:");

        string password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password:")
                .Secret());

        AuthenticationResult result = _authenticationService.Register(username, password);

        ShowResult(result);

        return result;
    }

    private static void ShowResult(AuthenticationResult result)
    {
        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
    }

    private static void WaitForContinue()
    {
        AnsiConsole.MarkupLine("[purple]Press Enter to continue.[/]");
        Console.ReadLine();
    }
}
