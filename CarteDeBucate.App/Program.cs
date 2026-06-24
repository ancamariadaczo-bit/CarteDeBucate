
AppSettings appSettings = AppSettings.Load();

AppServiceFactory.EnsureDatabaseIsUpToDate(appSettings);

IRecipeConsoleReader recipeReader = new RecipeConsoleReader();
IRecipeConsoleWriter recipeWriter = new RecipeConsoleWriter();

ICurrentUserContext currentUserContext = new CurrentUserContext();
IRecipeRepository recipeRepository = AppServiceFactory.CreateRecipeRepository(appSettings);

IRecipeImporterService importerService =
    AppServiceFactory.CreateRecipeImporterService(recipeRepository, currentUserContext);

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
    appSettings.CurrentInterfaceMode, recipeReader, recipeWriter, importerService, backupService);

await app.RunAsync();
