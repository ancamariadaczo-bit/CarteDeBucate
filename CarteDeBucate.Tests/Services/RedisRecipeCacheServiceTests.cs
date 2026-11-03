using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

public class RedisRecipeCacheServiceTests
{
    [Fact]
    public async Task GetAsync_WhenEntryDoesNotExist_ShouldReturnNull()
    {
        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();

        IDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        var result = await cacheService.GetAsync(42, 7);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenEntryExist_ShouldReturnTheCorrectRecipe()
    {
        Recipe recipe = CreateTestRecipe(7);
        recipe.Id = 42;
        byte[] recipeBytes = JsonSerializer.SerializeToUtf8Bytes(recipe);

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
        entries.Add("recipe:42:7", recipeBytes);

        IDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        var result = await cacheService.GetAsync(42, 7);

        Assert.NotNull(result);
        Assert.Equal(recipe.Name, result.Name);
        Assert.Equal(recipe.SourceUrl, result.SourceUrl);
        Assert.Equal(recipe.SavedAt, result.SavedAt);
        Assert.Equal(recipe.Ingredients, result.Ingredients);
        Assert.NotSame(recipe.Ingredients, result.Ingredients);
        Assert.Equal(recipe.Steps, result.Steps);
        Assert.NotSame(recipe.Steps, result.Steps);
        Assert.Equal(recipe.Notes, result.Notes);
        Assert.Equal(recipe.Status, result.Status);
    }

    [Fact]
    public async Task GetAsync_WhenDifferentUsersAndSameRecipeId_ShouldIsolateRecipes()
    {
        Recipe recipeForUser20 = CreateTestRecipe(20);
        recipeForUser20.Id = 5;

        Recipe recipeForUser30 = CreateTestRecipe(30);
        recipeForUser30.Id = 5;

        byte[] recipeBytesForUser20 = JsonSerializer.SerializeToUtf8Bytes(recipeForUser20);
        byte[] recipeBytesForUser30 = JsonSerializer.SerializeToUtf8Bytes(recipeForUser30);

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>
        {
            { "recipe:5:20", recipeBytesForUser20 },
            { "recipe:5:30", recipeBytesForUser30 }
        };

        IDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        var resultForUser20 = await cacheService.GetAsync(5, 20);
        var resultForUser30 = await cacheService.GetAsync(5, 30);

        Assert.NotNull(resultForUser20);
        Assert.NotNull(resultForUser30);

        Assert.Equal(recipeForUser20.Id, resultForUser20.Id);
        Assert.Equal(recipeForUser20.UserId, resultForUser20.UserId);
        Assert.Equal(recipeForUser30.Id, resultForUser30.Id);
        Assert.Equal(recipeForUser30.UserId, resultForUser30.UserId);

        Assert.Equal(recipeForUser20.Name, resultForUser20.Name);
        Assert.Equal(recipeForUser20.SourceUrl, resultForUser20.SourceUrl);
        Assert.Equal(recipeForUser20.SavedAt, resultForUser20.SavedAt);
        Assert.Equal(recipeForUser20.Ingredients, resultForUser20.Ingredients);
        Assert.NotSame(recipeForUser20.Ingredients, resultForUser20.Ingredients);
        Assert.Equal(recipeForUser20.Steps, resultForUser20.Steps);
        Assert.NotSame(recipeForUser20.Steps, resultForUser20.Steps);
        Assert.Equal(recipeForUser20.Notes, resultForUser20.Notes);
        Assert.Equal(recipeForUser20.Status, resultForUser20.Status);
    }

    [Fact]
    public async Task SetAsync_StoresTheRecipeUnderCorrectKey()
    {
        Recipe recipe = CreateTestRecipe(7);
        recipe.Id = 42;

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
        IDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        await cacheService.SetAsync(recipe, 7);

        byte[]? storedBytes = await cache.GetAsync($"recipe:{recipe.Id}:{recipe.UserId}");

        Assert.NotNull(storedBytes);

        Recipe? storedRecipe = JsonSerializer.Deserialize<Recipe>(storedBytes);

        Assert.NotNull(storedRecipe);
    }

    [Fact]
    public async Task SetAsync_SerializesTheCompleteRecipe()
    {
        Recipe recipe = CreateTestRecipe(7);
        recipe.Id = 42;

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
        IDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        await cacheService.SetAsync(recipe, 7);

        byte[]? storedBytes = await cache.GetAsync($"recipe:{recipe.Id}:{recipe.UserId}");

        Assert.NotNull(storedBytes);

        Recipe? storedRecipe = JsonSerializer.Deserialize<Recipe>(storedBytes);

        Assert.NotNull(storedRecipe);
        Assert.Equal(recipe.Id, storedRecipe.Id);
        Assert.Equal(recipe.UserId, storedRecipe.UserId);
        Assert.Equal(recipe.Name, storedRecipe.Name);
        Assert.Equal(recipe.SourceUrl, storedRecipe.SourceUrl);
        Assert.Equal(recipe.SavedAt, storedRecipe.SavedAt);
        Assert.Equal(recipe.Ingredients, storedRecipe.Ingredients);
        Assert.NotSame(recipe.Ingredients, storedRecipe.Ingredients);
        Assert.Equal(recipe.Steps, storedRecipe.Steps);
        Assert.NotSame(recipe.Steps, storedRecipe.Steps);
        Assert.Equal(recipe.Notes, storedRecipe.Notes);
        Assert.Equal(recipe.Status, storedRecipe.Status);
    }

    [Fact]
    public async Task SetAsync_ShouldUseThirtyMinuteAbsoluteExpiration()
    {
        Recipe recipe = CreateTestRecipe(7);
        recipe.Id = 42;

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
        FakeDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        await cacheService.SetAsync(recipe, 7);

        DistributedCacheEntryOptions? options = cache.GetOptions("recipe:42:7");

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(30), options.AbsoluteExpirationRelativeToNow);
        Assert.Null(options.AbsoluteExpiration);
        Assert.Null(options.SlidingExpiration);
    }

    [Fact]
    public async Task RemoveAsync_WhenDifferentUsersAndSameRecipeId_ShouldDeleteCorrectRecipe()
    {
        Recipe recipeForUser20 = CreateTestRecipe(20);
        recipeForUser20.Id = 5;

        Recipe recipeForUser30 = CreateTestRecipe(30);
        recipeForUser30.Id = 5;

        byte[] recipeBytesForUser20 = JsonSerializer.SerializeToUtf8Bytes(recipeForUser20);
        byte[] recipeBytesForUser30 = JsonSerializer.SerializeToUtf8Bytes(recipeForUser30);

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>
        {
            { "recipe:5:20", recipeBytesForUser20 },
            { "recipe:5:30", recipeBytesForUser30 }
        };

        IDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        await cacheService.RemoveAsync(recipeForUser20.Id, 20);

        var resultForUser20 = await cacheService.GetAsync(recipeForUser20.Id, 20);
        var resultForUser30 = await cacheService.GetAsync(recipeForUser30.Id, 30);

        Assert.Null(resultForUser20);
        Assert.NotNull(resultForUser30);

        Assert.Equal(recipeForUser30.Id, resultForUser30.Id);
        Assert.Equal(recipeForUser30.UserId, resultForUser30.UserId);
        Assert.Equal(recipeForUser30.Name, resultForUser30.Name);
        Assert.Equal(recipeForUser30.SourceUrl, resultForUser30.SourceUrl);
        Assert.Equal(recipeForUser30.SavedAt, resultForUser30.SavedAt);
        Assert.Equal(recipeForUser30.Ingredients, resultForUser30.Ingredients);
        Assert.Equal(recipeForUser30.Steps, resultForUser30.Steps);
        Assert.Equal(recipeForUser30.Notes, resultForUser30.Notes);
        Assert.Equal(recipeForUser30.Status, resultForUser30.Status);
    }

    [Fact]
    public async Task GetAsync_WhenCachedJsonIsMalformed_ShouldThrowJsonException()
    {
        byte[] invalidJson = Encoding.UTF8.GetBytes("{ this is not valid JSON");

        Dictionary<string, byte[]> entries = new()
        {
            { "recipe:42:7", invalidJson }
        };

        FakeDistributedCache cache = new FakeDistributedCache(entries);
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        await Assert.ThrowsAsync<JsonException>(() => cacheService.GetAsync(42, 7));
    }

    [Fact]
    public async Task SetAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        FakeDistributedCache cache = new FakeDistributedCache(new Dictionary<string, byte[]>());
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);

        Recipe recipe = new Recipe
        {
            Id = 42,
            UserId = 7,
            Name = "Test recipe"
        };

        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cacheService.SetAsync(recipe, 7, cancelledToken));
    }

    [Fact]
    public async Task GetAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        FakeDistributedCache cache = new FakeDistributedCache(new Dictionary<string, byte[]>());
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cacheService.GetAsync(42, 7, cancelledToken));
    }

    [Fact]
    public async Task RemoveAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        FakeDistributedCache cache = new FakeDistributedCache(new Dictionary<string, byte[]>());
        IRecipeCacheService cacheService = new RedisRecipeCacheService(cache);
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cacheService.RemoveAsync(42, 7, cancelledToken));
    }

    private static Recipe CreateTestRecipe(int? userId)
    {
        return new Recipe
        {
            Name = $"Banana bread for {userId}",
            SourceUrl = $"https://example.com/banana-bread-{userId}",
            SavedAt = new DateTime(2026, 1, 1),
            UserId = userId,
            Ingredients = new List<string>
            {
                $"{userId} bananas",
                $"{userId} eggs"
            },
            Steps = new List<string>
            {
                "Mix ingredients.",
                $"Bake for {userId} minutes."
            }
        };
    }
}