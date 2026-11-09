namespace CarteDeBucate.Web.Models;

public class RecipePrintViewModel
{
    public Recipe Recipe { get; set; } = new();

    public int? PrimaryPhotoId { get; set; }
}
