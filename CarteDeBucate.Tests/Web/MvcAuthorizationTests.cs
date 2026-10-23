using CarteDeBucate.Web.Controllers;
using Microsoft.AspNetCore.Authorization;

public class MvcAuthorizationTests
{
    [Fact]
    public void RecipesController_ShouldRequireAuthorization()
    {
        AssertControllerRequiresAuthorization(typeof(RecipesController));
    }

    [Fact]
    public void RecipeBackupsController_ShouldRequireAuthorization()
    {
        AssertControllerRequiresAuthorization(typeof(RecipeBackupsController));
    }

    private static void AssertControllerRequiresAuthorization(Type controllerType)
    {
        AuthorizeAttribute? authorizeAttribute = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
    }
}
