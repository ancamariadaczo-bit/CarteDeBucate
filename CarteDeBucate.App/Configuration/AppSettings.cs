using System.Text.Json;

public class AppSettings
{
    private const string ConfigurationFileName = "appsettings.json";

    public string RecipesFilePath { get; private set; } = "recipes.json";

    public string UsersFilePath { get; private set; } = "users.json";

    public string DatabasePath { get; private set; } = "recipes.db";

    public StorageMode CurrentStorageMode { get; private set; } = StorageMode.Json;

    public InterfaceMode CurrentInterfaceMode { get; private set; } = InterfaceMode.ClassicConsole;

    public static AppSettings Load()
    {
        string? configurationFilePath = FindConfigurationFilePath();

        return Load(configurationFilePath);
    }

    public static AppSettings Load(string? configurationFilePath)
    {
        if (string.IsNullOrWhiteSpace(configurationFilePath) || !File.Exists(configurationFilePath))
        {
            return new AppSettings();
        }

        string json = File.ReadAllText(configurationFilePath);

        AppSettingsFile? settingsFile = JsonSerializer.Deserialize<AppSettingsFile>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return FromFile(settingsFile);
    }

    private static string? FindConfigurationFilePath()
    {
        string currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ConfigurationFileName);

        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        string executableDirectoryPath = Path.Combine(AppContext.BaseDirectory, ConfigurationFileName);

        if (File.Exists(executableDirectoryPath))
        {
            return executableDirectoryPath;
        }

        return null;
    }

    private static AppSettings FromFile(AppSettingsFile? settingsFile)
    {
        AppSettings settings = new AppSettings();

        if (settingsFile == null)
        {
            return settings;
        }

        if (!string.IsNullOrWhiteSpace(settingsFile.JsonFilePath))
        {
            settings.RecipesFilePath = settingsFile.JsonFilePath;
        }

        if (!string.IsNullOrWhiteSpace(settingsFile.DatabasePath))
        {
            settings.DatabasePath = settingsFile.DatabasePath;
        }

        if (Enum.TryParse(settingsFile.StorageMode, ignoreCase: true, out StorageMode storageMode))
        {
            settings.CurrentStorageMode = storageMode;
        }

        if (Enum.TryParse(settingsFile.InterfaceMode, ignoreCase: true, out InterfaceMode interfaceMode))
        {
            settings.CurrentInterfaceMode = interfaceMode;
        }

        return settings;
    }

    private class AppSettingsFile
    {
        public string? StorageMode { get; set; }

        public string? InterfaceMode { get; set; }

        public string? JsonFilePath { get; set; }

        public string? DatabasePath { get; set; }
    }
}
