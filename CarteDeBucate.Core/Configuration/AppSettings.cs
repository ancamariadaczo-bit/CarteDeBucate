using System.Text.Json;

public class AppSettings
{
    private const string ConfigurationFileName = "appsettings.json";

    public string RecipesFilePath { get; private set; } = "recipes.json";

    public string UsersFilePath { get; private set; } = "users.json";

    public string DatabasePath { get; private set; } = "recipes.db";

    public bool AuthenticationEnabled { get; set; } = false;

    //public bool AiFallbackEnabled { get; private set; } = false;

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

        AppSettingsFile? settingsFile;

        try
        {
            settingsFile = JsonSerializer.Deserialize<AppSettingsFile>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return new AppSettings();
        }

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

        if (!string.IsNullOrWhiteSpace(settingsFile.RecipesFilePath))
        {
            settings.RecipesFilePath = settingsFile.RecipesFilePath;
        }

        if (!string.IsNullOrWhiteSpace(settingsFile.UsersFilePath))
        {
            settings.UsersFilePath = settingsFile.UsersFilePath;
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

        if (TryReadBoolean(settingsFile.AuthenticationEnabled, out bool authenticationEnabled))
        {
            settings.AuthenticationEnabled = authenticationEnabled;
        }

        // if (TryReadBoolean(settingsFile.AiFallbackEnabled, out bool aiFallbackEnabled))
        // {
        //     settings.AiFallbackEnabled = aiFallbackEnabled;
        // }

        return settings;
    }

    private static bool TryReadBoolean(JsonElement? element, out bool value)
    {
        value = false;

        if (element == null)
        {
            return false;
        }

        if (element.Value.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.Value.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        if (element.Value.ValueKind == JsonValueKind.String)
        {
            return bool.TryParse(element.Value.GetString(), out value);
        }

        return false;
    }

    private class AppSettingsFile
    {
        public string? StorageMode { get; set; }

        public string? InterfaceMode { get; set; }

        public string? RecipesFilePath { get; set; }

        public string? UsersFilePath { get; set; }

        public string? DatabasePath { get; set; }

        public JsonElement? AuthenticationEnabled { get; set; }

        //public JsonElement? AiFallbackEnabled { get; set; }
    }
}
