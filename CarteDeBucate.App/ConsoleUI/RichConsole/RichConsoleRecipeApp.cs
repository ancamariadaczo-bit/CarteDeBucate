using Spectre.Console;

public class RichConsoleRecipeApp : IRecipeApp
{
    private readonly IRecipeImporterService _importerService;
    private readonly IRecipeBackupService _backupService;
    private readonly RichConsoleMenu _menu;
    private readonly RichConsoleDisplay _display;

    public RichConsoleRecipeApp(
        IRecipeImporterService importerService,
        IRecipeBackupService backupService)
    {
        _importerService = importerService;
        _backupService = backupService;
        _menu = new RichConsoleMenu();
        _display = new RichConsoleDisplay();
    }

    public async Task RunAsync()
    {
        bool shouldExit = false;

        while (!shouldExit)
        {
            _display.ShowTitle();

            MainMenuOption selectedOption = _menu.ShowMainMenu();

            AnsiConsole.Clear();

            switch (selectedOption)
            {
                case MainMenuOption.AddRecipe:
                    //await AddRecipe();
                    _display.ShowInfo("Funcționalitatea de AddRecipe va fi conectată aici.");
                    break;

                case MainMenuOption.ImportRecipeFromUrl:
                    //await ImportFromUrlAsync();
                    _display.ShowInfo("Funcționalitatea de ImportRecipeFromUrl va fi conectată aici.");
                    break;

                case MainMenuOption.ShowRecipes:
                    //await ShowAllRecipesAsync();
                    _display.ShowInfo("Funcționalitatea de ShowRecipes va fi conectată aici.");
                    break;

                case MainMenuOption.SearchRecipe:
                    //await SearchRecipeAsync();
                    _display.ShowInfo("Funcționalitatea de SearchRecipe va fi conectată aici.");
                    break;

                case MainMenuOption.ViewRecipeDetails:
                    //await ViewRecipeDetails();
                    _display.ShowInfo("Funcționalitatea de ViewRecipeDetails va fi conectată aici.");
                    break;

                case MainMenuOption.EditRecipe:
                    //await EditRecipe();
                    _display.ShowInfo("Funcționalitatea de EditRecipe va fi conectată aici.");
                    break;

                case MainMenuOption.DeleteRecipe:
                    //await DeleteRecipe();
                    _display.ShowInfo("Funcționalitatea de DeleteRecipe va fi conectată aici.");
                    break;

                case MainMenuOption.ExportBackup:
                    _display.ShowInfo("Funcționalitatea de backup va fi conectată aici.");
                    break;

                case MainMenuOption.ImportBackup:
                    _display.ShowInfo("Funcționalitatea de import backup va fi conectată aici.");
                    break;

                case MainMenuOption.Exit:
                    shouldExit = true;
                    break;
            }

            if (!shouldExit)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]Apasă Enter pentru a continua...[/]");
                Console.ReadLine();
            }
        }
    }

    private async Task ShowAllRecipesAsync()
    {
        List<Recipe> recipes = _importerService.GetAllRecipes();

        _display.ShowRecipes(recipes);
    }

    private async Task SearchRecipeAsync()
    {
        string searchText = AnsiConsole.Ask<string>("Caută după [green]cuvânt[/]:");

        List<Recipe> recipes = _importerService.SearchRecipes(searchText);

        _display.ShowRecipes(recipes);
    }

    private async Task ImportFromUrlAsync()
    {
        string url = AnsiConsole.Ask<string>("Introdu [green]link-ul rețetei[/]:");

        RecipeImportResult result = await _importerService.ImportRecipeFromUrlAsync(url);

        if (result.Success)
        {
            _display.ShowSuccess(result.Message);
        }
        else
        {
            _display.ShowError(result.Message);
        }
    }
}