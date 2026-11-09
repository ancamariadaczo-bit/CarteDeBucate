namespace CarteDeBucate.Web.Models;

public class RecipeIndexItemViewModel
{
    public RecipeSummary Recipe { get; set; } = new();

    public int? PrimaryPhotoId { get; set; }
}
