using Microsoft.AspNetCore.Http;

namespace CarteDeBucate.Web.Models;

public class RecipePhotoUploadViewModel
{
    public int RecipeId { get; set; }

    public IFormFile? Photo { get; set; }
}
