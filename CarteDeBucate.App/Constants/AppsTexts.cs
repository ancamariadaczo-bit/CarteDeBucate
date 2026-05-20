public static class AppTexts
{
    public const string AppTitle = "=== Carte de bucate personală ===";

    public const string MenuAddRecipe = "Adaugă rețetă";
    public const string MenuShowRecipes = "Afișează rețete";
    public const string MenuSearchRecipe = "Caută rețetă";
    public const string MenuExit = "Ieșire";

    public const string ChooseOption = "Alege o opțiune: ";
    public const string InvalidOption = "Opțiune invalidă.";
    public const string AppClosed = "Aplicația s-a închis.";

    public const string RecipeAdded = "Rețeta a fost adăugată și salvată.";
    public const string NoRecipes = "Nu ai nicio rețetă salvată încă.";
    public const string YourRecipes = "Rețetele tale:";

    public const string EnterRecipeName = "Introdu numele rețetei: ";
    public const string EnterSourceUrl = "Introdu link-ul sursă al rețetei: ";
    public const string EnterNotes = "Adaugă notițe personale, dacă vrei: ";

    public const string EnterIngredientsIntro = "Introdu ingredientele, câte unul pe rând.";
    public const string EnterStepsIntro = "Introdu pașii rețetei, câte unul pe rând.";
    public const string EmptyLineToFinish = "Când ai terminat, apasă Enter pe linie goală.";

    public const string IngredientPrompt = "Ingredient: ";
    public const string StepPrompt = "Pas: ";

    public const string SearchPrompt = "Scrie textul căutat: ";
    public const string SearchResults = "Rezultatele căutării:";
    public const string NoSearchResults = "Nu am găsit nicio rețetă care să corespundă căutării.";

    public const string RecipeAlreadyExists = "Există deja o rețetă salvată cu acest link.";
    public const string RecipeNotSaved = "Rețeta nu a fost salvată.";

    public const string MenuImportRecipeFromUrl = "Importă rețetă din link";

    public const string EnterRecipeUrlToImport = "Introdu link-ul rețetei: ";
    public const string ImportingRecipe = "Încerc să import rețeta...";
    public const string RecipeImported = "Rețeta a fost importată. Completează acum ingredientele, pașii și notițele.";
    public const string RecipeImportFailed = "Nu am putut importa rețeta din link-ul introdus.";
    public const string ImportedTitleNotFound = "Titlu negăsit";

    public const string ImportFailedEmptyUrl = "Link-ul este gol.";
    public const string ImportFailedCouldNotDownloadPage = "Nu am putut descărca pagina.";
    public const string ImportFailedNotRecipePage = "Pagina nu pare să fie o rețetă.";
    public const string ImportSuccessJsonLd = "Rețeta a fost importată din date structurate JSON-LD.";
    public const string ImportSuccessVisibleHtml = "Rețeta a fost importată din conținutul vizibil al paginii.";
    public const string ImportSuccessTitleOnly = "Am importat doar titlul paginii.";
    public const string ImportFailedBlockedPage =
    "Pagina pare blocată de o verificare automată. Programul nu a primit conținutul real al rețetei.";

    public const string ImportFailedInvalidUrl =
    "Link-ul introdus nu este valid. Te rog introdu un link complet, care începe cu http:// sau https://.";

    public const string EmptyUrl = "Nu ai introdus niciun URL.";
    public const string Ingredients = "   Ingrediente:";
    public const string Steps = "   Pași:";
    public const string ImportedIngredients = "Ingredientele importate sunt:";

    public const string SeparatorLine = "-----------------";
    public const string ConsolePrompt = "> ";

    public const string SourceLabel = "   Sursă: ";
    public const string SavedAtLabel = "   Salvată la: ";
    public const string ImportedAtLabel = "Data importului: ";
    public const string NameLabel = "Nume: ";
    public const string NotesLabel = "   Notițe: ";
    public const string NotesTitle = "Notițe: ";

    public const string IngredientsLabel = "   Ingrediente:";
    public const string StepsLabel = "   Pași:";
    public const string IngredientsTitle = "Ingrediente:";
    public const string StepsTitle = "Pași:";

    public const string ListItemPrefix = "   - ";

    public const string ImportedRecipeTitle = "=== Rețetă importată ===";
    public const string ImportedRecipeEndLine = "========================";

    public const string NoIngredientsFound = "- Nu au fost găsite ingrediente.";
    public const string NoStepsFound = "- Nu au fost găsiți pași.";

    public const string KeepImportedIngredientsPrompt = "Dacă vrei să păstrezi ingredientele importate, apasă Enter.";
    public const string EditImportedIngredientsPrompt = "Dacă vrei să le rescrii manual, scrie orice text și apoi Enter.";

    public const string KeepImportedStepsPrompt = "Dacă vrei să păstrezi pașii importați, apasă Enter.";
    public const string EditImportedStepsPrompt = "Dacă vrei să îi rescrii manual, scrie orice text și apoi Enter.";

    public const string MenuDeleteRecipe = "Șterge rețetă";
    public const string EnterRecipeIdToDelete = "Introdu ID-ul rețetei pe care vrei să o ștergi: ";
    public const string InvalidRecipeId = "ID invalid.";
    public const string RecipeDeleted = "Rețeta a fost ștearsă.";
    public const string RecipeNotFound = "Nu am găsit o rețetă cu acest ID.";
    public const string MenuViewRecipeDetails = "Vezi detalii rețetă";
    public const string EnterRecipeIdToView = "Introdu ID-ul rețetei pe care vrei să o vezi: ";
    public const string RecipeDetailsTitle = "=== Detalii rețetă ===";
    public const string RecipeDetailsEndLine = "======================";
    public const string IdLabel = "ID";
    public const string UnknownStorageModeError = "Unknown storage mode.";
    public const string MenuEditRecipe = "Editează rețetă";
    public const string EnterRecipeIdToEdit = "Introdu ID-ul rețetei pe care vrei să o editezi: ";
    public const string RecipeUpdated = "Rețeta a fost actualizată.";
    public const string KeepCurrentValuePrompt = "Apasă Enter ca să păstrezi valoarea curentă.";
    public const string CurrentValueLabel = "Valoare curentă: ";
    public const string EditRecipeTitle = "=== Editare rețetă ===";
    public const string EditIngredientsPrompt = "Vrei să editezi ingredientele? Scrie da pentru editare sau apasă Enter ca să le păstrezi.";
    public const string EditStepsPrompt = "Vrei să editezi pașii? Scrie da pentru editare sau apasă Enter ca să îi păstrezi.";
    public const string RecipeNameRequired = "Recipe name is required.";
    public const string RecipeSourceUrlRequired = "Source URL is required.";
    public const string RecipeSavedDateRequired = "Saved date is required.";
    public const string RecipeIngredientsRequired = "Ingredients are required.";
    public const string RecipeStepsRequired = "Steps are required.";

    public const string SaveRecipeQuestion = "Dorești să salvezi această rețetă? (Y/N)?";
    public const string InvalidSaveOption = "Opțiune invalidă. Te rog introdu Y pentru Da sau N pentru Nu.";

}