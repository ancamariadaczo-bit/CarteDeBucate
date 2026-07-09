using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CarteDeBucate.Web.Filters;
using CarteDeBucate.Web.Models;

namespace CarteDeBucate.Web.Controllers;

[ServiceFilter(typeof(OptionalAuthenticationFilter))]
public class RecipesController : Controller
{
    private const int PageSize = 10;
    private const string SuccessMessageKey = "SuccessMessage";
    private const string DeleteCancelActionKey = "DeleteCancelAction";
    private const string EditCancelActionKey = "EditCancelAction";
    private const string PageNumberKey = "PageNumber";
    private const string SearchTextKey = "SearchText";
    private const string ReturnToIndexValue = "Index";
    private const string ShowManualAddRecipeLinkKey = "ShowManualAddRecipeLink";
    private const string ImportMissingRequiredDetailsMessage =
        "Nu am putut extrage ingredientele sau pașii din această pagină. Te rugăm să adaugi rețeta manual.";

    private readonly IRecipeLibraryService _recipeLibraryService;
    private readonly IRecipeImporterService _recipeImporterService;

    public RecipesController(
        IRecipeLibraryService recipeLibraryService,
        IRecipeImporterService recipeImporterService)
    {
        _recipeLibraryService = recipeLibraryService;
        _recipeImporterService = recipeImporterService;
    }

    public IActionResult Index(string? searchText, int pageNumber = 1)
    {
        List<RecipeSummary> recipes;
        PagedResult<RecipeSummary>? pagedResult = null;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            pagedResult = _recipeLibraryService.GetRecipeSummariesPage(pageNumber, PageSize);
            recipes = pagedResult.Items;
        }
        else
        {
            pagedResult = _recipeLibraryService.SearchRecipesPage(searchText, pageNumber, PageSize);
            recipes = pagedResult.Items;
        }

        if (pagedResult.TotalPages > 0 &&
            pagedResult.PageNumber > pagedResult.TotalPages)
        {
            return RedirectToAction(
                nameof(Index),
                new
                {
                    pageNumber = pagedResult.TotalPages,
                    searchText
                });
        }

        return View(new RecipeIndexViewModel
        {
            Recipes = recipes,
            SearchText = searchText ?? "",
            PageNumber = pagedResult?.PageNumber ?? 1,
            PageSize = pagedResult?.PageSize ?? PageSize,
            TotalItems = pagedResult?.TotalItems ?? recipes.Count,
            TotalPages = pagedResult?.TotalPages ?? 0,
            HasPreviousPage = pagedResult?.HasPreviousPage ?? false,
            HasNextPage = pagedResult?.HasNextPage ?? false
        });
    }

    public IActionResult Details(
        int id,
        int pageNumber = 1,
        string? searchText = null)
    {
        Recipe? recipe = _recipeLibraryService.GetRecipeById(id);

        if (recipe == null)
        {
            return RecipeNotFound();
        }

        SetPageNumber(pageNumber);
        SetSearchText(searchText);

        return View(recipe);
    }

    public IActionResult Create()
    {
        return View(new RecipeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(RecipeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        RecipeSaveResult result = _recipeLibraryService.SaveRecipe(model.ToRecipe());

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);

            return View(model);
        }

        if (result.Recipe == null)
        {
            TempData[SuccessMessageKey] =
                "Rețeta a fost salvată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.";

            return RedirectToAction(nameof(Index));
        }

        TempData[SuccessMessageKey] = result.Message;

        return RedirectToAction(
            nameof(Details),
            new { id = result.Recipe.Id });
    }

    public IActionResult Edit(
        int id,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        Recipe? recipe = _recipeLibraryService.GetRecipeById(id);

        if (recipe == null)
        {
            return RecipeNotFound();
        }

        SetEditCancelAction(returnTo);
        SetPageNumber(pageNumber);
        SetSearchText(searchText);

        return View(RecipeFormViewModel.FromRecipe(recipe));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        RecipeFormViewModel model,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            SetEditCancelAction(returnTo);
            SetPageNumber(pageNumber);
            SetSearchText(searchText);

            return View(model);
        }

        RecipeSaveResult result = _recipeLibraryService.UpdateRecipe(model.ToRecipe());

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);
            SetEditCancelAction(returnTo);
            SetPageNumber(pageNumber);
            SetSearchText(searchText);

            return View(model);
        }

        TempData[SuccessMessageKey] = result.Message;

        if (IsReturnToIndex(returnTo))
        {
            return RedirectToAction(
                nameof(Index),
                new { pageNumber, searchText });
        }

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = model.Id,
                pageNumber,
                searchText
            });
    }

    public IActionResult Delete(
        int id,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        Recipe? recipe = _recipeLibraryService.GetRecipeById(id);

        if (recipe == null)
        {
            return RecipeNotFound();
        }

        SetDeleteCancelAction(returnTo);
        SetPageNumber(pageNumber);
        SetSearchText(searchText);

        return View(recipe);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(
        int id,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        RecipeSaveResult result = _recipeLibraryService.DeleteRecipe(id);

        if (!result.IsSuccess)
        {
            Recipe? recipe = _recipeLibraryService.GetRecipeById(id);

            if (recipe == null)
            {
                return RecipeNotFound();
            }

            ModelState.AddModelError("", result.Message);
            SetDeleteCancelAction(returnTo);
            SetPageNumber(pageNumber);
            SetSearchText(searchText);

            return View(recipe);
        }

        TempData[SuccessMessageKey] = result.Message;

        if (IsReturnToIndex(returnTo))
        {
            return RedirectToAction(
                nameof(Index),
                new { pageNumber, searchText });
        }

        return RedirectToAction(
            nameof(Index),
            new { pageNumber, searchText });
    }

    private void SetEditCancelAction(string? returnTo)
    {
        ViewData[EditCancelActionKey] = IsReturnToIndex(returnTo)
            ? nameof(Index)
            : nameof(Details);
    }

    private void SetDeleteCancelAction(string? returnTo)
    {
        ViewData[DeleteCancelActionKey] = IsReturnToIndex(returnTo)
            ? nameof(Index)
            : nameof(Details);
    }

    private void SetPageNumber(int pageNumber)
    {
        ViewData[PageNumberKey] = Math.Max(1, pageNumber);
    }

    private void SetSearchText(string? searchText)
    {
        ViewData[SearchTextKey] = searchText ?? "";
    }

    private static bool IsReturnToIndex(string? returnTo)
    {
        return string.Equals(
            returnTo,
            ReturnToIndexValue,
            StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RecipeNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;

        return View("NotFound");
    }

    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string? url)
    {
        RecipeImportResult importResult =
            await _recipeImporterService.ImportRecipeFromUrlAsync(url ?? "");

        if (!importResult.Success)
        {
            if (IsMissingImportedRecipeDetails(importResult.Message))
            {
                ModelState.AddModelError(
                    "url",
                    ImportMissingRequiredDetailsMessage);
                ViewData[ShowManualAddRecipeLinkKey] = true;
            }
            else
            {
                ModelState.AddModelError("url", importResult.Message);
            }

            return View(model: url);
        }

        if (importResult.Recipe == null)
        {
            TempData[SuccessMessageKey] =
                "Rețeta a fost importată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.";

            return RedirectToAction(nameof(Index));
        }

        RecipeSaveResult result = _recipeLibraryService.SaveRecipe(importResult.Recipe);

        if (!result.IsSuccess)
        {
            if (IsMissingImportedRecipeDetails(result.Message))
            {
                ModelState.AddModelError(
                    "url",
                    ImportMissingRequiredDetailsMessage);
                ViewData[ShowManualAddRecipeLinkKey] = true;
            }
            else
            {
                ModelState.AddModelError("url", result.Message);
            }

            return View(model: url);
        }

        if (result.Recipe == null)
        {
            TempData[SuccessMessageKey] =
                "Rețeta a fost importată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.";

            return RedirectToAction(nameof(Index));
        }

        TempData[SuccessMessageKey] =
            "Rețeta a fost importată. Verifică informațiile și editează-le dacă este nevoie.";

        return RedirectToAction(
            nameof(Details),
            new { id = result.Recipe.Id });
    }

    private static bool IsMissingImportedRecipeDetails(string message)
    {
        return message.Contains(global::AppTexts.RecipeIngredientsRequired)
            || message.Contains(global::AppTexts.RecipeStepsRequired);
    }
}
