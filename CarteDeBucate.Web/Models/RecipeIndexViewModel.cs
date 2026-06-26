namespace CarteDeBucate.Web.Models;

public class RecipeIndexViewModel
{
    public List<RecipeSummary> Recipes { get; set; } = new();

    public string SearchText { get; set; } = "";

    public bool IsSearch => !string.IsNullOrWhiteSpace(SearchText);
}
