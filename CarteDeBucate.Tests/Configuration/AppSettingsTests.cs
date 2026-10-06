public class AppSettingsTests
{
    [Fact]
    public void Load_WhenConfigurationFileIsMissing_ShouldUseDefaults()
    {
        string tempDirectory = CreateTemporaryDirectory();

        try
        {
            string missingFilePath = Path.Combine(tempDirectory, "missing-appsettings.json");

            AppSettings settings = AppSettings.Load(missingFilePath);

            Assert.Equal(StorageMode.Json, settings.CurrentStorageMode);
            Assert.Equal(InterfaceMode.ClassicConsole, settings.CurrentInterfaceMode);
            Assert.Equal("recipes.json", settings.RecipesFilePath);
            Assert.Equal("recipes.db", settings.DatabasePath);
            Assert.Equal("recipe-photos", settings.RecipePhotosPath);
            Assert.False(settings.AuthenticationEnabled);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenConfigurationFileExists_ShouldUseConfiguredValues()
    {
        string tempDirectory = CreateTemporaryDirectory();

        try
        {
            string filePath = Path.Combine(tempDirectory, "appsettings.json");

            File.WriteAllText(
                filePath,
                """
                {
                  "StorageMode": "Database",
                  "InterfaceMode": "RichConsole",
                  "RecipesFilePath": "custom-recipes.json",
                  "UsersFilePath": "custom-users.json",
                  "DatabasePath": "custom-recipes.db",
                  "RecipePhotosPath": "custom-recipe-photos",
                  "AuthenticationEnabled": true
                }
                """);

            AppSettings settings = AppSettings.Load(filePath);

            Assert.Equal(StorageMode.Database, settings.CurrentStorageMode);
            Assert.Equal(InterfaceMode.RichConsole, settings.CurrentInterfaceMode);
            Assert.Equal("custom-recipes.json", settings.RecipesFilePath);
            Assert.Equal("custom-users.json", settings.UsersFilePath);
            Assert.Equal("custom-recipes.db", settings.DatabasePath);
            Assert.Equal("custom-recipe-photos", settings.RecipePhotosPath);
            Assert.True(settings.AuthenticationEnabled);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenConfigurationValuesAreInvalid_ShouldKeepDefaults()
    {
        string tempDirectory = CreateTemporaryDirectory();

        try
        {
            string filePath = Path.Combine(tempDirectory, "appsettings.json");

            File.WriteAllText(
                filePath,
                """
                {
                  "StorageMode": "Unknown",
                  "InterfaceMode": "Unknown",
                  "RecipesFilePath": "",
                  "UsersFilePath": "",
                  "DatabasePath": "",
                  "RecipePhotosPath": "   ",
                  "AuthenticationEnabled": "not-a-bool"
                }
                """);

            AppSettings settings = AppSettings.Load(filePath);

            Assert.Equal(StorageMode.Json, settings.CurrentStorageMode);
            Assert.Equal(InterfaceMode.ClassicConsole, settings.CurrentInterfaceMode);
            Assert.Equal("recipes.json", settings.RecipesFilePath);
            Assert.Equal("users.json", settings.UsersFilePath);
            Assert.Equal("recipes.db", settings.DatabasePath);
            Assert.Equal("recipe-photos", settings.RecipePhotosPath);
            Assert.False(settings.AuthenticationEnabled);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenConfigurationFileContainsInvalidJson_ShouldUseDefaults()
    {
        string tempDirectory = CreateTemporaryDirectory();

        try
        {
            string filePath = Path.Combine(tempDirectory, "appsettings.json");

            File.WriteAllText(filePath, "{ invalid json }");

            AppSettings settings = AppSettings.Load(filePath);

            Assert.Equal("recipe-photos", settings.RecipePhotosPath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"settings-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDirectory);

        return tempDirectory;
    }
}
