using System.Text;
using CarteDeBucate.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class RecipeBackupsControllerTests
{
    [Fact]
    public void Index_ShouldReturnDefaultView()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService();
        RecipeBackupsController controller = CreateController(backupService);

        IActionResult result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Export_WhenBackupSucceeds_ShouldCallServiceAndReturnJsonFile()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService
        {
            ExportContentResultToReturn = new RecipeBackupExportResult
            {
                IsSuccess = true,
                Message = "Exported.",
                ExportedCount = 1,
                Json = "[{\"Name\":\"Recipe\"}]"
            }
        };
        RecipeBackupsController controller = CreateController(backupService);

        IActionResult result = controller.Export();

        FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
        Assert.True(backupService.ExportToJsonContentWasCalled);
        Assert.Equal("application/json", fileResult.ContentType);
        Assert.EndsWith(".json", fileResult.FileDownloadName);
        Assert.Equal(
            "[{\"Name\":\"Recipe\"}]",
            Encoding.UTF8.GetString(fileResult.FileContents));
    }

    [Fact]
    public void Export_WhenBackupFails_ShouldRedirectToIndexAndSetErrorMessage()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService
        {
            ExportContentResultToReturn = new RecipeBackupExportResult
            {
                IsSuccess = false,
                Message = "Export failed."
            }
        };
        RecipeBackupsController controller = CreateController(backupService);

        IActionResult result = controller.Export();

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipeBackupsController.Index), redirectResult.ActionName);
        Assert.True(backupService.ExportToJsonContentWasCalled);
        Assert.Equal("Export failed.", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Import_WhenFileIsMissing_ShouldRedirectToIndexAndSetErrorMessage()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService();
        RecipeBackupsController controller = CreateController(backupService);

        IActionResult result = await controller.Import(null);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipeBackupsController.Index), redirectResult.ActionName);
        Assert.False(backupService.ImportFromJsonContentWasCalled);
        Assert.Equal(
            "Selectează un fișier backup JSON.",
            controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Import_WhenFileIsValid_ShouldCallService()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService();
        RecipeBackupsController controller = CreateController(backupService);
        IFormFile backupFile = CreateFormFile("[{\"Name\":\"Recipe\"}]");

        await controller.Import(backupFile);

        Assert.True(backupService.ImportFromJsonContentWasCalled);
        Assert.Equal(
            "[{\"Name\":\"Recipe\"}]",
            backupService.JsonPassedToImportFromJsonContent);
    }

    [Fact]
    public async Task Import_WhenBackupSucceeds_ShouldRedirectToIndexAndSetSuccessMessage()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService
        {
            ImportResultToReturn = new RecipeBackupResult
            {
                IsSuccess = true,
                Message = "Imported."
            }
        };
        RecipeBackupsController controller = CreateController(backupService);
        IFormFile backupFile = CreateFormFile("[]");

        IActionResult result = await controller.Import(backupFile);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipeBackupsController.Index), redirectResult.ActionName);
        Assert.Equal("Imported.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Import_WhenBackupFails_ShouldRedirectToIndexAndSetErrorMessage()
    {
        FakeRecipeBackupService backupService = new FakeRecipeBackupService
        {
            ImportResultToReturn = new RecipeBackupResult
            {
                IsSuccess = false,
                Message = "Import failed."
            }
        };
        RecipeBackupsController controller = CreateController(backupService);
        IFormFile backupFile = CreateFormFile("not json");

        IActionResult result = await controller.Import(backupFile);

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RecipeBackupsController.Index), redirectResult.ActionName);
        Assert.Equal("Import failed.", controller.TempData["ErrorMessage"]);
    }

    private static IFormFile CreateFormFile(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        MemoryStream stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "backupFile", "backup.json");
    }

    private static RecipeBackupsController CreateController(
        IRecipeBackupService backupService)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        RecipeBackupsController controller = new RecipeBackupsController(backupService)
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
