public interface IRecipePhotoRepository
{
    void Add(RecipePhoto photo);
    bool AddForUser(RecipePhoto photo, int userId);
    RecipePhoto? GetById(int photoId);
    RecipePhoto? GetByIdAndUserId(int photoId, int userId);
    List<RecipePhoto> GetByRecipeId(int recipeId);
    List<RecipePhoto> GetByRecipeIdAndUserId(int recipeId, int userId);
    bool Delete(int photoId);
    bool DeleteForUser(int photoId, int userId);
}
