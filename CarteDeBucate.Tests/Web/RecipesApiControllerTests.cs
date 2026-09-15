using System.Reflection;
using CarteDeBucate.Web.Controllers.Api;
using CarteDeBucate.Web.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class RecipesApiControllerTests
{
    [Fact]
    public void Create_WhenSaveSucceeds_ShouldSaveMappedRecipeAndReturnCreatedResult()
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
        RecipesApiController controller = new RecipesApiController(recipeService);
        CreateRecipeRequest request = CreateRequest();

        IActionResult result = controller.Create(request);

        CreatedResult createdResult = Assert.IsType<CreatedResult>(result);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal("/Recipes/Details/42", createdResult.Location);
        Assert.Equal(
            "Recipe saved successfully.",
            GetResponseProperty<string>(createdResult.Value, "message"));
        Assert.Equal(
            42,
            GetResponseProperty<int>(createdResult.Value, "recipeId"));

        Recipe recipePassedToService =
            Assert.IsType<Recipe>(recipeService.RecipePassedToSaveRecipe);
        Assert.Equal(request.Name, recipePassedToService.Name);
        Assert.Equal(request.SourceUrl, recipePassedToService.SourceUrl);
        Assert.Equal(request.Ingredients, recipePassedToService.Ingredients);
        Assert.Equal(request.Steps, recipePassedToService.Steps);
    }

    [Fact]
    public void Create_WhenSaveFails_ShouldReturnBadRequestWithServiceMessage()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Fail("Recipe already exists.")
        };
        RecipesApiController controller = new RecipesApiController(recipeService);

        IActionResult result = controller.Create(CreateRequest());

        BadRequestObjectResult badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal(
            "Recipe already exists.",
            GetResponseProperty<string>(badRequestResult.Value, "message"));
    }

    [Fact]
    public void Create_WhenSaveSucceedsWithoutRecipe_ShouldReturnInternalServerError()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success(
                "Recipe saved successfully.")
        };
        RecipesApiController controller = new RecipesApiController(recipeService);

        IActionResult result = controller.Create(CreateRequest());

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);
        Assert.Equal(
            "Recipe was saved, but the saved recipe could not be returned.",
            GetResponseProperty<string>(objectResult.Value, "message"));
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

    private static T GetResponseProperty<T>(object? response, string propertyName)
    {
        Assert.NotNull(response);

        PropertyInfo? property = response.GetType().GetProperty(propertyName);

        Assert.NotNull(property);

        return Assert.IsType<T>(property.GetValue(response));
    }
}
