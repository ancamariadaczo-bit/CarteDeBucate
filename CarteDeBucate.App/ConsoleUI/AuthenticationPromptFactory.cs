public static class AuthenticationPromptFactory
{
    public static IAuthenticationPrompt Create(
        InterfaceMode interfaceMode,
        IAuthenticationService authenticationService)
    {
        return interfaceMode switch
        {
            InterfaceMode.ClassicConsole => new ClassicConsoleAuthenticationPrompt(authenticationService),
            InterfaceMode.RichConsole => new RichConsoleAuthenticationPrompt(authenticationService),
            _ => throw new InvalidOperationException("Unknown interface mode.")
        };
    }
}