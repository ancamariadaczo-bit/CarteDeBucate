using CarteDeBucate.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

public class SettingsControllerTests
{
    [Fact]
    public void Index_ShouldReturnDefaultView()
    {
        SettingsController controller = new SettingsController();

        IActionResult result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
