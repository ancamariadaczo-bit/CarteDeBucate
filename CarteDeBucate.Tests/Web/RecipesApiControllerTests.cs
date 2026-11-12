using System.Reflection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarteDeBucate.Web.Controllers.Api;
using CarteDeBucate.Web.Models;
using CarteDeBucate.Web.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class RecipesApiControllerTests
{
    [Fact]
    public void Exists_WhenRecipeExists_ShouldReturnTrue()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeExistsBySourceUrlResultToReturn = true
        };
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = controller.Exists("https://example.com/banana-bread");

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetResponseProperty<bool>(okResult.Value, "exists"));
        Assert.Equal(1, recipeService.RecipeExistsBySourceUrlCallCount);
        Assert.Equal(
            "https://example.com/banana-bread",
            recipeService.SourceUrlPassedToRecipeExists);
    }

    [Fact]
    public void Exists_WhenRecipeDoesNotExist_ShouldReturnFalse()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = controller.Exists("https://example.com/missing");

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.False(GetResponseProperty<bool>(okResult.Value, "exists"));
        Assert.Equal(1, recipeService.RecipeExistsBySourceUrlCallCount);
    }

    [Fact]
    public void Exists_WithExteriorWhitespace_ShouldPassNormalizedUrlToService()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = controller.Exists(
            "  https://example.com/banana-bread  ");

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, recipeService.RecipeExistsBySourceUrlCallCount);
        Assert.Equal(
            "https://example.com/banana-bread",
            recipeService.SourceUrlPassedToRecipeExists);
    }

    [Fact]
    public void Exists_WithMissingSourceUrl_ShouldReturnBadRequestWithoutCallingService()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = controller.Exists(null);

        BadRequestObjectResult badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(
            AppTexts.RecipeSourceUrlRequired,
            GetResponseProperty<string>(badRequestResult.Value, "message"));
        Assert.Equal(0, recipeService.RecipeExistsBySourceUrlCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Exists_WithEmptySourceUrl_ShouldReturnBadRequestWithoutCallingService(
        string sourceUrl)
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = controller.Exists(sourceUrl);

        BadRequestObjectResult badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(
            AppTexts.RecipeSourceUrlRequired,
            GetResponseProperty<string>(badRequestResult.Value, "message"));
        Assert.Equal(0, recipeService.RecipeExistsBySourceUrlCallCount);
    }

    [Fact]
    public void Exists_WithUnsupportedProtocol_ShouldReturnBadRequestWithoutCallingService()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = controller.Exists("ftp://example.com/banana-bread");

        BadRequestObjectResult badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(
            AppTexts.RecipeSourceUrlInvalid,
            GetResponseProperty<string>(badRequestResult.Value, "message"));
        Assert.Equal(0, recipeService.RecipeExistsBySourceUrlCallCount);
    }

    [Fact]
    public async Task Create_WhenSaveSucceeds_ShouldSaveMappedRecipeAndReturnCreatedResult()
    {
        Recipe savedRecipe = new Recipe
        {
            Id = 42,
            Name = "Banana bread"
        };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success(
                "Recipe saved successfully.",
                savedRecipe)
        };
        RecipesApiController controller = CreateController(recipeService);
        CreateRecipeRequest request = CreateRequest();

        IActionResult result = await controller.Create(request);

        CreatedResult createdResult = Assert.IsType<CreatedResult>(result);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal("/Recipes/Details/42", createdResult.Location);
        Assert.Equal(
            "Recipe saved successfully.",
            GetResponseProperty<string>(createdResult.Value, "message"));
        Assert.Equal(
            42,
            GetResponseProperty<int>(createdResult.Value, "recipeId"));
        object photo = GetResponseProperty<object>(createdResult.Value, "photo");
        Assert.Equal("NotProvided", GetResponseProperty<string>(photo, "status"));

        Recipe recipePassedToService =
            Assert.IsType<Recipe>(recipeService.RecipePassedToSaveRecipe);
        Assert.Equal(request.Name, recipePassedToService.Name);
        Assert.Equal(request.SourceUrl, recipePassedToService.SourceUrl);
        Assert.Equal(request.Ingredients, recipePassedToService.Ingredients);
        Assert.Equal(request.Steps, recipePassedToService.Steps);
    }

    [Fact]
    public async Task Create_WhenSaveFails_ShouldReturnBadRequestWithServiceMessage()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Fail("Recipe already exists.")
        };
        FakeRecipePhotoUrlImporter photoImporter =
            new FakeRecipePhotoUrlImporter();
        RecipesApiController controller = CreateController(
            recipeService,
            photoImporter);

        IActionResult result = await controller.Create(CreateRequest());

        BadRequestObjectResult badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal(
            "Recipe already exists.",
            GetResponseProperty<string>(badRequestResult.Value, "message"));
        Assert.False(photoImporter.ImportAsyncWasCalled);
    }

    [Fact]
    public async Task Create_WithImageUrl_ShouldReturnSavedPhotoResult()
    {
        Recipe savedRecipe = new Recipe { Id = 42, Name = "Banana bread" };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success(
                "Recipe saved successfully.",
                savedRecipe)
        };
        FakeRecipePhotoUrlImporter photoImporter =
            new FakeRecipePhotoUrlImporter();
        RecipesApiController controller = CreateController(
            recipeService,
            photoImporter);
        CreateRecipeRequest request = CreateRequest();
        request.ImageUrl = "https://example.com/banana-bread.jpg";

        IActionResult result = await controller.Create(request);

        CreatedResult createdResult = Assert.IsType<CreatedResult>(result);
        object photo = GetResponseProperty<object>(createdResult.Value, "photo");
        Assert.Equal("Saved", GetResponseProperty<string>(photo, "status"));
        Assert.True(photoImporter.ImportAsyncWasCalled);
        Assert.Equal(42, photoImporter.RecipeIdPassedToImport);
        Assert.Equal(request.ImageUrl, photoImporter.ImageUrlPassedToImport);
    }

    [Fact]
    public async Task Create_WhenPhotoImportFails_ShouldStillReturnCreated()
    {
        Recipe savedRecipe = new Recipe { Id = 42, Name = "Banana bread" };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success(
                "Recipe saved successfully.",
                savedRecipe)
        };
        FakeRecipePhotoUrlImporter photoImporter =
            new FakeRecipePhotoUrlImporter
            {
                ResultToReturn = RecipePhotoUrlImportResult.Failure(
                    RecipePhotoUrlImportResultCode.DownloadFailed,
                    "Photo download failed.")
            };
        RecipesApiController controller = CreateController(
            recipeService,
            photoImporter);
        CreateRecipeRequest request = CreateRequest();
        request.ImageUrl = "https://example.com/banana-bread.jpg";

        IActionResult result = await controller.Create(request);

        CreatedResult createdResult = Assert.IsType<CreatedResult>(result);
        object photo = GetResponseProperty<object>(createdResult.Value, "photo");
        Assert.Equal("Failed", GetResponseProperty<string>(photo, "status"));
        Assert.Equal(
            "Photo download failed.",
            GetResponseProperty<string>(photo, "message"));
    }

    [Fact]
    public async Task Create_WhenSaveSucceedsWithoutRecipe_ShouldReturnInternalServerError()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success(
                "Recipe saved successfully.")
        };
        RecipesApiController controller = CreateController(recipeService);

        IActionResult result = await controller.Create(CreateRequest());

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);
        Assert.Equal(
            "Recipe was saved, but the saved recipe could not be returned.",
            GetResponseProperty<string>(objectResult.Value, "message"));
    }

    [Fact]
    public async Task Create_WithInvalidSourceUrl_ShouldReturnBadRequest()
    {
        using AuthenticationApiWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                factory.CreateToken("42", "chef"));
        CreateRecipeRequest request = CreateRequest();
        request.SourceUrl = "javascript:alert(1)";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingOptionalSourceUrlAndSteps_ShouldNormalizeThem()
    {
        Recipe savedRecipe = new Recipe { Id = 42 };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success("Recipe saved successfully.", savedRecipe)
        };
        RecipesApiController controller = CreateController(recipeService);
        CreateRecipeRequest request = CreateRequest();
        request.SourceUrl = null;
        request.Steps = null;

        IActionResult result = await controller.Create(request);

        Assert.IsType<CreatedResult>(result);
        Recipe recipe = Assert.IsType<Recipe>(recipeService.RecipePassedToSaveRecipe);
        Assert.Equal(string.Empty, recipe.SourceUrl);
        Assert.Empty(recipe.Steps);
    }

    [Fact]
    public async Task Create_WithInvalidImageUrl_ShouldReturnBadRequest()
    {
        using AuthenticationApiWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                factory.CreateToken("42", "chef"));
        CreateRecipeRequest request = CreateRequest();
        request.ImageUrl = "file:///tmp/recipe.jpg";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/recipes",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CreateRecipeRequest CreateRequest()
    {
        return new CreateRecipeRequest
        {
            Name = "Banana bread",
            SourceUrl = "https://example.com/banana-bread",
            Ingredients = new List<string>
            {
                "3 bananas",
                "200 g flour"
            },
            Steps = new List<string>
            {
                "Mix the ingredients.",
                "Bake the bread."
            }
        };
    }

    private static RecipesApiController CreateController(
        IRecipeLibraryService recipeLibraryService,
        FakeRecipePhotoUrlImporter? photoUrlImporter = null)
    {
        return new RecipesApiController(
            recipeLibraryService,
            photoUrlImporter ?? new FakeRecipePhotoUrlImporter());
    }

    private static T GetResponseProperty<T>(object? response, string propertyName)
    {
        Assert.NotNull(response);

        PropertyInfo? property = response.GetType().GetProperty(propertyName);

        Assert.NotNull(property);

        return Assert.IsAssignableFrom<T>(property.GetValue(response));
    }
}
