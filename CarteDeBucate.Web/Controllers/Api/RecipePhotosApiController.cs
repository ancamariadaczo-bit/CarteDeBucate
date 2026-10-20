using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers.Api;

//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/recipes/{recipeId:int}/photos")]
public class RecipePhotosApiController : ControllerBase
{
    private readonly IRecipePhotoService _recipePhotoService;
    private readonly IRecipePhotoReconciliationService _recipePhotoReconciliationService;

    public RecipePhotosApiController(
        IRecipePhotoService recipePhotoService,
        IRecipePhotoReconciliationService recipePhotoReconciliationService
    )
    {
        _recipePhotoService = recipePhotoService;
        _recipePhotoReconciliationService = recipePhotoReconciliationService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddPhoto(
        int recipeId,
        [FromForm] IFormFile? photo,
        CancellationToken cancellationToken
    )
    {
        if (photo is null)
        {
            return BadRequest(new { code = "MissingPhoto", message = "Photo file is required." });
        }

        using Stream content = photo.OpenReadStream();

        RecipePhotoResult result = await _recipePhotoService.AddPhotoAsync(
            recipeId,
            content,
            photo.FileName,
            photo.ContentType,
            photo.Length,
            RecipePhotoOrigin.UserUpload,
            cancellationToken
        );

        if (result.IsSuccess)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    code = result.Code.ToString(),
                    message = result.Message,
                    photo = result.Photo,
                }
            );
        }

        object errorResponse = new { code = result.Code.ToString(), message = result.Message };

        return result.Code switch
        {
            RecipePhotoResultCode.InvalidRecipeId
            or RecipePhotoResultCode.EmptyContent
            or RecipePhotoResultCode.InvalidContent => BadRequest(errorResponse),

            RecipePhotoResultCode.RecipeNotFound => NotFound(errorResponse),

            RecipePhotoResultCode.PhotoLimitReached => Conflict(errorResponse),

            RecipePhotoResultCode.FileTooLarge => StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                errorResponse
            ),

            RecipePhotoResultCode.UnsupportedFormat => StatusCode(
                StatusCodes.Status415UnsupportedMediaType,
                errorResponse
            ),

            RecipePhotoResultCode.StorageFailure or RecipePhotoResultCode.PersistenceFailure =>
                StatusCode(StatusCodes.Status500InternalServerError, errorResponse),

            _ => StatusCode(StatusCodes.Status500InternalServerError, errorResponse),
        };
    }

    [HttpDelete("~/api/photos/{photoId:int}")]
    public IActionResult DeletePhoto(int photoId)
    {
        RecipePhotoResult result = _recipePhotoService.DeletePhoto(photoId);

        object response = new { code = result.Code.ToString(), message = result.Message };

        if (result.IsSuccess)
        {
            return Ok(response);
        }

        return result.Code switch
        {
            RecipePhotoResultCode.PhotoNotFound => NotFound(response),

            RecipePhotoResultCode.StorageFailure or RecipePhotoResultCode.PersistenceFailure =>
                StatusCode(StatusCodes.Status500InternalServerError, response),

            _ => StatusCode(StatusCodes.Status500InternalServerError, response),
        };
    }

    [HttpPost("~/api/photos/reconcile")]
    public IActionResult ReconcilePhotos()
    {
        RecipePhotoReconciliationReport report = _recipePhotoReconciliationService.Reconcile();

        return Ok(report);
    }
}
