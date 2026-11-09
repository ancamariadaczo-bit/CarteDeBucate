using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

[Authorize]
[Route("[controller]")]
public class RecipePhotosController : Controller
{
    private readonly IRecipePhotoService _recipePhotoService;

    public RecipePhotosController(IRecipePhotoService recipePhotoService)
    {
        _recipePhotoService = recipePhotoService;
    }

    [HttpGet("Content/{id:int}")]
    public IActionResult Content(int id)
    {
        RecipePhotoContentResult result =
            _recipePhotoService.GetPhotoContent(id);

        if (result.IsSuccess && result.Content is not null)
        {
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers.Pragma = "no-cache";

            return File(result.Content, result.ContentType);
        }

        return result.Code switch
        {
            RecipePhotoResultCode.PhotoNotFound
                or RecipePhotoResultCode.ContentFileMissing
                => NotFound(),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
