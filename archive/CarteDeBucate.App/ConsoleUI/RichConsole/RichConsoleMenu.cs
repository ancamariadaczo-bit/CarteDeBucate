using Spectre.Console;

public class RichConsoleMenu : IRichConsoleMenu
{
    private static readonly MainMenuOption[] MenuOptions =
    [
        MainMenuOption.AddRecipe,
        MainMenuOption.ImportRecipeFromUrl,
        MainMenuOption.ShowRecipes,
        MainMenuOption.SearchRecipe,
        MainMenuOption.ViewRecipeDetails,
        MainMenuOption.EditRecipe,
        MainMenuOption.DeleteRecipe,
        MainMenuOption.ExportBackup,
        MainMenuOption.ImportBackup,
        MainMenuOption.Exit
    ];

    public MainMenuOption ShowMainMenu()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<MainMenuOption>()
                .Title(RichConsoleTexts.MainMenuTitleMarkup)
                .PageSize(10)
                .UseConverter(option => option.ToDisplayText())
                .AddChoices(MenuOptions));
    }
}
