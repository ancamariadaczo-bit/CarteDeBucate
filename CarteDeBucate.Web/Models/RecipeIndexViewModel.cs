namespace CarteDeBucate.Web.Models;

public class RecipeIndexViewModel
{
    public List<RecipeSummary> Recipes { get; set; } = new();

    public string SearchText { get; set; } = "";

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    public bool IsSearch => !string.IsNullOrWhiteSpace(SearchText);
}
