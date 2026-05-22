
IRecipeConsoleReader recipeReader = new RecipeConsoleReader();
IRecipeConsoleWriter recipeWriter = new RecipeConsoleWriter();

IRecipeRepository recipeRepository = CreateRecipeRepository(AppSettings.CurrentStorageMode);
RecipeImporter recipeImporter = new RecipeImporter();

IRecipeImporterService importerService = new RecipeImporterService(recipeRepository, recipeImporter);

IRecipeBackupService backupService = new RecipeBackupService(recipeRepository);

RecipeConsoleApp app = new RecipeConsoleApp(recipeReader, recipeWriter, importerService, backupService);

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

        DatabaseMigrator migrator = new DatabaseMigrator(AppSettings.DatabasePath);
        migrator.ApplyMigrations();

        return new DatabaseRecipeRepository(AppSettings.DatabasePath);
    }

    throw new InvalidOperationException(AppTexts.UnknownStorageModeError);
}