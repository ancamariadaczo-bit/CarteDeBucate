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
            Assert.Equal("recipes.json", settings.JsonFilePath);
            Assert.Equal("recipes.db", settings.DatabasePath);
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
                  "JsonFilePath": "custom-recipes.json",
                  "DatabasePath": "custom-recipes.db"
                }
                """);

            AppSettings settings = AppSettings.Load(filePath);

            Assert.Equal(StorageMode.Database, settings.CurrentStorageMode);
            Assert.Equal(InterfaceMode.RichConsole, settings.CurrentInterfaceMode);
            Assert.Equal("custom-recipes.json", settings.JsonFilePath);
            Assert.Equal("custom-recipes.db", settings.DatabasePath);
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
                  "JsonFilePath": "",
                  "DatabasePath": ""
                }
                """);

            AppSettings settings = AppSettings.Load(filePath);

            Assert.Equal(StorageMode.Json, settings.CurrentStorageMode);
            Assert.Equal(InterfaceMode.ClassicConsole, settings.CurrentInterfaceMode);
            Assert.Equal("recipes.json", settings.JsonFilePath);
            Assert.Equal("recipes.db", settings.DatabasePath);
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
