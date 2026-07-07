public static class RecipeAppFactory
{
    public static IRecipeApp Create(
        InterfaceMode uiMode,
        IRecipeConsoleReader recipeReader,
        IRecipeConsoleWriter recipeWriter,
        IRecipeImporterService importerService,
        IRecipeLibraryService recipeLibraryService,
        IRecipeBackupService backupService)
    {
        return uiMode switch
        {
            InterfaceMode.ClassicConsole =>
                new ClassicConsoleRecipeApp(
                    recipeReader,
                    recipeWriter,
                    importerService,
                    recipeLibraryService,
                    backupService),
            InterfaceMode.RichConsole =>
                new RichConsoleRecipeApp(importerService, recipeLibraryService, backupService),
            _ => throw new InvalidOperationException("Unknown UI mode.")
        };
    }
}
