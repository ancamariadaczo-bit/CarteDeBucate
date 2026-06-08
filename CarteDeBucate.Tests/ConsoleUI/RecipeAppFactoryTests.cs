public class RecipeAppFactoryTests
{
    [Fact]
    public void Create_WithClassicConsoleMode_ShouldReturnClassicConsoleRecipeApp()
    {
        IRecipeApp app = RecipeAppFactory.Create(
            InterfaceMode.ClassicConsole,
            new FakeRecipeConsoleReader(),
            new FakeRecipeConsoleWriter(),
            new FakeRecipeImporterService(),
            new FakeRecipeBackupService());

        Assert.IsType<ClassicConsoleRecipeApp>(app);
    }

    [Fact]
    public void Create_WithRichConsoleMode_ShouldReturnRichConsoleRecipeApp()
    {
        IRecipeApp app = RecipeAppFactory.Create(
            InterfaceMode.RichConsole,
            new FakeRecipeConsoleReader(),
            new FakeRecipeConsoleWriter(),
            new FakeRecipeImporterService(),
            new FakeRecipeBackupService());

        Assert.IsType<RichConsoleRecipeApp>(app);
    }

    [Fact]
    public void ToDisplayText_ShouldReturnTextForEveryMainMenuOption()
    {
        Assert.Equal(RichConsoleTexts.MenuAddRecipe, MainMenuOption.AddRecipe.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuImportRecipeFromUrl, MainMenuOption.ImportRecipeFromUrl.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuShowRecipes, MainMenuOption.ShowRecipes.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuSearchRecipe, MainMenuOption.SearchRecipe.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuViewRecipeDetails, MainMenuOption.ViewRecipeDetails.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuEditRecipe, MainMenuOption.EditRecipe.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuDeleteRecipe, MainMenuOption.DeleteRecipe.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuExportBackup, MainMenuOption.ExportBackup.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuImportBackup, MainMenuOption.ImportBackup.ToDisplayText());
        Assert.Equal(RichConsoleTexts.MenuExit, MainMenuOption.Exit.ToDisplayText());
    }
}
