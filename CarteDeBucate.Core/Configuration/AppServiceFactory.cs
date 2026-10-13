using Microsoft.Extensions.Logging;

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

    public static IRecipeRepository CreateRecipeRepository(
        AppSettings settings,
        ILogger<DatabaseRecipeRepository> logger)
    {
        return settings.CurrentStorageMode switch
        {
            StorageMode.Json => new JsonRecipeRepository(settings.RecipesFilePath),
            StorageMode.Database => new DatabaseRecipeRepository(settings.DatabasePath, logger),
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
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string rootPath = ResolveDatabaseRecipePhotosPath(settings);

        return new FileSystemRecipePhotoStorage(rootPath);
    }

    internal static string ResolveDatabaseRecipePhotosPath(
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string normalizedDatabasePath = Path.GetFullPath(
            settings.DatabasePath);
        string databaseDirectoryPath = Path.GetDirectoryName(
            normalizedDatabasePath)
            ?? throw new InvalidOperationException(
                "The database path must have a parent directory.");

        if (Path.IsPathFullyQualified(settings.RecipePhotosPath))
        {
            return Path.GetFullPath(settings.RecipePhotosPath);
        }

        return Path.GetFullPath(
            settings.RecipePhotosPath,
            databaseDirectoryPath);
    }

    public static IRecipePhotoService CreateDatabaseRecipePhotoService(
        AppSettings settings,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        ILoggerFactory loggerFactory)
    {
        return CreateDatabaseRecipePhotoService(
            settings,
            recipeRepository,
            currentUserContext,
            new RecipePhotoOptions(),
            TimeProvider.System,
            loggerFactory);
    }

    public static IRecipePhotoService CreateDatabaseRecipePhotoService(
        AppSettings settings,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        RecipePhotoOptions options,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (settings.CurrentStorageMode != StorageMode.Database)
        {
            throw new InvalidOperationException(
                "Recipe photos require database storage.");
        }

        IRecipePhotoRepository photoRepository =
            CreateDatabaseRecipePhotoRepository(settings.DatabasePath);
        IRecipePhotoStorage storage =
            CreateDatabaseRecipePhotoStorage(settings);
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
            AppSettings settings,
            IRecipeRepository recipeRepository,
            ILoggerFactory loggerFactory)
    {
        return CreateDatabaseRecipePhotoReconciliationService(
            settings,
            recipeRepository,
            new RecipePhotoOptions(),
            TimeProvider.System,
            loggerFactory);
    }

    public static IRecipePhotoReconciliationService
        CreateDatabaseRecipePhotoReconciliationService(
            AppSettings settings,
            IRecipeRepository recipeRepository,
            RecipePhotoOptions options,
            TimeProvider timeProvider,
            ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (settings.CurrentStorageMode != StorageMode.Database)
        {
            throw new InvalidOperationException(
                "Recipe photo reconciliation requires database storage.");
        }

        IRecipePhotoStorage storage =
            CreateDatabaseRecipePhotoStorage(settings);
        IRecipePhotoRepository photoRepository =
            CreateDatabaseRecipePhotoRepository(settings.DatabasePath);
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
        AppSettings settings,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        ILoggerFactory loggerFactory)
    {
        return CreateDatabaseRecipeLibraryService(
            settings,
            recipeRepository,
            currentUserContext,
            new RecipePhotoOptions(),
            TimeProvider.System,
            loggerFactory);
    }

    public static IRecipeLibraryService CreateDatabaseRecipeLibraryService(
        AppSettings settings,
        IRecipeRepository recipeRepository,
        ICurrentUserContext? currentUserContext,
        RecipePhotoOptions photoOptions,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(photoOptions);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IRecipePhotoService photoService =
            CreateDatabaseRecipePhotoService(
                settings,
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
