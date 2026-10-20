using Microsoft.Extensions.Logging;

public static class AppServiceFactory
{
    public static void EnsureDatabaseIsUpToDate(StorageMode storageMode, string databasePath)
    {
        if (storageMode != StorageMode.Database)
        {
            return;
        }

        EnsureDatabaseIsUpToDate(databasePath);
    }

    public static void EnsureDatabaseIsUpToDate(string databasePath)
    {
        DatabaseInitializer databaseInitializer = new DatabaseInitializer(databasePath);
        databaseInitializer.Initialize();

        DatabaseMigrator migrator = new DatabaseMigrator(databasePath);
        migrator.ApplyMigrations();
    }

    public static IRecipeRepository CreateRecipeRepository(
        StorageMode storageMode,
        string recipesFilePath,
        string databasePath,
        ILogger<DatabaseRecipeRepository> logger)
    {
        return storageMode switch
        {
            StorageMode.Json => new JsonRecipeRepository(recipesFilePath),
            StorageMode.Database => new DatabaseRecipeRepository(databasePath, logger),
            _ => throw new InvalidOperationException(AppTexts.UnknownStorageModeError)
        };
    }

    public static IRecipeRepository CreateDatabaseRecipeRepository(
        string databasePath,
        ILogger<DatabaseRecipeRepository> logger)
    {
        return new DatabaseRecipeRepository(databasePath, logger);
    }

    public static IRecipePhotoRepository CreateDatabaseRecipePhotoRepository(
        string databasePath)
    {
        return new DatabaseRecipePhotoRepository(databasePath);
    }

    internal static IRecipePhotoStorage CreateDatabaseRecipePhotoStorage(
        string recipePhotosPath)
    {
        return new FileSystemRecipePhotoStorage(recipePhotosPath);
    }

    public static IRecipePhotoService CreateDatabaseRecipePhotoService(
        string databasePath,
        string recipePhotosPath,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        ILoggerFactory loggerFactory)
    {
        return CreateDatabaseRecipePhotoService(
            databasePath,
            recipePhotosPath,
            recipeRepository,
            currentUserContext,
            new RecipePhotoOptions(),
            TimeProvider.System,
            loggerFactory);
    }

    public static IRecipePhotoService CreateDatabaseRecipePhotoService(
        string databasePath,
        string recipePhotosPath,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        RecipePhotoOptions options,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IRecipePhotoRepository photoRepository =
            CreateDatabaseRecipePhotoRepository(databasePath);
        IRecipePhotoStorage storage =
            CreateDatabaseRecipePhotoStorage(recipePhotosPath);
        RecipePhotoValidator validator = new();
        RecipePhotoFileNameNormalizer fileNameNormalizer = new();
        ILogger<RecipePhotoService> logger =
            loggerFactory.CreateLogger<RecipePhotoService>();

        return new RecipePhotoService(
            storage,
            photoRepository,
            recipeRepository,
            currentUserContext,
            validator,
            fileNameNormalizer,
            options,
            timeProvider,
            logger);
    }

    public static IRecipePhotoReconciliationService
        CreateDatabaseRecipePhotoReconciliationService(
            string databasePath,
            string recipePhotosPath,
            IRecipeRepository recipeRepository,
            ILoggerFactory loggerFactory)
    {
        return CreateDatabaseRecipePhotoReconciliationService(
            databasePath,
            recipePhotosPath,
            recipeRepository,
            new RecipePhotoOptions(),
            TimeProvider.System,
            loggerFactory);
    }

    public static IRecipePhotoReconciliationService
        CreateDatabaseRecipePhotoReconciliationService(
            string databasePath,
            string recipePhotosPath,
            IRecipeRepository recipeRepository,
            RecipePhotoOptions options,
            TimeProvider timeProvider,
            ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IRecipePhotoStorage storage =
            CreateDatabaseRecipePhotoStorage(recipePhotosPath);
        IRecipePhotoRepository photoRepository =
            CreateDatabaseRecipePhotoRepository(databasePath);
        ILogger<RecipePhotoReconciliationService> logger =
            loggerFactory.CreateLogger<RecipePhotoReconciliationService>();

        return new RecipePhotoReconciliationService(
            storage,
            recipeRepository,
            photoRepository,
            options,
            timeProvider,
            logger);
    }

    public static IUserRepository CreateUserRepository(
        StorageMode storageMode,
        string usersFilePath,
        string databasePath)
    {
        return storageMode switch
        {
            StorageMode.Json => new JsonUserRepository(usersFilePath),
            StorageMode.Database => new DatabaseUserRepository(databasePath),
            _ => throw new InvalidOperationException(AppTexts.UnknownStorageModeError)
        };
    }

    public static IUserRepository CreateDatabaseUserRepository(string databasePath)
    {
        return new DatabaseUserRepository(databasePath);
    }

    public static IRecipeImporterService CreateRecipeImporterService(
        HttpClient httpClient,
        RecipeImportDestinationPolicy destinationPolicy)
    {
        RecipeImporter recipeImporter = new RecipeImporter(
            httpClient,
            destinationPolicy);

        return new RecipeImporterService(recipeImporter);
    }

    public static IRecipeLibraryService CreateRecipeLibraryService(
        IRecipeRepository recipeRepository,
        ICurrentUserContext currentUserContext)
    {
        return new RecipeLibraryService(recipeRepository, currentUserContext);
    }

    public static IRecipeLibraryService CreateDatabaseRecipeLibraryService(
        string databasePath,
        string recipePhotosPath,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        ILoggerFactory loggerFactory)
    {
        return CreateDatabaseRecipeLibraryService(
            databasePath,
            recipePhotosPath,
            recipeRepository,
            currentUserContext,
            new RecipePhotoOptions(),
            TimeProvider.System,
            loggerFactory);
    }

    public static IRecipeLibraryService CreateDatabaseRecipeLibraryService(
        string databasePath,
        string recipePhotosPath,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        RecipePhotoOptions photoOptions,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(photoOptions);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IRecipePhotoService photoService =
            CreateDatabaseRecipePhotoService(
                databasePath,
                recipePhotosPath,
                recipeRepository,
                currentUserContext,
                photoOptions,
                timeProvider,
                loggerFactory);
        IRecipePhotoDeletionCoordinator photoDeletionCoordinator =
            (IRecipePhotoDeletionCoordinator)photoService;
        ILogger<RecipeLibraryService> logger =
            loggerFactory.CreateLogger<RecipeLibraryService>();

        return new RecipeLibraryService(
            recipeRepository,
            currentUserContext,
            photoDeletionCoordinator,
            logger);
    }

    public static IRecipeBackupService CreateRecipeBackupService(
        IRecipeRepository recipeRepository, ICurrentUserContext currentUserContext)
    {
        return new RecipeBackupService(recipeRepository, currentUserContext);
    }

    public static IAuthenticationService CreateAuthenticationService(
        StorageMode storageMode,
        string usersFilePath,
        string databasePath,
        ICurrentUserContext currentUserContext)
    {
        IUserRepository userRepository = CreateUserRepository(
            storageMode,
            usersFilePath,
            databasePath);

        return CreateAuthenticationService(userRepository, currentUserContext);
    }

    public static IAuthenticationService CreateAuthenticationService(
        IUserRepository userRepository, ICurrentUserContext currentUserContext)
    {
        return new AuthenticationService(userRepository, currentUserContext);
    }
}
