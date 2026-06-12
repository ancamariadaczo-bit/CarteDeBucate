
AppSettings appSettings = AppSettings.Load();

EnsureDatabaseIsUpToDate(appSettings);

IRecipeConsoleReader recipeReader = new RecipeConsoleReader();
IRecipeConsoleWriter recipeWriter = new RecipeConsoleWriter();

IRecipeRepository recipeRepository = CreateRecipeRepository(appSettings);
RecipeImporter recipeImporter = new RecipeImporter();
ICurrentUserContext currentUserContext = new CurrentUserContext();

IRecipeImporterService importerService = new RecipeImporterService(
    recipeRepository,
    recipeImporter,
    currentUserContext);

IRecipeBackupService backupService = new RecipeBackupService(recipeRepository, currentUserContext);

IUserRepository userRepository = CreateUserRepository(appSettings);
IAuthenticationService authenticationService = new AuthenticationService(userRepository, currentUserContext);

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

void EnsureDatabaseIsUpToDate(AppSettings settings)
{
    if (settings.CurrentStorageMode != StorageMode.Database)
    {
        return;
    }

    DatabaseInitializer databaseInitializer = new DatabaseInitializer(settings.DatabasePath);
    databaseInitializer.Initialize();

    DatabaseMigrator migrator = new DatabaseMigrator(settings.DatabasePath);
    migrator.ApplyMigrations();
}

IRecipeRepository CreateRecipeRepository(AppSettings settings)
{
    return settings.CurrentStorageMode switch
    {
        StorageMode.Json => new JsonRecipeRepository(settings.RecipesFilePath),
        StorageMode.Database => new DatabaseRecipeRepository(settings.DatabasePath),
        _ => throw new InvalidOperationException(AppTexts.UnknownStorageModeError)
    };
}

IUserRepository CreateUserRepository(AppSettings settings)
{
    return settings.CurrentStorageMode switch
    {
        StorageMode.Json => new JsonUserRepository(settings.UsersFilePath),
        StorageMode.Database => new DatabaseUserRepository(settings.DatabasePath),
        _ => throw new InvalidOperationException(AppTexts.UnknownStorageModeError)
    };
}
