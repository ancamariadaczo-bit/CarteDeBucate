using CarteDeBucate.Web.Models;
using CarteDeBucate.Web.Services.Photos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

[Authorize]
public class RecipesController : Controller
{
    private const int PageSize = 10;
    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
    private const string WarningMessageKey = "WarningMessage";
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
    private readonly IRecipePhotoService _recipePhotoService;
    private readonly IRecipePhotoUrlImporter _recipePhotoUrlImporter;
    private readonly ILogger<RecipesController> _logger;

    public RecipesController(
        IRecipeLibraryService recipeLibraryService,
        IRecipeImporterService recipeImporterService,
        IRecipePhotoService recipePhotoService,
        IRecipePhotoUrlImporter recipePhotoUrlImporter,
        ILogger<RecipesController> logger)
    {
        _recipeLibraryService = recipeLibraryService;
        _recipeImporterService = recipeImporterService;
        _recipePhotoService = recipePhotoService;
        _recipePhotoUrlImporter = recipePhotoUrlImporter;
        _logger = logger;
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

        // The current page contains at most ten recipes. Photo metadata is
        // intentionally loaded per recipe until Core exposes a bulk query.
        List<RecipeIndexItemViewModel> items = recipes
            .Select(recipe => new RecipeIndexItemViewModel
            {
                Recipe = recipe,
                PrimaryPhotoId = GetPrimaryPhotoId(recipe.Id)
            })
            .ToList();

        return View(new RecipeIndexViewModel
        {
            Recipes = recipes,
            Items = items,
            SearchText = searchText ?? "",
            PageNumber = pagedResult?.PageNumber ?? 1,
            PageSize = pagedResult?.PageSize ?? PageSize,
            TotalItems = pagedResult?.TotalItems ?? recipes.Count,
            TotalPages = pagedResult?.TotalPages ?? 0,
            HasPreviousPage = pagedResult?.HasPreviousPage ?? false,
            HasNextPage = pagedResult?.HasNextPage ?? false
        });
    }

    public async Task<IActionResult> Details(
        int id,
        int pageNumber = 1,
        string? searchText = null)
    {
        Recipe? recipe = await _recipeLibraryService.GetRecipeByIdAsync(id);

        if (recipe == null)
        {
            return RecipeNotFound();
        }

        IReadOnlyList<RecipePhotoInfo> photos = GetPhotosOrEmpty(id);

        SetPageNumber(pageNumber);
        SetSearchText(searchText);

        return View(new RecipeDetailsViewModel
        {
            Recipe = recipe,
            Photos = photos
        });
    }

    [HttpGet]
    public async Task<IActionResult> Print(int id)
    {
        Recipe? recipe = await _recipeLibraryService.GetRecipeByIdAsync(id);

        if (recipe == null)
        {
            return RecipeNotFound();
        }

        return View(new RecipePrintViewModel
        {
            Recipe = recipe,
            PrimaryPhotoId = GetPrimaryPhotoId(id)
        });
    }

    public IActionResult Create()
    {
        return View(new RecipeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RecipeFormViewModel model,
        CancellationToken cancellationToken = default)
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

        if (model.Photo != null)
        {
            await using Stream content = model.Photo.OpenReadStream();
            RecipePhotoResult photoResult =
                await _recipePhotoService.AddPhotoAsync(
                    result.Recipe.Id,
                    content,
                    model.Photo.FileName,
                    model.Photo.ContentType,
                    model.Photo.Length,
                    RecipePhotoOrigin.UserUpload,
                    cancellationToken);

            if (!photoResult.IsSuccess)
            {
                TempData[ErrorMessageKey] =
                    $"Rețeta a fost salvată, dar fotografia nu a putut fi adăugată. {GetPhotoFailureMessage(photoResult)}";
            }
        }

        return RedirectToAction(
            nameof(Details),
            new { id = result.Recipe.Id });
    }

    public async Task<IActionResult> Edit(
        int id,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        Recipe? recipe = await _recipeLibraryService.GetRecipeByIdAsync(id);

        if (recipe == null)
        {
            return RecipeNotFound();
        }

        RecipeFormViewModel model = RecipeFormViewModel.FromRecipe(recipe);
        model.Photos = GetPhotosOrEmpty(id);

        SetEditCancelAction(returnTo);
        SetPageNumber(pageNumber);
        SetSearchText(searchText);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
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
            model.Photos = GetPhotosOrEmpty(id);
            SetEditCancelAction(returnTo);
            SetPageNumber(pageNumber);
            SetSearchText(searchText);

            return View(model);
        }

        RecipeSaveResult result = await _recipeLibraryService.UpdateRecipeAsync(model.ToRecipe());

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);
            model.Photos = GetPhotosOrEmpty(id);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPhoto(
        RecipePhotoUploadViewModel model,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        if (model.Photo == null || model.Photo.Length == 0)
        {
            TempData[ErrorMessageKey] = "Selectează o fotografie pentru încărcare.";

            return RedirectToEdit(
                model.RecipeId,
                returnTo,
                pageNumber,
                searchText);
        }

        await using Stream content = model.Photo.OpenReadStream();
        RecipePhotoResult result = await _recipePhotoService.AddPhotoAsync(
            model.RecipeId,
            content,
            model.Photo.FileName,
            model.Photo.ContentType,
            model.Photo.Length,
            RecipePhotoOrigin.UserUpload,
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData[SuccessMessageKey] = result.Message;
        }
        else
        {
            TempData[ErrorMessageKey] = GetPhotoFailureMessage(result);
        }

        return RedirectToEdit(
            model.RecipeId,
            returnTo,
            pageNumber,
            searchText);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePhoto(
        int recipeId,
        int photoId,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        RecipePhotoResult result = _recipePhotoService.DeletePhoto(photoId);

        if (result.IsSuccess && result.Code == RecipePhotoResultCode.Success)
        {
            TempData[SuccessMessageKey] = result.Message;
        }
        else if (result.IsSuccess)
        {
            TempData[WarningMessageKey] = result.Message;
        }
        else
        {
            TempData[ErrorMessageKey] = GetPhotoFailureMessage(result);
        }

        return RedirectToEdit(
            recipeId,
            returnTo,
            pageNumber,
            searchText);
    }

    public async Task<IActionResult> Delete(
        int id,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        Recipe? recipe = await _recipeLibraryService.GetRecipeByIdAsync(id);

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
    public async Task<IActionResult> DeleteConfirmed(
        int id,
        string? returnTo = null,
        int pageNumber = 1,
        string? searchText = null)
    {
        RecipeSaveResult result = await _recipeLibraryService.DeleteRecipeAsync(id);

        if (!result.IsSuccess)
        {
            Recipe? recipe = await _recipeLibraryService.GetRecipeByIdAsync(id);

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
    public async Task<IActionResult> Import(
        string? url,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recipe URL import started");

        RecipeImportResult importResult =
            await _recipeImporterService.ImportRecipeFromUrlAsync(url ?? "");

        if (!importResult.Success)
        {
            _logger.LogWarning(
                "Recipe URL import failed. Reason: {FailureReason}",
                importResult.Message);

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
            _logger.LogWarning(
                "Recipe URL import reported success without returning a recipe");

            TempData[SuccessMessageKey] =
                "Rețeta a fost importată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.";

            return RedirectToAction(nameof(Index));
        }

        RecipeSaveResult result = _recipeLibraryService.SaveRecipe(importResult.Recipe);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Imported recipe could not be saved. Reason: {FailureReason}",
                result.Message);

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
            _logger.LogWarning(
                "Imported recipe save reported success without returning the saved recipe");

            TempData[SuccessMessageKey] =
                "Rețeta a fost importată, dar pagina de detalii nu a putut fi deschisă. Verifică lista de rețete.";

            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(importResult.ImageUrl))
        {
            TempData[SuccessMessageKey] =
                "Rețeta a fost importată. Verifică informațiile și editează-le dacă este nevoie.";
        }
        else
        {
            RecipePhotoUrlImportResult photoResult =
                await _recipePhotoUrlImporter.ImportAsync(
                    result.Recipe.Id,
                    importResult.ImageUrl,
                    cancellationToken);

            if (photoResult.IsSuccess)
            {
                TempData[SuccessMessageKey] =
                    "Rețeta și fotografia au fost importate. Verifică informațiile și editează-le dacă este nevoie.";
            }
            else
            {
                TempData[SuccessMessageKey] =
                    "Rețeta a fost importată. Verifică informațiile și editează-le dacă este nevoie.";
                TempData[WarningMessageKey] =
                    $"Fotografia nu a putut fi copiată. {photoResult.Message}";
            }
        }

        _logger.LogInformation(
            "Recipe URL import completed successfully. Recipe identifier: {RecipeId}",
            result.Recipe.Id);

        return RedirectToAction(
            nameof(Details),
            new { id = result.Recipe.Id });
    }

    private static bool IsMissingImportedRecipeDetails(string message)
    {
        return message.Contains(global::AppTexts.RecipeIngredientsRequired)
            || message.Contains(global::AppTexts.RecipeStepsRequired);
    }

    private IReadOnlyList<RecipePhotoInfo> GetPhotosOrEmpty(int recipeId)
    {
        RecipePhotosResult result =
            _recipePhotoService.GetPhotosForRecipe(recipeId);

        if (result.IsSuccess)
        {
            return result.Photos;
        }

        _logger.LogWarning(
            "Could not load photos for recipe {RecipeId}. Result code: {ResultCode}. Reason: {FailureReason}",
            recipeId,
            result.Code,
            result.Message);

        return Array.Empty<RecipePhotoInfo>();
    }

    private int? GetPrimaryPhotoId(int recipeId)
    {
        return GetPhotosOrEmpty(recipeId).FirstOrDefault()?.Id;
    }

    private IActionResult RedirectToEdit(
        int recipeId,
        string? returnTo,
        int pageNumber,
        string? searchText)
    {
        return RedirectToAction(
            nameof(Edit),
            new
            {
                id = recipeId,
                returnTo,
                pageNumber,
                searchText
            });
    }

    private static string GetPhotoFailureMessage(RecipePhotoResult result)
    {
        return result.Code switch
        {
            RecipePhotoResultCode.PhotoLimitReached =>
                "O rețetă poate avea maximum 20 de fotografii.",
            RecipePhotoResultCode.UnsupportedFormat or
            RecipePhotoResultCode.InvalidContent =>
                "Fotografia trebuie să fie un fișier JPEG, PNG sau WebP valid.",
            RecipePhotoResultCode.FileTooLarge =>
                "Fotografia depășește limita de 10 MiB.",
            RecipePhotoResultCode.RecipeNotFound or
            RecipePhotoResultCode.PhotoNotFound or
            RecipePhotoResultCode.InvalidRecipeId =>
                "Rețeta sau fotografia nu mai există.",
            _ => "Fotografia nu a putut fi procesată. Încearcă din nou."
        };
    }
}
