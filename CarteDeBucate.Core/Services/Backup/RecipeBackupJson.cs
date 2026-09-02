using System.Text.Json;

internal static class RecipeBackupJson
{
    public const string VersionPropertyName = "version";
    public const string ExportedAtUtcPropertyName = "exportedAtUtc";
    public const string RecipesPropertyName = "recipes";

    public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
