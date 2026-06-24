using Microsoft.AspNetCore.Mvc;
using CarteDeBucate.Web.Models;

namespace CarteDeBucate.Web.Controllers;

public class RecipesController : Controller
{
    private readonly IRecipeImporterService _recipeService;

    public RecipesController(IRecipeImporterService recipeService)
    {
        _recipeService = recipeService;
    }

    public IActionResult Index()
    {
        List<RecipeSummary> recipes = _recipeService.GetRecipeSummaries();

        return View(recipes);
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

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        Recipe? recipe = _recipeService.GetRecipeById(id);

        if (recipe == null)
        {
            return NotFound();
        }

        return View(RecipeFormViewModel.FromRecipe(recipe));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, RecipeFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        RecipeSaveResult result = _recipeService.UpdateRecipe(model.ToRecipe());

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);

            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public IActionResult Delete(int id)
    {
        Recipe? recipe = _recipeService.GetRecipeById(id);

        if (recipe == null)
        {
            return NotFound();
        }

        return View(recipe);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
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

            return View(recipe);
        }

        return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
    }
}
