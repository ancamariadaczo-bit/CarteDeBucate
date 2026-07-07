
AppSettings appSettings = AppSettings.Load();

AppServiceFactory.EnsureDatabaseIsUpToDate(appSettings);

IRecipeConsoleReader recipeReader = new RecipeConsoleReader();
IRecipeConsoleWriter recipeWriter = new RecipeConsoleWriter();

ICurrentUserContext currentUserContext = new CurrentUserContext();
IRecipeRepository recipeRepository = AppServiceFactory.CreateRecipeRepository(appSettings);

IRecipeLibraryService recipeLibraryService =
    AppServiceFactory.CreateRecipeLibraryService(recipeRepository, currentUserContext);

IRecipeImporterService importerService =
    AppServiceFactory.CreateRecipeImporterService();

IRecipeBackupService backupService =
    AppServiceFactory.CreateRecipeBackupService(recipeRepository, currentUserContext);

IAuthenticationService authenticationService =
    AppServiceFactory.CreateAuthenticationService(appSettings, currentUserContext);

if (appSettings.AuthenticationEnabled)
{
    IAuthenticationPrompt authenticationPrompt = AuthenticationPromptFactory.Create(
        appSettings.CurrentInterfaceMode, authenticationService);

    AuthenticationResult authenticationResult = authenticationPrompt.Run();

    if (!authenticationResult.IsSuccess)
    {
        return;
    }
}

IRecipeApp app = RecipeAppFactory.Create(
    appSettings.CurrentInterfaceMode,
    recipeReader,
    recipeWriter,
    importerService,
    recipeLibraryService,
    backupService);

await app.RunAsync();
