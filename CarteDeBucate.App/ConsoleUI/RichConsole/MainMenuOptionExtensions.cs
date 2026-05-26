public static class MainMenuOptionExtensions
{
    public static string ToDisplayText(this MainMenuOption option)
    {
        return option switch
        {
            MainMenuOption.AddRecipe => AppTexts.MenuAddRecipe,
            MainMenuOption.ImportRecipeFromUrl => AppTexts.MenuImportRecipeFromUrl,
            MainMenuOption.ShowRecipes => AppTexts.MenuShowRecipes,
            MainMenuOption.SearchRecipe => AppTexts.MenuSearchRecipe,
            MainMenuOption.ViewRecipeDetails => AppTexts.MenuViewRecipeDetails,
            MainMenuOption.EditRecipe => AppTexts.MenuEditRecipe,
            MainMenuOption.DeleteRecipe => AppTexts.MenuDeleteRecipe,
            MainMenuOption.ExportBackup => AppTexts.MenuExportBackup,
            MainMenuOption.ImportBackup => AppTexts.MenuImportBackup,
            MainMenuOption.Exit => AppTexts.MenuExit,
            _ => option.ToString()
        };
    }
}