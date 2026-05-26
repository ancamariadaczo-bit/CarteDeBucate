public static class RecipeAppFactory
{
    public static IRecipeApp Create(
        InterfaceMode uiMode,
        IRecipeConsoleReader recipeReader,
        IRecipeConsoleWriter recipeWriter,
        IRecipeImporterService importerService,
        IRecipeBackupService backupService)
    {
        return uiMode switch
        {
            InterfaceMode.ClassicConsole =>
                new ClassicConsoleRecipeApp(recipeReader, recipeWriter, importerService, backupService),
            InterfaceMode.RichConsole =>
                new RichConsoleRecipeApp(importerService, backupService),
            _ => throw new InvalidOperationException("Unknown UI mode.")
        };
    }
}