namespace CarteDeBucate.Web.Models;

public class RecipeFormViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string SourceUrl { get; set; } = "";

    public string IngredientsText { get; set; } = "";

    public string StepsText { get; set; } = "";

    public string? Notes { get; set; }

    public RecipeStatus Status { get; set; } = RecipeStatus.Saved;

    public DateTime SavedAt { get; set; } = DateTime.Now;

    public int? UserId { get; set; }

    public static RecipeFormViewModel FromRecipe(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return new RecipeFormViewModel
        {
            Id = recipe.Id,
            Name = recipe.Name,
            SourceUrl = recipe.SourceUrl,
            IngredientsText = string.Join(Environment.NewLine, recipe.Ingredients),
            StepsText = string.Join(Environment.NewLine, recipe.Steps),
            Notes = recipe.Notes,
            Status = recipe.Status,
            SavedAt = recipe.SavedAt,
            UserId = recipe.UserId
        };
    }

    public Recipe ToRecipe()
    {
        return new Recipe
        {
            Id = Id,
            Name = Name,
            SourceUrl = SourceUrl,
            Ingredients = SplitLines(IngredientsText),
            Steps = SplitLines(StepsText),
            Notes = Notes ?? "",
            Status = Status,
            SavedAt = SavedAt,
            UserId = UserId
        };
    }

    private static List<string> SplitLines(string text)
    {
        return (text ?? "")
            .Split(
                ["\r\n", "\r", "\n"],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
