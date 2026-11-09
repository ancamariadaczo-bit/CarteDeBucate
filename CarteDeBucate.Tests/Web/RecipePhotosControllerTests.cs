using CarteDeBucate.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class RecipePhotosControllerTests
{
    [Fact]
    public void Content_WhenPhotoExists_ShouldReturnPrivateInlineStream()
    {
        using MemoryStream content = new MemoryStream([1, 2, 3]);
        FakeRecipePhotoService photoService = new FakeRecipePhotoService
        {
            GetPhotoContentResultToReturn =
                RecipePhotoContentResult.Success(
                    content,
                    "image/jpeg",
                    "original.jpg",
                    "Photo loaded.")
        };
        RecipePhotosController controller = CreateController(photoService);

        IActionResult result = controller.Content(17);

        FileStreamResult fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Same(content, fileResult.FileStream);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Empty(fileResult.FileDownloadName);
        Assert.Equal(
            "private, no-store",
            controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(
            "no-cache",
            controller.Response.Headers.Pragma.ToString());
        Assert.True(photoService.GetPhotoContentWasCalled);
        Assert.Equal(17, photoService.PhotoIdPassedToGetContent);
    }

    [Fact]
    public void Content_WithInvalidId_ShouldReturnNotFound()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService
        {
            GetPhotoContentResultToReturn =
                RecipePhotoContentResult.Failure(
                    RecipePhotoResultCode.PhotoNotFound,
                    "Photo not found.")
        };
        RecipePhotosController controller = CreateController(photoService);

        IActionResult result = controller.Content(0);

        Assert.IsType<NotFoundResult>(result);
        Assert.True(photoService.GetPhotoContentWasCalled);
        Assert.Equal(0, photoService.PhotoIdPassedToGetContent);
    }

    [Fact]
    public void Content_WhenPhotoDoesNotBelongToUser_ShouldReturnNotFound()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService
        {
            GetPhotoContentResultToReturn =
                RecipePhotoContentResult.Failure(
                    RecipePhotoResultCode.PhotoNotFound,
                    "Photo not found.")
        };
        RecipePhotosController controller = CreateController(photoService);

        IActionResult result = controller.Content(17);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Content_WhenContentFileIsMissing_ShouldReturnNotFound()
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService
        {
            GetPhotoContentResultToReturn =
                RecipePhotoContentResult.Failure(
                    RecipePhotoResultCode.ContentFileMissing,
                    "Photo content is missing.")
        };
        RecipePhotosController controller = CreateController(photoService);

        IActionResult result = controller.Content(17);

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData(RecipePhotoResultCode.StorageFailure)]
    [InlineData(RecipePhotoResultCode.PersistenceFailure)]
    public void Content_WhenTechnicalFailureOccurs_ShouldReturnServerError(
        RecipePhotoResultCode resultCode)
    {
        FakeRecipePhotoService photoService = new FakeRecipePhotoService
        {
            GetPhotoContentResultToReturn =
                RecipePhotoContentResult.Failure(
                    resultCode,
                    "Photo could not be loaded.")
        };
        RecipePhotosController controller = CreateController(photoService);

        IActionResult result = controller.Content(17);

        StatusCodeResult statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusResult.StatusCode);
    }

    [Fact]
    public void Controller_ShouldRequireAuthorizationAndUseContentRoute()
    {
        Type controllerType = typeof(RecipePhotosController);

        AuthorizeAttribute? authorizeAttribute = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();
        RouteAttribute? routeAttribute = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .SingleOrDefault();
        HttpGetAttribute? httpGetAttribute = controllerType
            .GetMethod(
                nameof(RecipePhotosController.Content),
                [typeof(int)])?
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal("[controller]", routeAttribute?.Template);
        Assert.Equal("Content/{id:int}", httpGetAttribute?.Template);
    }

    private static RecipePhotosController CreateController(
        IRecipePhotoService photoService)
    {
        return new RecipePhotosController(photoService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
