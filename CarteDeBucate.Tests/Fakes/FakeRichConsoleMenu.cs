public class FakeRichConsoleMenu : IRichConsoleMenu
{
    public Queue<MainMenuOption> OptionsToReturn { get; } = new();

    public bool ShowMainMenuWasCalled { get; private set; }

    public MainMenuOption ShowMainMenu()
    {
        ShowMainMenuWasCalled = true;

        return OptionsToReturn.Count == 0
            ? MainMenuOption.Exit
            : OptionsToReturn.Dequeue();
    }
}
