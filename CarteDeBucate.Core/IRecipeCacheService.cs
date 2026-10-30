public interface IRecipeCacheService
{
    Task<Recipe?> GetAsync(int recipeId, int userId, CancellationToken cancellationToken = default);

    Task SetAsync(Recipe recipe, int userId, CancellationToken cancellationToken = default);

    Task RemoveAsync(int recipeId, int userId, CancellationToken cancellationToken = default);
}
