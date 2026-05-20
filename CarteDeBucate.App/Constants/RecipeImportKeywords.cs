public static class RecipeImportKeywords
{
    public static readonly string[] RecipePageKeywords =
    {
        // Romanian
        "ingrediente",
        "ingredient",
        "preparare",
        "mod de preparare",
        "instructiuni",
        "instrucțiuni",
        "pasi",
        "pași",
        "pregatire",
        "pregătire",
        "reteta",
        "rețeta",

        // English
        "recipe",
        "ingredients",
        "ingredient",
        "instructions",
        "directions",
        "method",
        "preparation",
        "prep",
        "steps",
        "how to make",
        "cook",
        "cooking",
        "bake",
        "baking",

        // Structured data / JSON-LD
        "recipeingredient",
        "recipeinstructions",
        "cooktime",
        "preptime",
        "totaltime",
        "recipeyield"
    };

    public static readonly string[] IngredientSectionStartKeywords =
    {
        // Romanian
        "ingrediente",

        // English
        "ingredients",
        "ingredient"
    };

    public static readonly string[] IngredientSectionStopKeywords =
    {
        // Romanian
        "preparare",
        "mod de preparare",
        "instructiuni",
        "instrucțiuni",
        "pasi",
        "pași",
        "pregatire",
        "pregătire",

        // English
        "instructions",
        "directions",
        "method",
        "preparation",
        "steps",
        "how to make",
        "cook",
        "cooking",
        "bake",
        "baking"
    };

    public static readonly string[] StepsSectionStartKeywords =
    {
        // Romanian
        "preparare",
        "mod de preparare",
        "instructiuni",
        "instrucțiuni",
        "pasi",
        "pași",
        "pregatire",
        "pregătire",

        // English
        "instructions",
        "directions",
        "method",
        "preparation",
        "steps",
        "how to make",
        "how to prepare",
        "how to cook",
        "make it",
        "directions"
    };

    public static readonly string[] StepsSectionStopKeywords =
    {
        // Romanian
        "pofta buna",
        "poftă bună",
        "note",
        "comentarii",
        "alte retete",
        "alte rețete",

        // English
        "notes",
        "tips",
        "nutrition",
        "nutritional information",
        "comments",
        "related recipes",
        "more recipes",
        "you may also like",
        "did you make this recipe",
        "leave a comment",
        "recipe notes",
        "storage",
        "serving suggestions"
    };

    public static readonly string[] RecipeUrlKeywords =
{
    "reteta",
    "retete",
    "recipe",
    "recipes",
    "food",
    "cooking",
    "baking"
};
    public static readonly string[] BlockedPageKeywords =
    {
    "please wait while your request is being verified",
    "checking your browser before accessing",
    "verify you are human",
    "checking if the site connection is secure",
    "cf-browser-verification",
    "cloudflare ray id"
};
    public static readonly string[] StrongBlockedPageKeywords =
    {
    "please wait while your request is being verified",
    "checking your browser before accessing",
    "checking if the site connection is secure",
    "verify you are human"
};

    public static readonly string[] CloudflareBlockedPageKeywords =
    {
    "cloudflare ray id",
    "attention required",
    "access denied",
    "error 1020"
};

    public const int BlockedPageMinimumKeywordScore = 2;

    public const string CloudflareRayIdKeyword = "cloudflare ray id";
}