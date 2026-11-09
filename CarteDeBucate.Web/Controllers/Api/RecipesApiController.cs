using CarteDeBucate.Web.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using CarteDeBucate.Web.Services.Photos;
using CarteDeBucate.Web.Models;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/recipes")]
public class RecipesApiController : ControllerBase
{
    private readonly IRecipeLibraryService _recipeLibraryService;
    private readonly IRecipePhotoUrlImporter _recipePhotoUrlImporter;

    public RecipesApiController(
        IRecipeLibraryService recipeLibraryService,
        IRecipePhotoUrlImporter recipePhotoUrlImporter)
    {
        _recipeLibraryService = recipeLibraryService;
        _recipePhotoUrlImporter = recipePhotoUrlImporter;
    }

    [HttpGet("exists")]
    public IActionResult Exists([FromQuery] string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return BadRequest(new
            {
                message = AppTexts.RecipeSourceUrlRequired
            });
        }

        string normalizedSourceUrl = sourceUrl.Trim();

        if (!UrlValidator.IsValidHttpUrl(normalizedSourceUrl))
        {
            return BadRequest(new
            {
                message = AppTexts.RecipeSourceUrlInvalid
            });
        }

        bool exists =
            _recipeLibraryService.RecipeExistsBySourceUrl(normalizedSourceUrl);

        return Ok(new
        {
            exists
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRecipeRequest request,
        CancellationToken cancellationToken = default)
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

        string photoStatus = "NotProvided";
        string photoMessage = "No recipe photo was provided.";

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            RecipePhotoUrlImportResult photoResult =
                await _recipePhotoUrlImporter.ImportAsync(
                    result.Recipe.Id,
                    request.ImageUrl,
                    cancellationToken);

            photoStatus = photoResult.IsSuccess ? "Saved" : "Failed";
            photoMessage = photoResult.Message;
        }

        return Created(
            $"/Recipes/Details/{result.Recipe.Id}",
            new
            {
                message = result.Message,
                recipeId = result.Recipe.Id,
                photo = new
                {
                    status = photoStatus,
                    message = photoMessage
                }
            });
    }
}
