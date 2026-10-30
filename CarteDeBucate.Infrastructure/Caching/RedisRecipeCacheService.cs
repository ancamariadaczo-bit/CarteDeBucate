using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

public class RedisRecipeCacheService : IRecipeCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _cache;

    public RedisRecipeCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<Recipe?> GetAsync(int recipeId, int userId, CancellationToken cancellationToken = default)
    {
        string key = GetCacheKey(recipeId, userId);

        string? json = await _cache.GetStringAsync(key, cancellationToken);

        if (json == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Recipe>(json);
    }

    public async Task SetAsync(Recipe recipe, int userId, CancellationToken cancellationToken = default)
    {
        string key = GetCacheKey(recipe.Id, userId);

        string json = JsonSerializer.Serialize(recipe);

        var options = new DistributedCacheEntryOptions
        {
            // The cached entry expires after CacheDuration period, regardless of its usage.
            AbsoluteExpirationRelativeToNow = CacheDuration,
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveAsync(int recipeId, int userId, CancellationToken cancellationToken = default)
    {
        string key = GetCacheKey(recipeId, userId);

        await _cache.RemoveAsync(key, cancellationToken);
    }

    private static string GetCacheKey(int recipeId, int userId)
    {
        return $"recipe:{recipeId}:{userId}";
    }
}
