using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Reflection;

public class AppServiceFactoryTests
{
    [Fact]
    public void FactoryMethods_ShouldNotReceiveAppSettings()
    {
        MethodInfo[] factoryMethods = typeof(AppServiceFactory).GetMethods(
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            factoryMethods.SelectMany(method => method.GetParameters()),
            parameter => parameter.ParameterType == typeof(AppSettings));
    }

    [Fact]
    public void CreateRecipeRepository_WithJsonStorage_ShouldCreateJsonRepository()
    {
        IRecipeRepository repository = AppServiceFactory.CreateRecipeRepository(
            StorageMode.Json,
            "recipes.json",
            "recipes.db",
            NullLogger<DatabaseRecipeRepository>.Instance);

        Assert.IsType<JsonRecipeRepository>(repository);
    }

    [Fact]
    public void CreateRecipeRepository_WithDatabaseStorage_ShouldCreateDatabaseRepository()
    {
        IRecipeRepository repository = AppServiceFactory.CreateRecipeRepository(
            StorageMode.Database,
            "recipes.json",
            "recipes.db",
            NullLogger<DatabaseRecipeRepository>.Instance);

        Assert.IsType<DatabaseRecipeRepository>(repository);
    }

    [Fact]
    public void CreateDatabaseRecipePhotoRepository_ShouldCreateDatabaseRepository()
    {
        IRecipePhotoRepository repository =
            AppServiceFactory.CreateDatabaseRecipePhotoRepository("recipes.db");

        Assert.IsType<DatabaseRecipePhotoRepository>(repository);
    }

    [Fact]
    public void CreateDatabaseRecipePhotoStorage_ShouldPassAbsoluteRootToStorage()
    {
        string databaseDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid():N}");
        string expectedRootPath = Path.Combine(
            databaseDirectoryPath,
            "recipe-photos");
        try
        {
            IRecipePhotoStorage storage =
                AppServiceFactory.CreateDatabaseRecipePhotoStorage(
                    expectedRootPath);

            Assert.IsType<FileSystemRecipePhotoStorage>(storage);
            Assert.True(Directory.Exists(expectedRootPath));
            Assert.True(Directory.Exists(Path.Combine(
                expectedRootPath,
                FileSystemRecipePhotoStorage.StagingDirectoryName)));
            Assert.True(Directory.Exists(Path.Combine(
                expectedRootPath,
                FileSystemRecipePhotoStorage.QuarantineDirectoryName)));
        }
        finally
        {
            if (Directory.Exists(databaseDirectoryPath))
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateDatabaseRecipePhotoService_ShouldComposeDefaultDependencies()
    {
        string databaseDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(
            databaseDirectoryPath,
            "recipes.db");
        string recipePhotosPath = Path.Combine(
            databaseDirectoryPath,
            "recipe-photos");
        FakeRecipeRepository recipeRepository = new();
        CurrentUserContext currentUserContext = new();
        RecordingLoggerFactory loggerFactory = new();

        try
        {
            IRecipePhotoService service =
                AppServiceFactory.CreateDatabaseRecipePhotoService(
                    databasePath,
                    recipePhotosPath,
                    recipeRepository,
                    currentUserContext,
                    loggerFactory);

            RecipePhotoService concreteService =
                Assert.IsType<RecipePhotoService>(service);
            Assert.IsType<FileSystemRecipePhotoStorage>(
                GetPrivateField<IRecipePhotoStorage>(
                    concreteService,
                    "_storage"));
            Assert.IsType<DatabaseRecipePhotoRepository>(
                GetPrivateField<IRecipePhotoRepository>(
                    concreteService,
                    "_photoRepository"));
            Assert.Same(
                recipeRepository,
                GetPrivateField<IRecipeRepository>(
                    concreteService,
                    "_recipeRepository"));
            Assert.Same(
                currentUserContext,
                GetPrivateField<ICurrentUserContext>(
                    concreteService,
                    "_currentUserContext"));
            Assert.IsType<RecipePhotoValidator>(
                GetPrivateField<RecipePhotoValidator>(
                    concreteService,
                    "_validator"));
            Assert.IsType<RecipePhotoFileNameNormalizer>(
                GetPrivateField<RecipePhotoFileNameNormalizer>(
                    concreteService,
                    "_fileNameNormalizer"));

            RecipePhotoOptions options =
                GetPrivateField<RecipePhotoOptions>(
                    concreteService,
                    "_options");
            Assert.Equal(10L * 1024 * 1024, options.MaxFileSizeBytes);
            Assert.Equal(20, options.MaxPhotosPerRecipe);
            Assert.Equal(
                TimeSpan.FromHours(24),
                options.ReconciliationMinimumAge);
            Assert.Same(
                TimeProvider.System,
                GetPrivateField<TimeProvider>(
                    concreteService,
                    "_timeProvider"));
            Assert.NotNull(GetPrivateField<ILogger<RecipePhotoService>>(
                concreteService,
                "_logger"));
            Assert.Equal(1, loggerFactory.CreateLoggerCallCount);
        }
        finally
        {
            if (Directory.Exists(databaseDirectoryPath))
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateDatabaseRecipePhotoService_WithConfigurableDependencies_ShouldPassProvidedInstances()
    {
        string databaseDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(
            databaseDirectoryPath,
            "recipes.db");
        string recipePhotosPath = Path.Combine(
            databaseDirectoryPath,
            "recipe-photos");
        RecipePhotoOptions options = new()
        {
            MaxFileSizeBytes = 1234,
            MaxPhotosPerRecipe = 7
        };
        FixedTimeProvider timeProvider = new(
            new DateTimeOffset(
                2026,
                10,
                13,
                12,
                0,
                0,
                TimeSpan.Zero));
        RecordingLoggerFactory loggerFactory = new();

        try
        {
            IRecipePhotoService service =
                AppServiceFactory.CreateDatabaseRecipePhotoService(
                    databasePath,
                    recipePhotosPath,
                    new FakeRecipeRepository(),
                    null,
                    options,
                    timeProvider,
                    loggerFactory);

            RecipePhotoService concreteService =
                Assert.IsType<RecipePhotoService>(service);
            Assert.Same(
                options,
                GetPrivateField<RecipePhotoOptions>(
                    concreteService,
                    "_options"));
            Assert.Same(
                timeProvider,
                GetPrivateField<TimeProvider>(
                    concreteService,
                    "_timeProvider"));
            Assert.Null(GetPrivateFieldValue(
                concreteService,
                "_currentUserContext"));
            Assert.Equal(1, loggerFactory.CreateLoggerCallCount);
        }
        finally
        {
            if (Directory.Exists(databaseDirectoryPath))
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateDatabaseRecipePhotoReconciliationService_ShouldComposeWithoutRunning()
    {
        string databaseDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(
            databaseDirectoryPath,
            "recipes.db");
        string recipePhotosPath = Path.Combine(
            databaseDirectoryPath,
            "recipe-photos");
        FakeRecipeRepository recipeRepository = new();
        RecordingLoggerFactory loggerFactory = new();

        try
        {
            IRecipePhotoReconciliationService service =
                AppServiceFactory
                    .CreateDatabaseRecipePhotoReconciliationService(
                        databasePath,
                        recipePhotosPath,
                        recipeRepository,
                        loggerFactory);

            RecipePhotoReconciliationService concreteService =
                Assert.IsType<RecipePhotoReconciliationService>(service);
            Assert.IsType<FileSystemRecipePhotoStorage>(
                GetPrivateField<IRecipePhotoStorage>(
                    concreteService,
                    "_storage"));
            Assert.IsType<DatabaseRecipePhotoRepository>(
                GetPrivateField<IRecipePhotoRepository>(
                    concreteService,
                    "_photoRepository"));
            Assert.Same(
                recipeRepository,
                GetPrivateField<IRecipeRepository>(
                    concreteService,
                    "_recipeRepository"));
            Assert.Equal(
                TimeSpan.FromHours(24),
                GetPrivateField<TimeSpan>(
                    concreteService,
                    "_reconciliationMinimumAge"));
            Assert.Same(
                TimeProvider.System,
                GetPrivateField<TimeProvider>(
                    concreteService,
                    "_timeProvider"));
            Assert.NotNull(
                GetPrivateField<
                    ILogger<RecipePhotoReconciliationService>>(
                        concreteService,
                        "_logger"));
            Assert.False(recipeRepository.GetAllRecipesWasCalled);
            Assert.Equal(1, loggerFactory.CreateLoggerCallCount);
        }
        finally
        {
            if (Directory.Exists(databaseDirectoryPath))
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateDatabaseRecipePhotoReconciliationService_WithConfigurableDependencies_ShouldPassProvidedValues()
    {
        string databaseDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(
            databaseDirectoryPath,
            "recipes.db");
        string recipePhotosPath = Path.Combine(
            databaseDirectoryPath,
            "recipe-photos");
        FakeRecipeRepository recipeRepository = new();
        RecipePhotoOptions options = new()
        {
            ReconciliationMinimumAge = TimeSpan.FromHours(6)
        };
        FixedTimeProvider timeProvider = new(
            new DateTimeOffset(
                2026,
                10,
                13,
                13,
                0,
                0,
                TimeSpan.Zero));
        RecordingLoggerFactory loggerFactory = new();

        try
        {
            IRecipePhotoReconciliationService service =
                AppServiceFactory
                    .CreateDatabaseRecipePhotoReconciliationService(
                        databasePath,
                        recipePhotosPath,
                        recipeRepository,
                        options,
                        timeProvider,
                        loggerFactory);

            RecipePhotoReconciliationService concreteService =
                Assert.IsType<RecipePhotoReconciliationService>(service);
            Assert.Equal(
                options.ReconciliationMinimumAge,
                GetPrivateField<TimeSpan>(
                    concreteService,
                    "_reconciliationMinimumAge"));
            Assert.Same(
                timeProvider,
                GetPrivateField<TimeProvider>(
                    concreteService,
                    "_timeProvider"));
            Assert.False(recipeRepository.GetAllRecipesWasCalled);
            Assert.Equal(1, loggerFactory.CreateLoggerCallCount);
        }
        finally
        {
            if (Directory.Exists(databaseDirectoryPath))
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateUserRepository_WithJsonStorage_ShouldCreateJsonRepository()
    {
        IUserRepository repository = AppServiceFactory.CreateUserRepository(
            StorageMode.Json,
            "users.json",
            "recipes.db");

        Assert.IsType<JsonUserRepository>(repository);
    }

    [Fact]
    public void CreateUserRepository_WithDatabaseStorage_ShouldCreateDatabaseRepository()
    {
        IUserRepository repository = AppServiceFactory.CreateUserRepository(
            StorageMode.Database,
            "users.json",
            "recipes.db");

        Assert.IsType<DatabaseUserRepository>(repository);
    }

    [Fact]
    public void CreateRecipeImporterService_ShouldRequireProvidedDependencies()
    {
        var factoryMethods = typeof(AppServiceFactory)
            .GetMethods()
            .Where(method =>
                method.Name == nameof(AppServiceFactory.CreateRecipeImporterService))
            .ToArray();

        var factoryMethod = Assert.Single(factoryMethods);
        var parameters = factoryMethod.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(HttpClient), parameters[0].ParameterType);
        Assert.Equal(
            typeof(RecipeImportDestinationPolicy),
            parameters[1].ParameterType);
    }

    [Fact]
    public void CreateRecipeImporterService_WithProvidedDependencies_ShouldCreateService()
    {
        using HttpClient httpClient = new HttpClient();
        RecipeImportDestinationPolicy destinationPolicy =
            new RecipeImportDestinationPolicy(new DnsHostAddressResolver());

        IRecipeImporterService service =
            AppServiceFactory.CreateRecipeImporterService(
                httpClient,
                destinationPolicy);

        Assert.IsType<RecipeImporterService>(service);
    }

    [Fact]
    public void CreateRecipeLibraryService_ShouldUseProvidedRepository()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        IRecipeLibraryService service =
            AppServiceFactory.CreateRecipeLibraryService(repository, currentUserContext);
        Recipe recipe = CreateValidRecipe();

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddRecipeWasCalled);
        Assert.Same(recipe, repository.AddedRecipe);
    }

    [Fact]
    public void CreateDatabaseRecipeLibraryService_ShouldUsePhotoDeletionCoordinator()
    {
        string databaseDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(
            databaseDirectoryPath,
            "recipes.db");
        string recipePhotosPath = Path.Combine(
            databaseDirectoryPath,
            "recipe-photos");
        FakeRecipeRepository recipeRepository = new();
        CurrentUserContext currentUserContext = new();
        RecordingLoggerFactory loggerFactory = new();

        try
        {
            IRecipeLibraryService service =
                AppServiceFactory.CreateDatabaseRecipeLibraryService(
                    databasePath,
                    recipePhotosPath,
                    recipeRepository,
                    currentUserContext,
                    loggerFactory);

            RecipeLibraryService concreteService =
                Assert.IsType<RecipeLibraryService>(service);
            Assert.Same(
                recipeRepository,
                GetPrivateField<IRecipeRepository>(
                    concreteService,
                    "_recipeRepository"));
            Assert.Same(
                currentUserContext,
                GetPrivateField<ICurrentUserContext>(
                    concreteService,
                    "_currentUserContext"));
            Assert.IsType<RecipePhotoService>(
                GetPrivateField<IRecipePhotoDeletionCoordinator>(
                    concreteService,
                    "_photoDeletionCoordinator"));
            Assert.NotNull(GetPrivateField<ILogger<RecipeLibraryService>>(
                concreteService,
                "_logger"));
            Assert.Equal(2, loggerFactory.CreateLoggerCallCount);
        }
        finally
        {
            if (Directory.Exists(databaseDirectoryPath))
            {
                Directory.Delete(databaseDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateRecipeLibraryService_ShouldRemainWithoutPhotoDeletionCoordinator()
    {
        IRecipeLibraryService service =
            AppServiceFactory.CreateRecipeLibraryService(
                new FakeRecipeRepository(),
                new CurrentUserContext());

        RecipeLibraryService concreteService =
            Assert.IsType<RecipeLibraryService>(service);
        Assert.Null(GetPrivateFieldValue(
            concreteService,
            "_photoDeletionCoordinator"));
    }

    [Fact]
    public void CreateAuthenticationService_ShouldUseProvidedRepository()
    {
        FakeUserRepository repository = new FakeUserRepository();
        CurrentUserContext currentUserContext = new CurrentUserContext();
        IAuthenticationService service =
            AppServiceFactory.CreateAuthenticationService(repository, currentUserContext);

        AuthenticationResult result = service.Register("anca", "secret-password");

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddWasCalled);
        Assert.True(currentUserContext.IsAuthenticated);
    }

    private static T GetPrivateField<T>(
        object instance,
        string fieldName)
    {
        return Assert.IsAssignableFrom<T>(
            GetPrivateFieldValue(instance, fieldName));
    }

    private static object? GetPrivateFieldValue(
        object instance,
        string fieldName)
    {
        FieldInfo? field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        return field.GetValue(instance);
    }

    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public int CreateLoggerCallCount { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            CreateLoggerCallCount++;

            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private static Recipe CreateValidRecipe()
    {
        return new Recipe
        {
            Name = "Test recipe",
            SourceUrl = "https://example.com/recipe",
            SavedAt = DateTime.Now,
            Ingredients = new List<string> { "Ingredient" },
            Steps = new List<string> { "Step" }
        };
    }
}
