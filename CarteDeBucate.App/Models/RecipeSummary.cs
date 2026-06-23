public class RecipeSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public RecipeStatus Status { get; set; } = RecipeStatus.Saved;
    public int? UserId { get; set; }
}
