public static class RecipeImportConstants
{
    public const string JsonLdScriptType = "application/ld+json";

    public const string JsonLdTypeProperty = "@type";
    public const string JsonLdGraphProperty = "@graph";

    public const string RecipeType = "Recipe";

    public const string NameProperty = "name";
    public const string RecipeIngredientProperty = "recipeIngredient";
    public const string RecipeInstructionsProperty = "recipeInstructions";
    public const string TextProperty = "text";
    public const string ItemListElementProperty = "itemListElement";

    public const string TitleTag = "title";
    public const string H1Tag = "h1";

    public static readonly string[] SectionCandidateTags =
{
    "h2",
    "h3",
    "h4",
    "strong",
    "b",
    "p",
    "div"
};

public static readonly string[] HeadingLikeTags =
{
    "h1",
    "h2",
    "h3",
    "h4",
    "strong",
    "b"
};
}