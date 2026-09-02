using System.Text.Json;

public class RecipeBackupDocumentReaderTests
{
    [Fact]
    public void DetectVersion_WithVersionTwoDocument_ShouldReturnCurrentVersion()
    {
        const string json = """
            {
              "version": 2,
              "exportedAtUtc": "2026-09-02T12:00:00Z",
              "recipes": []
            }
            """;

        int version = RecipeBackupDocumentReader.DetectVersion(json);

        Assert.Equal(RecipeBackupVersions.Current, version);
    }

    [Fact]
    public void DetectVersion_WithLegacyArray_ShouldReturnLegacyVersion()
    {
        const string json = "[]";

        int version = RecipeBackupDocumentReader.DetectVersion(json);

        Assert.Equal(RecipeBackupVersions.LegacyJsonArray, version);
    }

    [Fact]
    public void DetectVersion_WithInvalidJson_ShouldThrowJsonException()
    {
        Assert.ThrowsAny<JsonException>(() =>
            RecipeBackupDocumentReader.DetectVersion("not valid json"));
    }

    [Fact]
    public void DetectVersion_WithScalarRoot_ShouldThrowJsonException()
    {
        Assert.Throws<JsonException>(() =>
            RecipeBackupDocumentReader.DetectVersion("42"));
    }

    [Fact]
    public void ReadCurrentDocument_WithoutFormatProperty_ShouldReturnDocument()
    {
        const string json = """
            {
              "version": 2,
              "exportedAtUtc": "2026-09-02T12:00:00Z",
              "recipes": []
            }
            """;

        RecipeBackupDocument document =
            RecipeBackupDocumentReader.ReadCurrentDocument(json);

        Assert.Equal(RecipeBackupVersions.Current, document.Version);
        Assert.Equal(DateTimeKind.Utc, document.ExportedAtUtc.Kind);
        Assert.Empty(document.Recipes);
    }

    [Theory]
    [InlineData("""{"exportedAtUtc":"2026-09-02T12:00:00Z","recipes":[]}""")]
    [InlineData("""{"version":3,"exportedAtUtc":"2026-09-02T12:00:00Z","recipes":[]}""")]
    [InlineData("""{"version":1,"exportedAtUtc":"2026-09-02T12:00:00Z","recipes":[]}""")]
    [InlineData("""{"version":0,"exportedAtUtc":"2026-09-02T12:00:00Z","recipes":[]}""")]
    [InlineData("""{"version":-1,"exportedAtUtc":"2026-09-02T12:00:00Z","recipes":[]}""")]
    public void ReadCurrentDocument_WithUnsupportedVersion_ShouldThrowJsonException(string json)
    {
        Assert.Throws<JsonException>(() =>
            RecipeBackupDocumentReader.ReadCurrentDocument(json));
    }

    [Theory]
    [InlineData("""{"version":2,"recipes":[]}""")]
    [InlineData("""{"version":2,"exportedAtUtc":"invalid","recipes":[]}""")]
    public void ReadCurrentDocument_WithInvalidExportDate_ShouldThrowJsonException(string json)
    {
        Assert.Throws<JsonException>(() =>
            RecipeBackupDocumentReader.ReadCurrentDocument(json));
    }

    [Theory]
    [InlineData("""{"version":2,"exportedAtUtc":"2026-09-02T12:00:00Z"}""")]
    [InlineData("""{"version":2,"exportedAtUtc":"2026-09-02T12:00:00Z","recipes":null}""")]
    public void ReadCurrentDocument_WithInvalidRecipes_ShouldThrowJsonException(string json)
    {
        Assert.Throws<JsonException>(() =>
            RecipeBackupDocumentReader.ReadCurrentDocument(json));
    }
}
