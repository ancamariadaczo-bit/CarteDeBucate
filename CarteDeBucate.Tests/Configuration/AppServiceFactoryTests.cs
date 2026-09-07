using Microsoft.Extensions.Logging.Abstractions;

public class AppServiceFactoryTests
{
    [Fact]
    public void CreateRecipeRepository_WithJsonStorage_ShouldCreateJsonRepository()
    {
        AppSettings settings = LoadSettings(StorageMode.Json);

        IRecipeRepository repository = AppServiceFactory.CreateRecipeRepository(
            settings,
            NullLogger<DatabaseRecipeRepository>.Instance);

        Assert.IsType<JsonRecipeRepository>(repository);
    }

    [Fact]
    public void CreateRecipeRepository_WithDatabaseStorage_ShouldCreateDatabaseRepository()
    {
        AppSettings settings = LoadSettings(StorageMode.Database);

        IRecipeRepository repository = AppServiceFactory.CreateRecipeRepository(
            settings,
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
    public void CreateUserRepository_WithJsonStorage_ShouldCreateJsonRepository()
    {
        AppSettings settings = LoadSettings(StorageMode.Json);

        IUserRepository repository = AppServiceFactory.CreateUserRepository(settings);

        Assert.IsType<JsonUserRepository>(repository);
    }

    [Fact]
    public void CreateUserRepository_WithDatabaseStorage_ShouldCreateDatabaseRepository()
    {
        AppSettings settings = LoadSettings(StorageMode.Database);

        IUserRepository repository = AppServiceFactory.CreateUserRepository(settings);

        Assert.IsType<DatabaseUserRepository>(repository);
    }

    [Fact]
    public async Task CreateRecipeImporterService_WithEmptyUrl_ShouldReturnFailedImport()
    {
        IRecipeImporterService service = AppServiceFactory.CreateRecipeImporterService();

        RecipeImportResult result = await service.ImportRecipeFromUrlAsync("");

        Assert.False(result.Success);
        Assert.Equal(AppTexts.EmptyUrl, result.Message);
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

    private static AppSettings LoadSettings(StorageMode storageMode)
    {
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"service-factory-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string configurationFilePath = Path.Combine(tempDirectory, "appsettings.json");

            File.WriteAllText(
                configurationFilePath,
                $$"""
                {
                  "StorageMode": "{{storageMode}}"
                }
                """);

            return AppSettings.Load(configurationFilePath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
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
