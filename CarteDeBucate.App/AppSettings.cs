public static class AppSettings
{
    public const string JsonFilePath = "recipes.json";

    public const string DatabasePath = "recipes.db";

    // Choose where recipes should be read from and saved to.
    // For JSON storage:
    //public const StorageMode CurrentStorageMode = StorageMode.Json;
    // For database storage:
    public const StorageMode CurrentStorageMode = StorageMode.Database;
}