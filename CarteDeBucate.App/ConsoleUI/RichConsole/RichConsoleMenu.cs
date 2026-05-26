using Spectre.Console;

public class RichConsoleMenu
{
    public MainMenuOption ShowMainMenu()
    {
        string selectedOption = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Ce vrei să faci?[/]")
                .PageSize(10)
                .AddChoices(
                    AppTexts.MenuAddRecipe,
                    AppTexts.MenuImportRecipeFromUrl,
                    AppTexts.MenuShowRecipes,
                    AppTexts.MenuSearchRecipe,
                    AppTexts.MenuViewRecipeDetails,
                    AppTexts.MenuEditRecipe,
                    AppTexts.MenuDeleteRecipe,
                    AppTexts.MenuExportBackup,
                    AppTexts.MenuImportBackup,
                    AppTexts.MenuExit));

        return selectedOption switch
        {
            AppTexts.MenuAddRecipe => MainMenuOption.AddRecipe,
            AppTexts.MenuImportRecipeFromUrl => MainMenuOption.ImportRecipeFromUrl,
            AppTexts.MenuShowRecipes => MainMenuOption.ShowRecipes,
            AppTexts.MenuSearchRecipe => MainMenuOption.SearchRecipe,
            AppTexts.MenuViewRecipeDetails => MainMenuOption.ViewRecipeDetails,
            AppTexts.MenuEditRecipe => MainMenuOption.EditRecipe,
            AppTexts.MenuDeleteRecipe => MainMenuOption.DeleteRecipe,
            AppTexts.MenuExportBackup => MainMenuOption.ExportBackup,
            AppTexts.MenuImportBackup => MainMenuOption.ImportBackup,
            AppTexts.MenuExit => MainMenuOption.Exit,
            _ => MainMenuOption.Exit
        };
    }
}