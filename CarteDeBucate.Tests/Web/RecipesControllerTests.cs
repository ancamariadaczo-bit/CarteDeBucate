using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class RecipesControllerTests
{
    [Fact]
    public void Index_WithoutSearchText_ShouldReturnRecipeSummaries()
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

        IActionResult result = controller.Index(null);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Same(recipes, model.Recipes);
        Assert.Equal("", model.SearchText);
        Assert.False(model.IsSearch);
        Assert.True(recipeService.GetRecipeSummariesWasCalled);
        Assert.False(recipeService.SearchRecipesWasCalled);
    }

    [Fact]
    public void Index_WithSearchText_ShouldReturnSearchResultsAndTerm()
    {
        List<RecipeSummary> searchResults = new List<RecipeSummary>
        {
            new RecipeSummary { Id = 2, Name = "Supa" }
        };
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            SearchResultsToReturn = searchResults
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Index("supa");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Same(searchResults, model.Recipes);
        Assert.Equal("supa", model.SearchText);
        Assert.True(model.IsSearch);
        Assert.True(recipeService.SearchRecipesWasCalled);
        Assert.Equal("supa", recipeService.SearchTextPassedToSearchRecipes);
        Assert.False(recipeService.GetRecipeSummariesWasCalled);
    }

    [Fact]
    public void Index_WithWhitespaceSearchText_ShouldReturnRecipeSummaries()
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>
        {
            new RecipeSummary { Id = 3, Name = "Recipe" }
        };
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            RecipeSummariesToReturn = recipes
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Index("   ");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Same(recipes, model.Recipes);
        Assert.False(model.IsSearch);
        Assert.True(recipeService.GetRecipeSummariesWasCalled);
        Assert.False(recipeService.SearchRecipesWasCalled);
    }

    [Fact]
    public void Index_WhenSearchHasNoResults_ShouldReturnEmptySearchModel()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService();
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Index("inexistent");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Empty(model.Recipes);
        Assert.Equal("inexistent", model.SearchText);
        Assert.True(model.IsSearch);
    }

    [Fact]
    public void Details_WhenRecipeDoesNotExist_ShouldReturnFriendlyNotFoundView()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService();
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Details(17);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
        Assert.True(recipeService.GetRecipeByIdWasCalled);
        Assert.Equal(17, recipeService.RecipeIdPassedToGetRecipeById);
    }

    [Fact]
    public void Edit_WhenRecipeDoesNotExist_ShouldReturnFriendlyNotFoundView()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService();
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Edit(18);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
    }

    [Fact]
    public void Delete_WhenRecipeDoesNotExist_ShouldReturnFriendlyNotFoundView()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService();
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Delete(19);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
    }

    [Fact]
    public void Delete_WhenOpenedFromIndex_ShouldCancelBackToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Delete(17, "Index");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<Recipe>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Index),
            controller.ViewData["DeleteCancelAction"]);
    }

    [Fact]
    public void Delete_WhenOpenedWithoutReturnToIndex_ShouldCancelBackToDetails()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Delete(17);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<Recipe>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Details),
            controller.ViewData["DeleteCancelAction"]);
    }

    [Fact]
    public void Edit_WhenOpenedFromIndex_ShouldCancelBackToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Edit(17, "Index");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RecipeFormViewModel>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Index),
            controller.ViewData["EditCancelAction"]);
    }

    [Fact]
    public void Edit_WhenOpenedWithoutReturnToIndex_ShouldCancelBackToDetails()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = new RecipesController(recipeService);

        IActionResult result = controller.Edit(17);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RecipeFormViewModel>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Details),
            controller.ViewData["EditCancelAction"]);
    }

    [Fact]
    public void CreatePost_WhenSaveSucceeds_ShouldRedirectToSavedRecipeDetails()
    {
        Recipe savedRecipe = new Recipe { Id = 41 };
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            SaveResultToReturn = RecipeSaveResult.Success("Saved.", savedRecipe)
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();

        IActionResult result = controller.Create(model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Details), redirectResult.ActionName);
        Assert.Equal(41, redirectResult.RouteValues?["id"]);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal("Saved.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void CreatePost_WhenSuccessfulResultHasNoRecipe_ShouldRedirectToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            SaveResultToReturn = RecipeSaveResult.Success("Saved.")
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();

        IActionResult result = controller.Create(model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Equal(
            "Rețeta a fost salvată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.",
            controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void CreatePost_WhenSaveFails_ShouldKeepBusinessErrorAndEnteredValues()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            SaveResultToReturn = RecipeSaveResult.Fail("Reteta exista deja.")
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();

        IActionResult result = controller.Create(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.True(recipeService.SaveRecipeWasCalled);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == "Reteta exista deja.");
    }

    [Fact]
    public void CreatePost_WhenModelStateIsInvalid_ShouldNotCallService()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService();
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = new RecipeFormViewModel();
        controller.ModelState.AddModelError(
            nameof(RecipeFormViewModel.Name),
            "Numele este obligatoriu.");

        IActionResult result = controller.Create(model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(recipeService.SaveRecipeWasCalled);
    }

    [Fact]
    public void EditPost_WhenUpdateSucceeds_ShouldKeepModelId()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            UpdateResultToReturn = RecipeSaveResult.Success("Updated.")
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();
        model.Id = 23;

        IActionResult result = controller.Edit(23, model);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Details), redirectResult.ActionName);
        Assert.Equal(23, redirectResult.RouteValues?["id"]);
        Assert.True(recipeService.UpdateRecipeWasCalled);
        Assert.Equal(23, recipeService.RecipePassedToUpdateRecipe?.Id);
        Assert.Equal("Updated.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void DeletePost_WhenDeleteSucceeds_ShouldRedirectToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            DeleteResultToReturn = RecipeSaveResult.Success("Deleted.")
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.DeleteConfirmed(31);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.True(recipeService.DeleteRecipeWasCalled);
        Assert.Equal(31, recipeService.RecipeIdPassedToDeleteRecipe);
        Assert.Equal("Deleted.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void EditPost_WhenUpdateFails_ShouldNotSetSuccessMessage()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            UpdateResultToReturn = RecipeSaveResult.Fail("Update failed.")
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();
        model.Id = 23;

        IActionResult result = controller.Edit(23, model);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == "Update failed.");
        Assert.False(controller.TempData.ContainsKey("SuccessMessage"));
    }

    [Fact]
    public async Task ImportPost_WhenImportSucceeds_ShouldRedirectToRecipeDetails()
    {
        Recipe importedRecipe = new Recipe { Id = 52 };
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            ImportAndSaveResultToReturn =
                RecipeSaveResult.Success("Imported.", importedRecipe)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result =
            await controller.Import("https://example.com/imported-recipe");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Details), redirectResult.ActionName);
        Assert.Equal(52, redirectResult.RouteValues?["id"]);
        Assert.True(recipeService.ImportFromUrlAndSaveAsyncWasCalled);
        Assert.Equal(
            "Rețeta a fost importată. Verifică informațiile și editează-le dacă este nevoie.",
            controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task ImportPost_WhenImportFails_ShouldReturnViewWithEnteredUrlAndError()
    {
        const string url = "https://example.com/failed-import";
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            ImportAndSaveResultToReturn =
                RecipeSaveResult.Fail("Import failed.")
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = await controller.Import(url);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(url, viewResult.Model);
        Assert.True(recipeService.ImportFromUrlAndSaveAsyncWasCalled);
        Assert.True(controller.ModelState.ContainsKey("url"));
        Assert.Contains(
            controller.ModelState["url"]!.Errors,
            error => error.ErrorMessage == "Import failed.");
        Assert.False(controller.TempData.ContainsKey("SuccessMessage"));
    }

    [Fact]
    public async Task ImportPost_WhenImportedRecipeMissesDetails_ShouldShowManualAddMessage()
    {
        const string url = "https://example.com/incomplete-recipe";
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            ImportAndSaveResultToReturn = RecipeSaveResult.Fail(
                "Ingredientele sunt obligatorii.\nPșii sunt obligatorii.")
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = await controller.Import(url);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(url, viewResult.Model);
        Assert.Contains(
            controller.ModelState["url"]!.Errors,
            error => error.ErrorMessage ==
                "Nu am putut extrage ingredientele sau pașii din această pagină. Te rugăm să adaugi rețeta manual.");
        Assert.True(controller.ViewData["ShowManualAddRecipeLink"] is true);
    }

    [Fact]
    public async Task ImportPost_WhenSuccessfulResultHasNoRecipe_ShouldRedirectToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            ImportAndSaveResultToReturn = RecipeSaveResult.Success("Imported.")
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result =
            await controller.Import("https://example.com/imported-recipe");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal(
            "Rețeta a fost importată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.",
            controller.TempData["SuccessMessage"]);
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

    private static RecipesController CreateController(
        IRecipeImporterService recipeService)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        RecipesController controller = new RecipesController(recipeService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new TestTempDataProvider())
        };

        return controller;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
