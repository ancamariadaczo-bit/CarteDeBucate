using System.Security.Claims;
using System.Text.Json;
using CarteDeBucate.Web.Services.Authentication;
using Microsoft.AspNetCore.Http;

public class WebCurrentUserFilteringTests
{
    [Fact]
    public void GetRecipeSummaries_WithoutWebUser_ShouldReturnGlobalRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateRecipe(1, userId: null, "https://example.com/global"),
                CreateRecipe(2, userId: 7, "https://example.com/current-user")
            }
        };
        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            CreateAnonymousWebCurrentUserContext());

        List<RecipeSummary> recipes = service.GetRecipeSummaries();

        Assert.Equal(2, recipes.Count);
        Assert.Contains(recipes, recipe => recipe.UserId == null);
        Assert.True(repository.GetAllRecipeSummariesWasCalled);
        Assert.False(repository.GetRecipeSummariesByUserIdWasCalled);
    }

    [Fact]
    public void GetRecipeSummaries_WithAuthenticatedWebUser_ShouldReturnOnlyCurrentUserRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateRecipe(1, userId: 7, "https://example.com/current-user"),
                CreateRecipe(2, userId: 9, "https://example.com/other-user"),
                CreateRecipe(3, userId: null, "https://example.com/global")
            }
        };
        RecipeLibraryService service = CreateRecipeLibraryService(repository, userId: 7);

        List<RecipeSummary> recipes = service.GetRecipeSummaries();

        RecipeSummary recipe = Assert.Single(recipes);
        Assert.Equal(1, recipe.Id);
        Assert.Equal(7, repository.UserIdPassedToGetRecipeSummariesByUserId);
        Assert.False(repository.GetAllRecipeSummariesWasCalled);
    }

    [Fact]
    public void GetRecipeSummaries_WithAuthenticatedWebUser_ShouldNotReturnGlobalRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateRecipe(1, userId: null, "https://example.com/global"),
                CreateRecipe(2, userId: 7, "https://example.com/current-user")
            }
        };
        RecipeLibraryService service = CreateRecipeLibraryService(repository, userId: 7);

        List<RecipeSummary> recipes = service.GetRecipeSummaries();

        RecipeSummary recipe = Assert.Single(recipes);
        Assert.Equal(2, recipe.Id);
        Assert.Equal(7, recipe.UserId);
    }

    [Fact]
    public void SaveRecipe_WithAuthenticatedWebUser_ShouldSetCurrentUserId()
    {
        Recipe recipe = CreateRecipe(0, userId: null, "https://example.com/imported");
        FakeRecipeRepository repository = new FakeRecipeRepository();
        HttpCurrentUserContext currentUserContext = CreateWebCurrentUserContext(userId: 7);
        RecipeLibraryService service = new RecipeLibraryService(
            repository,
            currentUserContext);

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddRecipeWasCalled);
        Assert.Equal(7, repository.AddedRecipe?.UserId);
    }

    [Fact]
    public void ExportToJsonContent_WithAuthenticatedWebUser_ShouldExportOnlyCurrentUserRecipes()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            Recipes = new List<Recipe>
            {
                CreateRecipe(1, userId: 7, "https://example.com/current-user"),
                CreateRecipe(2, userId: 9, "https://example.com/other-user"),
                CreateRecipe(3, userId: null, "https://example.com/global")
            }
        };
        RecipeBackupService service = new RecipeBackupService(
            repository,
            CreateWebCurrentUserContext(userId: 7));

        RecipeBackupExportResult result = service.ExportToJsonContent();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ExportedCount);

        using JsonDocument jsonDocument = JsonDocument.Parse(result.Json);
        JsonElement exportedRecipe = Assert.Single(
            jsonDocument.RootElement.GetProperty("recipes").EnumerateArray());

        Assert.False(exportedRecipe.TryGetProperty("userId", out _));
        Assert.Equal(
            "https://example.com/current-user",
            exportedRecipe.GetProperty("sourceUrl").GetString());
    }

    private static RecipeLibraryService CreateRecipeLibraryService(
        FakeRecipeRepository repository,
        int userId)
    {
        return new RecipeLibraryService(
            repository,
            CreateWebCurrentUserContext(userId));
    }

    private static HttpCurrentUserContext CreateWebCurrentUserContext(int userId)
    {
        Claim[] claims =
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "anca")
        };
        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        return new HttpCurrentUserContext(new HttpContextAccessor
        {
            HttpContext = httpContext
        });
    }

    private static HttpCurrentUserContext CreateAnonymousWebCurrentUserContext()
    {
        return new HttpCurrentUserContext(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
    }

    private static Recipe CreateRecipe(int id, int? userId, string sourceUrl)
    {
        return new Recipe
        {
            Id = id,
            Name = "Banana bread",
            SourceUrl = sourceUrl,
            Ingredients = new List<string> { "Banana" },
            Steps = new List<string> { "Bake" },
            Notes = "",
            SavedAt = DateTime.Now,
            UserId = userId
        };
    }
}
