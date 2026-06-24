using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Mvc;

public class RecipesControllerTests
{
    [Fact]
    public void Index_ShouldReturnViewWithRecipeSummaries()
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>
        {
            new RecipeSummary { Id = 1, Name = "Recipe" }
        };
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            RecipeSummariesToReturn = recipes
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Index();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(recipes, viewResult.Model);
        Assert.True(recipeService.GetRecipeSummariesWasCalled);
    }

    [Fact]
    public void Details_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService();
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Details(17);

        Assert.IsType<NotFoundResult>(result);
        Assert.True(recipeService.GetRecipeByIdWasCalled);
        Assert.Equal(17, recipeService.RecipeIdPassedToGetRecipeById);
    }

    [Fact]
    public void CreatePost_WhenSaveSucceeds_ShouldRedirectToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            SaveResultToReturn = RecipeSaveResult.Success("Saved.")
        };
        RecipesController controller = new RecipesController(recipeService);
        RecipeFormViewModel model = CreateValidModel();

        IActionResult result = controller.Create(model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.True(recipeService.SaveRecipeWasCalled);
    }

    [Fact]
    public void EditPost_WhenUpdateSucceeds_ShouldKeepModelId()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            UpdateResultToReturn = RecipeSaveResult.Success("Updated.")
        };
        RecipesController controller = new RecipesController(recipeService);
        RecipeFormViewModel model = CreateValidModel();
        model.Id = 23;

        IActionResult result = controller.Edit(23, model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Details), redirectResult.ActionName);
        Assert.Equal(23, redirectResult.RouteValues?["id"]);
        Assert.True(recipeService.UpdateRecipeWasCalled);
        Assert.Equal(23, recipeService.RecipePassedToUpdateRecipe?.Id);
    }

    [Fact]
    public void DeletePost_WhenDeleteSucceeds_ShouldRedirectToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            DeleteResultToReturn = RecipeSaveResult.Success("Deleted.")
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.DeleteConfirmed(31);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.True(recipeService.DeleteRecipeWasCalled);
        Assert.Equal(31, recipeService.RecipeIdPassedToDeleteRecipe);
    }

    private static RecipeFormViewModel CreateValidModel()
    {
        return new RecipeFormViewModel
        {
            Name = "Test recipe",
            SourceUrl = "https://example.com/recipe",
            IngredientsText = "Ingredient",
            StepsText = "Step",
            SavedAt = DateTime.Now
        };
    }
}
