public class RecipeImportResult
{
    public bool Success { get; set; }
    public Recipe? Recipe { get; set; }
    public string? ImageUrl { get; set; }
    public string Message { get; set; } = "";
}
