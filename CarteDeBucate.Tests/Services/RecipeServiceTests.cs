public class RecipeServiceTests
{
    [Fact]
    public void SaveRecipe_WithValidRecipe_ShouldSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        FakeRecipeImporter importer = new FakeRecipeImporter();

        RecipeService service = new RecipeService(repository, importer);

        Recipe recipe = CreateValidRecipe();

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddRecipeWasCalled);
        Assert.Equal(recipe, repository.AddedRecipe);
    }

    [Fact]
    public void SaveRecipe_WithInvalidRecipe_ShouldNotSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();
        FakeRecipeImporter importer = new FakeRecipeImporter();

        RecipeService service = new RecipeService(repository, importer);

        Recipe recipe = CreateValidRecipe();
        recipe.Name = "";

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.False(result.IsSuccess);
        Assert.False(repository.AddRecipeWasCalled);
    }

    [Fact]
    public void SaveRecipe_WithExistingSourceUrl_ShouldNotSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository
        {
            SourceUrlExists = true
        };

        FakeRecipeImporter importer = new FakeRecipeImporter();

        RecipeService service = new RecipeService(repository, importer);

        Recipe recipe = CreateValidRecipe();

        RecipeSaveResult result = service.SaveRecipe(recipe);

        Assert.False(result.IsSuccess);
        Assert.False(repository.AddRecipeWasCalled);
        Assert.Equal(AppTexts.RecipeAlreadyExists, result.Message);
    }

    [Fact]
    public async Task ImportFromUrlAndSaveAsync_WithFailedImport_ShouldNotSaveRecipe()
    {
        FakeRecipeRepository repository = new FakeRecipeRepository();

        FakeRecipeImporter importer = new FakeRecipeImporter
        {
            ImportResult = new RecipeImportResult
            {
                Success = false,
                Message = "Import failed."
            }
        };

        RecipeService service = new RecipeService(repository, importer);

        RecipeImportResult result = await service.ImportRecipeFromUrlAsync("https://example.com");

        Assert.False(result.Success);
        Assert.False(repository.AddRecipeWasCalled);
        Assert.Equal("Import failed.", result.Message);
    }

    [Fact]
    public async Task ImportRecipeFromUrlAsync_WithSuccessfulImport_ShouldReturnImportedRecipe()
    {
        Recipe recipe = CreateValidRecipe();

        FakeRecipeRepository repository = new FakeRecipeRepository();

        FakeRecipeImporter importer = new FakeRecipeImporter
        {
            ImportResult = new RecipeImportResult
            {
                Success = true,
                Recipe = recipe,
                Message = "Import successful."
            }
        };

        RecipeService service = new RecipeService(repository, importer);

        RecipeImportResult result = await service.ImportRecipeFromUrlAsync(recipe.SourceUrl);

        Assert.True(result.Success);
        Assert.Equal(recipe, result.Recipe);
        Assert.Equal("Import successful.", result.Message);
        Assert.False(repository.AddRecipeWasCalled);
    }

    [Fact]
    public async Task ImportFromUrlAndSaveAsync_WithSuccessfulImport_ShouldSaveRecipe()
    {
        Recipe recipe = CreateValidRecipe();

        FakeRecipeRepository repository = new FakeRecipeRepository();

        FakeRecipeImporter importer = new FakeRecipeImporter
        {
            ImportResult = new RecipeImportResult
            {
                Success = true,
                Recipe = recipe,
                Message = "Import successful."
            }
        };

        RecipeService service = new RecipeService(repository, importer);

        RecipeSaveResult result = await service.ImportFromUrlAndSaveAsync(recipe.SourceUrl);

        Assert.True(result.IsSuccess);
        Assert.True(repository.AddRecipeWasCalled);
        Assert.Equal(recipe, repository.AddedRecipe);
    }

    private static Recipe CreateValidRecipe()
    {
        return new Recipe
        {
            Name = "Banana bread",
            SourceUrl = "https://example.com/banana-bread",
            SavedAt = new DateTime(2026, 1, 1),
            Ingredients = new List<string>
            {
                "2 bananas",
                "2 eggs"
            },
            Steps = new List<string>
            {
                "Mix ingredients.",
                "Bake for 30 minutes."
            }
        };
    }

    public class FakeRecipeRepository : IRecipeRepository
    {
        public bool AddRecipeWasCalled { get; private set; }
        public bool UpdateRecipeWasCalled { get; private set; }
        public bool DeleteRecipeWasCalled { get; private set; }

        public Recipe? AddedRecipe { get; private set; }
        public Recipe? UpdatedRecipe { get; private set; }
        public int? DeletedRecipeId { get; private set; }

        public bool SourceUrlExists { get; set; }

        public List<Recipe> Recipes { get; set; } = new List<Recipe>();

        public List<Recipe> GetAllRecipes()
        {
            return Recipes;
        }

        public Recipe? GetRecipeById(int recipeId)
        {
            return Recipes.FirstOrDefault(recipe => recipe.Id == recipeId);
        }

        public void AddRecipe(Recipe recipe)
        {
            AddRecipeWasCalled = true;
            AddedRecipe = recipe;
            Recipes.Add(recipe);
        }

        public void UpdateRecipe(Recipe recipe)
        {
            UpdateRecipeWasCalled = true;
            UpdatedRecipe = recipe;

            Recipe? existingRecipe = Recipes.FirstOrDefault(r => r.Id == recipe.Id);

            if (existingRecipe != null)
            {
                Recipes.Remove(existingRecipe);
            }

            Recipes.Add(recipe);
        }

        public bool RecipeExistsBySourceUrl(string sourceUrl)
        {
            if (SourceUrlExists)
            {
                return true;
            }

            return Recipes.Any(recipe => recipe.SourceUrl == sourceUrl);
        }

        public void DeleteRecipe(int recipeId)
        {
            DeleteRecipeWasCalled = true;
            DeletedRecipeId = recipeId;

            Recipe? existingRecipe = Recipes.FirstOrDefault(recipe => recipe.Id == recipeId);

            if (existingRecipe != null)
            {
                Recipes.Remove(existingRecipe);
            }
        }
    }

    public class FakeRecipeImporter : IRecipeImporter
    {
        public RecipeImportResult ImportResult { get; set; } = new RecipeImportResult
        {
            Success = false,
            Message = "Import result was not configured."
        };

        public Task<RecipeImportResult> ImportFromUrlAsync(string url)
        {
            return Task.FromResult(ImportResult);
        }
    }
}