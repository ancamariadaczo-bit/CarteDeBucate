namespace CarteDeBucate.Web.Models;

public class RecipeDetailsViewModel
{
    public Recipe Recipe { get; set; } = new();

    public IReadOnlyList<RecipePhotoInfo> Photos { get; set; } =
        Array.Empty<RecipePhotoInfo>();
}
