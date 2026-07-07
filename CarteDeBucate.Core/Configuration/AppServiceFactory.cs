public static class AppServiceFactory
{
    public static void EnsureDatabaseIsUpToDate(AppSettings settings)
    {
        if (settings.CurrentStorageMode != StorageMode.Database)
        {
            return;
        }

        EnsureDatabaseIsUpToDate(settings.DatabasePath);
    }

    public static void EnsureDatabaseIsUpToDate(string databasePath)
    {
        DatabaseInitializer databaseInitializer = new DatabaseInitializer(databasePath);
        databaseInitializer.Initialize();

        DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
        migrator.ApplyMigrations();
    }

    public static IRecipeRepository CreateRecipeRepository(AppSettings settings)
    {
        return settings.CurrentStorageMode switch
        {
            StorageMode.Json => new JsonRecipeRepository(settings.RecipesFilePath),
            StorageMode.Database => new DatabaseRecipeRepository(settings.DatabasePath),
            _ => throw new InvalidOperationException(AppTexts.UnknownStorageModeError)
        };
    }

    public static IRecipeRepository CreateDatabaseRecipeRepository(string databasePath)
    {
        return new DatabaseRecipeRepository(databasePath);
    }

    public static IUserRepository CreateUserRepository(AppSettings settings)
    {
        return settings.CurrentStorageMode switch
        {
            StorageMode.Json => new JsonUserRepository(settings.UsersFilePath),
            StorageMode.Database => new DatabaseUserRepository(settings.DatabasePath),
            _ => throw new InvalidOperationException(AppTexts.UnknownStorageModeError)
        };
    }

    public static IUserRepository CreateDatabaseUserRepository(string databasePath)
    {
        return new DatabaseUserRepository(databasePath);
    }

    public static IRecipeImporterService CreateRecipeImporterService()
    {
        RecipeImporter recipeImporter = new RecipeImporter();

        return new RecipeImporterService(recipeImporter);
    }

    public static IRecipeLibraryService CreateRecipeLibraryService(
        IRecipeRepository recipeRepository,
        ICurrentUserContext currentUserContext)
    {
        return new RecipeLibraryService(recipeRepository, currentUserContext);
    }

    public static IRecipeBackupService CreateRecipeBackupService(
        IRecipeRepository recipeRepository, ICurrentUserContext currentUserContext)
    {
        return new RecipeBackupService(recipeRepository, currentUserContext);
    }

    public static IAuthenticationService CreateAuthenticationService(
        AppSettings settings, ICurrentUserContext currentUserContext)
    {
        IUserRepository userRepository = CreateUserRepository(settings);

        return CreateAuthenticationService(userRepository, currentUserContext);
    }

    public static IAuthenticationService CreateAuthenticationService(
        IUserRepository userRepository, ICurrentUserContext currentUserContext)
    {
        return new AuthenticationService(userRepository, currentUserContext);
    }
}
