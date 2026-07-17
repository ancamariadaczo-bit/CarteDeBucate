using System.Text;
using CarteDeBucate.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarteDeBucate.Web.Controllers;

[ServiceFilter(typeof(OptionalAuthenticationFilter))]
public class RecipeBackupsController : Controller
{
    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
    private const string JsonContentType = "application/json";
    private const string EmptyBackupFileMessage = "Selectează un fișier backup JSON.";

    private readonly IRecipeBackupService _backupService;
    private readonly ILogger<RecipeBackupsController> _logger;

    public RecipeBackupsController(
        IRecipeBackupService backupService,
        ILogger<RecipeBackupsController> logger)
    {
        _backupService = backupService;
        _logger = logger;
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
            _logger.LogWarning(
                "Recipe backup export failed. Reason: {FailureReason}",
                result.Message);

            TempData[ErrorMessageKey] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        byte[] fileContents = Encoding.UTF8.GetBytes(result.Json);

        _logger.LogInformation(
            "Recipe backup export completed successfully. File size: {FileSizeBytes} bytes",
            fileContents.Length);

        return File(fileContents, JsonContentType, CreateBackupFileName());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            _logger.LogWarning("Recipe backup import rejected because the file was missing or empty");

            TempData[ErrorMessageKey] = EmptyBackupFileMessage;

            return RedirectToAction(nameof(Index));
        }

        using StreamReader reader = new StreamReader(backupFile.OpenReadStream());
        string json = await reader.ReadToEndAsync();

        RecipeBackupResult result = _backupService.ImportFromJsonContent(json);
        TempData[result.IsSuccess ? SuccessMessageKey : ErrorMessageKey] = result.Message;

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Recipe backup import completed successfully. File size: {FileSizeBytes} bytes",
                backupFile.Length);
        }
        else
        {
            _logger.LogWarning(
                "Recipe backup import failed. File size: {FileSizeBytes} bytes. Reason: {FailureReason}",
                backupFile.Length,
                result.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private static string CreateBackupFileName()
    {
        return $"recipes-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
    }
}
