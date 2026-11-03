public class FakeRecipeCacheService : IRecipeCacheService
{
    public Recipe? RecipeToReturn { get; set; }

    public Exception? ExceptionToThrowOnGet { get; set; }

    public Exception? ExceptionToThrowOnSet { get; set; }

    public Exception? ExceptionToThrowOnRemove { get; set; }

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public int? RecipeIdPassedToGet { get; private set; }

    public int? UserIdPassedToGet { get; private set; }

    public Recipe? RecipePassedToSet { get; private set; }

    public int? UserIdPassedToSet { get; private set; }

    public int? RecipeIdPassedToRemove { get; private set; }

    public int? UserIdPassedToRemove { get; private set; }

    public Task<Recipe?> GetAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetCallCount++;
        RecipeIdPassedToGet = recipeId;
        UserIdPassedToGet = userId;

        if (ExceptionToThrowOnGet is not null)
        {
            throw ExceptionToThrowOnGet;
        }

        return Task.FromResult(RecipeToReturn);
    }

    public Task SetAsync(
        Recipe recipe,
        int userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SetCallCount++;
        RecipePassedToSet = recipe;
        UserIdPassedToSet = userId;

        if (ExceptionToThrowOnSet is not null)
        {
            throw ExceptionToThrowOnSet;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        int recipeId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RemoveCallCount++;
        RecipeIdPassedToRemove = recipeId;
        UserIdPassedToRemove = userId;

        if (ExceptionToThrowOnRemove is not null)
        {
            throw ExceptionToThrowOnRemove;
        }

        return Task.CompletedTask;
    }
}
