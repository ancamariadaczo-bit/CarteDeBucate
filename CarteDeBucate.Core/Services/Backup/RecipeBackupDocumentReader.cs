using System.Text.Json;

public static class RecipeBackupDocumentReader
{
    public static int DetectVersion(string json)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(json);
        JsonElement root = jsonDocument.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return RecipeBackupVersions.LegacyJsonArray;
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(RecipeBackupJson.VersionPropertyName, out JsonElement versionElement)
            && versionElement.ValueKind == JsonValueKind.Number
            && versionElement.TryGetInt32(out int version)
            && version == RecipeBackupVersions.Current)
        {
            return version;
        }

        throw new JsonException("Unsupported recipe backup document.");
    }

    public static RecipeBackupDocument ReadCurrentDocument(string json)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(json);
        JsonElement root = jsonDocument.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(RecipeBackupJson.VersionPropertyName, out JsonElement versionElement)
            || versionElement.ValueKind != JsonValueKind.Number
            || !versionElement.TryGetInt32(out int version)
            || version != RecipeBackupVersions.Current)
        {
            throw new JsonException("Unsupported recipe backup version.");
        }

        if (!root.TryGetProperty(RecipeBackupJson.ExportedAtUtcPropertyName, out JsonElement exportedAtElement)
            || exportedAtElement.ValueKind != JsonValueKind.String
            || !exportedAtElement.TryGetDateTime(out _))
        {
            throw new JsonException("The backup export date is missing or invalid.");
        }

        if (!root.TryGetProperty(RecipeBackupJson.RecipesPropertyName, out JsonElement recipesElement)
            || recipesElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The backup recipes are missing or invalid.");
        }

        RecipeBackupDocument? backupDocument = JsonSerializer.Deserialize<RecipeBackupDocument>(
            json,
            RecipeBackupJson.SerializerOptions);

        if (backupDocument?.Recipes == null)
        {
            throw new JsonException("The backup document could not be read.");
        }

        return backupDocument;
    }
}
