using Microsoft.AspNetCore.Mvc;
using CarteDeBucate.Web.Models;

namespace CarteDeBucate.Web.Controllers;

public class RecipesController : Controller
{
    private const string SuccessMessageKey = "SuccessMessage";
    private const string DeleteCancelActionKey = "DeleteCancelAction";
    private const string EditCancelActionKey = "EditCancelAction";
    private const string ReturnToIndexValue = "Index";

    private readonly IRecipeImporterService _recipeService;

    public RecipesController(IRecipeImporterService recipeService)
    {
        _recipeService = recipeService;
    }

    public IActionResult Index(string? searchText)
    {
        List<RecipeSummary> recipes = string.IsNullOrWhiteSpace(searchText)
            ? _recipeService.GetRecipeSummaries()
            : _recipeService.SearchRecipes(searchText);

        return View(new RecipeIndexViewModel
        {
            Recipes = recipes,
            SearchText = searchText ?? ""
        });
    }

    public IActionResult Details(int id)
    {
        Recipe? recipe = _recipeService.GetRecipeById(id);

        if (recipe == null)
        {
            return NotFound();
        }

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

        RecipeSaveResult result = _recipeService.SaveRecipe(model.ToRecipe());

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

    public IActionResult Edit(int id, string? returnTo = null)
    {
        Recipe? recipe = _recipeService.GetRecipeById(id);

        if (recipe == null)
        {
            return NotFound();
        }

        SetEditCancelAction(returnTo);

        return View(RecipeFormViewModel.FromRecipe(recipe));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        RecipeFormViewModel model,
        string? returnTo = null)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            SetEditCancelAction(returnTo);

            return View(model);
        }

        RecipeSaveResult result = _recipeService.UpdateRecipe(model.ToRecipe());

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);
            SetEditCancelAction(returnTo);

            return View(model);
        }

        TempData[SuccessMessageKey] = result.Message;

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public IActionResult Delete(int id, string? returnTo = null)
    {
        Recipe? recipe = _recipeService.GetRecipeById(id);

        if (recipe == null)
        {
            return NotFound();
        }

        SetDeleteCancelAction(returnTo);

        return View(recipe);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id, string? returnTo = null)
    {
        RecipeSaveResult result = _recipeService.DeleteRecipe(id);

        if (!result.IsSuccess)
        {
            Recipe? recipe = _recipeService.GetRecipeById(id);

            if (recipe == null)
            {
                return NotFound();
            }

            ModelState.AddModelError("", result.Message);
            SetDeleteCancelAction(returnTo);

            return View(recipe);
        }

        TempData[SuccessMessageKey] = result.Message;

        return RedirectToAction(nameof(Index));
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

    private static bool IsReturnToIndex(string? returnTo)
    {
        return string.Equals(
            returnTo,
            ReturnToIndexValue,
            StringComparison.OrdinalIgnoreCase);
    }

    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string? url)
    {
        RecipeSaveResult result =
            await _recipeService.ImportFromUrlAndSaveAsync(url ?? "");

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("url", result.Message);

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
}
