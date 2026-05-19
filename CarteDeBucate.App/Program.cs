
IRecipeRepository recipeRepository = CreateRecipeRepository(AppSettings.CurrentStorageMode);
RecipeImporter recipeImporter = new RecipeImporter();

IRecipeService recipeService = new RecipeService(recipeRepository, recipeImporter);

RecipeConsoleApp app = new RecipeConsoleApp(recipeService);

await app.RunAsync();

IRecipeRepository CreateRecipeRepository(StorageMode storageMode)
{
    if (storageMode == StorageMode.Json)
    {
        return new JsonRecipeRepository(AppSettings.JsonFilePath);
    }

    if (storageMode == StorageMode.Database)
    {
        DatabaseInitializer databaseInitializer = new DatabaseInitializer(AppSettings.DatabasePath);
        databaseInitializer.Initialize();

        return new DatabaseRecipeRepository(AppSettings.DatabasePath);
    }

    throw new InvalidOperationException(AppTexts.UnknownStorageModeError);
}