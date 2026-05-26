public static class AppSettings
{
    public const string JsonFilePath = "recipes.json";

    public const string DatabasePath = "recipes.db";

    // Choose where recipes should be read from and saved to.
    // For JSON storage:
    //public const StorageMode CurrentStorageMode = StorageMode.Json;
    // For database storage:
    public const StorageMode CurrentStorageMode = StorageMode.Database;

    // Choose the UI to render.
    // For a classic consule:
    //public const InterfaceMode CurrentInterfaceMode = InterfaceMode.ClassicConsole;
    // For a modern console:
    public const InterfaceMode CurrentInterfaceMode = InterfaceMode.RichConsole;
}