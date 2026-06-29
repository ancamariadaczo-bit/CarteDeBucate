using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

public class RecipeBackupsController : Controller
{
    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
    private const string JsonContentType = "application/json";
    private const string EmptyBackupFileMessage = "Selectează un fișier backup JSON.";

    private readonly IRecipeBackupService _backupService;

    public RecipeBackupsController(IRecipeBackupService backupService)
    {
        _backupService = backupService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Export()
    {
        RecipeBackupExportResult result = _backupService.ExportToJsonContent();

        if (!result.IsSuccess)
        {
            TempData[ErrorMessageKey] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        byte[] fileContents = Encoding.UTF8.GetBytes(result.Json);

        return File(fileContents, JsonContentType, CreateBackupFileName());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            TempData[ErrorMessageKey] = EmptyBackupFileMessage;

            return RedirectToAction(nameof(Index));
        }

        using StreamReader reader = new StreamReader(backupFile.OpenReadStream());
        string json = await reader.ReadToEndAsync();

        RecipeBackupResult result = _backupService.ImportFromJsonContent(json);
        TempData[result.IsSuccess ? SuccessMessageKey : ErrorMessageKey] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    private static string CreateBackupFileName()
    {
        return $"recipes-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
    }
}
