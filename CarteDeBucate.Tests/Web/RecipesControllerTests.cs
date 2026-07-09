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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeSummariesPageToReturn =
                new PagedResult<RecipeSummary>(recipes, 2, 10, 12)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index(null, pageNumber: 2);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Equal(recipes, model.Recipes);
        Assert.Equal("", model.SearchText);
        Assert.False(model.IsSearch);
        Assert.Equal(2, model.PageNumber);
        Assert.Equal(10, model.PageSize);
        Assert.Equal(12, model.TotalItems);
        Assert.Equal(2, model.TotalPages);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
        Assert.True(recipeService.GetRecipeSummariesPageWasCalled);
        Assert.Equal(2, recipeService.PageNumberPassedToGetRecipeSummariesPage);
        Assert.Equal(10, recipeService.PageSizePassedToGetRecipeSummariesPage);
        Assert.False(recipeService.GetRecipeSummariesWasCalled);
        Assert.False(recipeService.SearchRecipesWasCalled);
    }

    [Fact]
    public void Index_WithSearchText_ShouldReturnSearchResultsAndTerm()
    {
        List<RecipeSummary> searchResults = new List<RecipeSummary>
        {
            new RecipeSummary { Id = 2, Name = "Supa" }
        };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SearchResultsPageToReturn =
                new PagedResult<RecipeSummary>(searchResults, 2, 10, 12)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index("supa", pageNumber: 2);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Equal(searchResults, model.Recipes);
        Assert.Equal("supa", model.SearchText);
        Assert.True(model.IsSearch);
        Assert.Equal(2, model.PageNumber);
        Assert.Equal(10, model.PageSize);
        Assert.Equal(12, model.TotalItems);
        Assert.Equal(2, model.TotalPages);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
        Assert.True(recipeService.SearchRecipesPageWasCalled);
        Assert.Equal("supa", recipeService.SearchTextPassedToSearchRecipesPage);
        Assert.Equal(2, recipeService.PageNumberPassedToSearchRecipesPage);
        Assert.Equal(10, recipeService.PageSizePassedToSearchRecipesPage);
        Assert.False(recipeService.SearchRecipesWasCalled);
        Assert.False(recipeService.GetRecipeSummariesWasCalled);
        Assert.False(recipeService.GetRecipeSummariesPageWasCalled);
    }

    [Fact]
    public void Index_WithWhitespaceSearchText_ShouldReturnRecipeSummaries()
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>
        {
            new RecipeSummary { Id = 3, Name = "Recipe" }
        };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeSummariesPageToReturn =
                new PagedResult<RecipeSummary>(recipes, 1, 10, recipes.Count)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index("   ");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Equal(recipes, model.Recipes);
        Assert.False(model.IsSearch);
        Assert.True(recipeService.GetRecipeSummariesPageWasCalled);
        Assert.False(recipeService.GetRecipeSummariesWasCalled);
        Assert.False(recipeService.SearchRecipesWasCalled);
        Assert.False(recipeService.SearchRecipesPageWasCalled);
    }

    [Fact]
    public void Index_WithInvalidPageNumber_ShouldReturnNormalizedPageNumber()
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>
        {
            new RecipeSummary { Id = 4, Name = "Recipe" }
        };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeSummariesPageToReturn =
                new PagedResult<RecipeSummary>(recipes, 0, 10, recipes.Count)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index(null, pageNumber: 0);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Equal(1, model.PageNumber);
        Assert.True(recipeService.GetRecipeSummariesPageWasCalled);
        Assert.Equal(0, recipeService.PageNumberPassedToGetRecipeSummariesPage);
        Assert.Equal(10, recipeService.PageSizePassedToGetRecipeSummariesPage);
    }

    [Fact]
    public void Index_WhenPageNumberIsAfterLastPage_ShouldRedirectToLastPage()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeSummariesPageToReturn =
                new PagedResult<RecipeSummary>(
                    new List<RecipeSummary>(),
                    3,
                    10,
                    11)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index(null, pageNumber: 3);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal(2, redirectResult.RouteValues?["pageNumber"]);
    }

    [Fact]
    public void Index_WhenSearchPageNumberIsAfterLastPage_ShouldRedirectToLastSearchPage()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SearchResultsPageToReturn =
                new PagedResult<RecipeSummary>(
                    new List<RecipeSummary>(),
                    3,
                    10,
                    11)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index("supa", pageNumber: 3);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal(2, redirectResult.RouteValues?["pageNumber"]);
        Assert.Equal("supa", redirectResult.RouteValues?["searchText"]);
    }

    [Fact]
    public void Index_WhenSearchHasNoResults_ShouldReturnEmptySearchModel()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            SearchResultsPageToReturn =
                new PagedResult<RecipeSummary>(new List<RecipeSummary>(), 1, 10, 0)
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Index("inexistent");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        RecipeIndexViewModel model =
            Assert.IsType<RecipeIndexViewModel>(viewResult.Model);
        Assert.Empty(model.Recipes);
        Assert.Equal("inexistent", model.SearchText);
        Assert.True(model.IsSearch);
        Assert.Equal(0, model.TotalItems);
        Assert.Equal(0, model.TotalPages);
        Assert.True(recipeService.SearchRecipesPageWasCalled);
        Assert.False(recipeService.SearchRecipesWasCalled);
    }

    [Fact]
    public void Details_WhenRecipeDoesNotExist_ShouldReturnFriendlyNotFoundView()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Details(17);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
        Assert.True(recipeService.GetRecipeByIdWasCalled);
        Assert.Equal(17, recipeService.RecipeIdPassedToGetRecipeById);
    }

    [Fact]
    public void Details_WithPageNumber_ShouldExposePageNumberToView()
    {
        Recipe recipe = new Recipe { Id = 17, Name = "Recipe" };
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = recipe
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Details(17, pageNumber: 3, searchText: "supa");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(recipe, viewResult.Model);
        Assert.Equal(3, controller.ViewData["PageNumber"]);
        Assert.Equal("supa", controller.ViewData["SearchText"]);
    }

    [Fact]
    public void Edit_WhenRecipeDoesNotExist_ShouldReturnFriendlyNotFoundView()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Edit(18);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
    }

    [Fact]
    public void Delete_WhenRecipeDoesNotExist_ShouldReturnFriendlyNotFoundView()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Delete(19);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
    }

    [Fact]
    public void Delete_WhenOpenedFromIndex_ShouldCancelBackToIndex()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Delete(17);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<Recipe>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Details),
            controller.ViewData["DeleteCancelAction"]);
    }

    [Fact]
    public void Delete_WithPageNumber_ShouldExposePageNumberToView()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Delete(17, pageNumber: 3);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<Recipe>(viewResult.Model);
        Assert.Equal(3, controller.ViewData["PageNumber"]);
    }

    [Fact]
    public void Delete_WhenOpenedFromIndexWithPageNumber_ShouldKeepIndexAndPageNumber()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Delete(
            17,
            returnTo: "Index",
            pageNumber: 3,
            searchText: "supa");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<Recipe>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Index),
            controller.ViewData["DeleteCancelAction"]);
        Assert.Equal(3, controller.ViewData["PageNumber"]);
        Assert.Equal("supa", controller.ViewData["SearchText"]);
    }

    [Fact]
    public void Edit_WhenOpenedFromIndex_ShouldCancelBackToIndex()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Edit(17, "Index");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RecipeFormViewModel>(viewResult.Model);
        Assert.Equal(
            nameof(RecipesController.Index),
            controller.ViewData["EditCancelAction"]);
    }

    [Fact]
    public void Edit_WithPageNumber_ShouldExposePageNumberToView()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.Edit(17, pageNumber: 3, searchText: "supa");

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RecipeFormViewModel>(viewResult.Model);
        Assert.Equal(3, controller.ViewData["PageNumber"]);
        Assert.Equal("supa", controller.ViewData["SearchText"]);
    }

    [Fact]
    public void Edit_WhenOpenedWithoutReturnToIndex_ShouldCancelBackToDetails()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 17, Name = "Recipe" }
        };
        RecipesController controller = CreateController(recipeService);

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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService();
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
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
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
    public void EditPost_WhenUpdateSucceeds_ShouldRedirectToDetailsWithPageNumber()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            RecipeToReturn = new Recipe { Id = 23, Name = "Recipe" },
            UpdateResultToReturn = RecipeSaveResult.Success("Updated.")
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();
        model.Id = 23;

        IActionResult result = controller.Edit(23, model, pageNumber: 3, searchText: "supa");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Details), redirectResult.ActionName);
        Assert.Equal(23, redirectResult.RouteValues?["id"]);
        Assert.Equal(3, redirectResult.RouteValues?["pageNumber"]);
        Assert.Equal("supa", redirectResult.RouteValues?["searchText"]);
    }

    [Fact]
    public void EditPost_WhenOpenedFromIndexAndUpdateSucceeds_ShouldRedirectToIndexWithPageNumber()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            UpdateResultToReturn = RecipeSaveResult.Success("Updated.")
        };
        RecipesController controller = CreateController(recipeService);
        RecipeFormViewModel model = CreateValidModel();
        model.Id = 23;

        IActionResult result = controller.Edit(
            23,
            model,
            returnTo: "Index",
            pageNumber: 3,
            searchText: "supa");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal(3, redirectResult.RouteValues?["pageNumber"]);
        Assert.Equal("supa", redirectResult.RouteValues?["searchText"]);
    }

    [Fact]
    public void DeletePost_WhenDeleteSucceeds_ShouldRedirectToIndex()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
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
    public void DeletePost_WhenOpenedFromIndex_ShouldRedirectToIndexWithPageNumber()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
        {
            DeleteResultToReturn = RecipeSaveResult.Success("Deleted.")
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = controller.DeleteConfirmed(
            31,
            returnTo: "Index",
            pageNumber: 3,
            searchText: "supa");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Index), redirectResult.ActionName);
        Assert.Equal(3, redirectResult.RouteValues?["pageNumber"]);
        Assert.Equal("supa", redirectResult.RouteValues?["searchText"]);
    }

    [Fact]
    public void EditPost_WhenUpdateFails_ShouldNotSetSuccessMessage()
    {
        FakeRecipeLibraryService recipeService = new FakeRecipeLibraryService
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
        FakeRecipeImporterService importerService = new FakeRecipeImporterService
        {
            ImportResultToReturn = new RecipeImportResult
            {
                Success = true,
                Recipe = importedRecipe,
                Message = "Imported."
            }
        };
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Success("Imported.", importedRecipe)
        };
        RecipesController controller = CreateController(libraryService, importerService);

        IActionResult result =
            await controller.Import("https://example.com/imported-recipe");

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipesController.Details), redirectResult.ActionName);
        Assert.Equal(52, redirectResult.RouteValues?["id"]);
        Assert.True(importerService.ImportRecipeFromUrlAsyncWasCalled);
        Assert.True(libraryService.SaveRecipeWasCalled);
        Assert.Equal(importedRecipe, libraryService.RecipePassedToSaveRecipe);
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
            ImportResultToReturn = new RecipeImportResult
            {
                Success = false,
                Message = "Import failed."
            }
        };
        RecipesController controller = CreateController(recipeService);

        IActionResult result = await controller.Import(url);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(url, viewResult.Model);
        Assert.True(recipeService.ImportRecipeFromUrlAsyncWasCalled);
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
            ImportResultToReturn = new RecipeImportResult
            {
                Success = true,
                Recipe = new Recipe { Name = "Incomplete recipe" },
                Message = "Imported."
            }
        };
        FakeRecipeLibraryService libraryService = new FakeRecipeLibraryService
        {
            SaveResultToReturn = RecipeSaveResult.Fail(
                "Ingredientele sunt obligatorii.\nPșii sunt obligatorii.")
        };
        RecipesController controller = CreateController(libraryService, recipeService);

        IActionResult result = await controller.Import(url);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(url, viewResult.Model);
        Assert.Contains(
            controller.ModelState["url"]!.Errors,
            error => error.ErrorMessage ==
                "Nu am putut extrage ingredientele sau pașii din această pagină. Te rugăm să adaugi rețeta manual.");
        Assert.True(controller.ViewData["ShowManualAddRecipeLink"] is true);
        Assert.True(libraryService.SaveRecipeWasCalled);
    }

    [Fact]
    public async Task ImportPost_WhenSuccessfulResultHasNoRecipe_ShouldRedirectToIndex()
    {
        FakeRecipeImporterService recipeService = new FakeRecipeImporterService
        {
            ImportResultToReturn = new RecipeImportResult
            {
                Success = true,
                Message = "Imported."
            }
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
        IRecipeLibraryService recipeLibraryService)
    {
        return CreateController(
            recipeLibraryService,
            new FakeRecipeImporterService());
    }

    private static RecipesController CreateController(
        IRecipeImporterService recipeImporterService)
    {
        return CreateController(
            new FakeRecipeLibraryService(),
            recipeImporterService);
    }

    private static RecipesController CreateController(
        IRecipeLibraryService recipeLibraryService,
        IRecipeImporterService recipeImporterService)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        RecipesController controller = new RecipesController(
            recipeLibraryService,
            recipeImporterService)
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
