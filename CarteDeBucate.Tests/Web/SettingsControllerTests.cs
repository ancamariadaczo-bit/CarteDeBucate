using System.Security.Claims;
using CarteDeBucate.Web.Controllers;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class SettingsControllerTests
{
    [Fact]
    public void Index_ShouldReturnTheAuthenticatedUsersPreferredCulture()
    {
        SettingsController controller = CreateController(
            CreateUserRepository("ro-RO"));

        IActionResult result = controller.Index();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        PreferredCultureViewModel model =
            Assert.IsType<PreferredCultureViewModel>(viewResult.Model);
        Assert.Equal("ro-RO", model.PreferredCulture);
    }

    [Fact]
    public void UpdatePreferredCulture_ShouldPersistTheValueAndSetTheCultureCookie()
    {
        FakeUserRepository repository = CreateUserRepository("en-US");
        SettingsController controller = CreateController(repository);

        IActionResult result = controller.UpdatePreferredCulture(
            new PreferredCultureViewModel { PreferredCulture = "ro-RO" });

        RedirectToActionResult redirectResult =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(SettingsController.Index), redirectResult.ActionName);
        Assert.Equal("ro-RO", repository.GetById(7)?.PreferredCulture);
        string setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(
            ".AspNetCore.Culture",
            setCookie);
        Assert.Contains("ro-RO", setCookie);
    }

    [Fact]
    public void UpdatePreferredCulture_WithUnsupportedValue_ShouldNotPersist()
    {
        FakeUserRepository repository = CreateUserRepository("en-US");
        SettingsController controller = CreateController(repository);

        IActionResult result = controller.UpdatePreferredCulture(
            new PreferredCultureViewModel { PreferredCulture = "de-DE" });

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(SettingsController.Index), viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal("en-US", repository.GetById(7)?.PreferredCulture);
    }

    private static SettingsController CreateController(FakeUserRepository repository)
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "7")],
                "Test"))
        };

        return new SettingsController(repository)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static FakeUserRepository CreateUserRepository(string preferredCulture)
    {
        FakeUserRepository repository = new();
        repository.Users.Add(new User
        {
            Id = 7,
            Email = "anca@example.com",
            PreferredCulture = preferredCulture
        });

        return repository;
    }
}
