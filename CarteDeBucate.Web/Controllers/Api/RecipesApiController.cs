using CarteDeBucate.Web.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers.Api;

[ApiController]
[Route("api/recipes")]
public class RecipesApiController : ControllerBase
{
    private readonly IRecipeLibraryService _recipeLibraryService;

    public RecipesApiController(IRecipeLibraryService recipeLibraryService)
    {
        _recipeLibraryService = recipeLibraryService;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateRecipeRequest request)
    {
        RecipeSaveResult result =
            _recipeLibraryService.SaveRecipe(request.ToRecipe());

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        if (result.Recipe == null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Recipe was saved, but the saved recipe could not be returned."
                });
        }

        return Created(
            $"/Recipes/Details/{result.Recipe.Id}",
            new
            {
                message = result.Message,
                recipeId = result.Recipe.Id
            });
    }
}