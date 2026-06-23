public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public List<string> Ingredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    public string Notes { get; set; } = "";
    public RecipeStatus Status { get; set; } = RecipeStatus.Saved;
    public int? UserId { get; set; }
}