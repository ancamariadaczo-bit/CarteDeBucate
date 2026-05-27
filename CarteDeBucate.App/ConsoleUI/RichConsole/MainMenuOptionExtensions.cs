public static class MainMenuOptionExtensions
{
    public static string ToDisplayText(this MainMenuOption option)
    {
        return option switch
        {
            MainMenuOption.AddRecipe => RichConsoleTexts.MenuAddRecipe,
            MainMenuOption.ImportRecipeFromUrl => RichConsoleTexts.MenuImportRecipeFromUrl,
            MainMenuOption.ShowRecipes => RichConsoleTexts.MenuShowRecipes,
            MainMenuOption.SearchRecipe => RichConsoleTexts.MenuSearchRecipe,
            MainMenuOption.ViewRecipeDetails => RichConsoleTexts.MenuViewRecipeDetails,
            MainMenuOption.EditRecipe => RichConsoleTexts.MenuEditRecipe,
            MainMenuOption.DeleteRecipe => RichConsoleTexts.MenuDeleteRecipe,
            MainMenuOption.ExportBackup => RichConsoleTexts.MenuExportBackup,
            MainMenuOption.ImportBackup => RichConsoleTexts.MenuImportBackup,
            MainMenuOption.Exit => RichConsoleTexts.MenuExit,
            _ => option.ToString()
        };
    }
}
