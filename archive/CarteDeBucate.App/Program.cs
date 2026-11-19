using Microsoft.Extensions.Logging.Abstractions;

AppSettings appSettings = AppSettings.Load();

AppServiceFactory.EnsureDatabaseIsUpToDate(
    appSettings.CurrentStorageMode,
    appSettings.DatabasePath);

IRecipeConsoleReader recipeReader = new RecipeConsoleReader();
IRecipeConsoleWriter recipeWriter = new RecipeConsoleWriter();

ICurrentUserContext currentUserContext = new CurrentUserContext();
IRecipeRepository recipeRepository = AppServiceFactory.CreateRecipeRepository(
    appSettings.CurrentStorageMode,
    appSettings.RecipesFilePath,
    appSettings.DatabasePath,
    NullLogger<DatabaseRecipeRepository>.Instance);

IRecipeLibraryService recipeLibraryService =
    AppServiceFactory.CreateRecipeLibraryService(recipeRepository, currentUserContext);

IHostAddressResolver hostAddressResolver = new DnsHostAddressResolver();
RecipeImportDestinationPolicy destinationPolicy =
    new RecipeImportDestinationPolicy(hostAddressResolver);
RecipeImportHttpMessageHandler httpMessageHandler =
    new RecipeImportHttpMessageHandler(destinationPolicy);
using HttpClient recipeImporterHttpClient = new HttpClient(httpMessageHandler);
RecipeImporterHttpClientConfiguration.Configure(recipeImporterHttpClient);

IRecipeImporterService importerService =
    AppServiceFactory.CreateRecipeImporterService(
        recipeImporterHttpClient,
        destinationPolicy);

IRecipeBackupService backupService =
    AppServiceFactory.CreateRecipeBackupService(recipeRepository, currentUserContext);

IAuthenticationService authenticationService =
    AppServiceFactory.CreateAuthenticationService(
        appSettings.CurrentStorageMode,
        appSettings.UsersFilePath,
        appSettings.DatabasePath,
        currentUserContext);

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
