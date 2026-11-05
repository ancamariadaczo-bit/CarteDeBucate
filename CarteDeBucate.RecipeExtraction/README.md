# RecipeExtractor

RecipeExtractor is a .NET library for extracting neutral recipe data from HTML and JSON-LD.

The library does not download web pages. The consuming application is responsible for downloading the HTML and passing it to the extractor together with the source URL.

## Features

- Extracts recipes from JSON-LD objects, arrays, and `@graph` collections.
- Supports `@type` represented as a string or an array.
- Extracts ingredients and instructions from JSON-LD.
- Falls back to visible HTML ingredient and instruction sections.
- Recognizes common Romanian and English section headings.
- Decodes HTML entities and normalizes whitespace.
- Detects common blocked or verification pages.
- Can optionally return a title-only result.
- Returns neutral models without application, persistence, or localization dependencies.

## Dependencies and target framework

- Targets `net10.0`.
- Depends on `HtmlAgilityPack` version `1.12.4`, installed transitively with the package.

## What this package does not do

- It does not download web pages.
- It does not persist recipes.
- It does not create application-specific recipe models.
- It does not assign identifiers, saved dates, statuses, or user ownership.
- It does not provide localized presentation messages.

## Installation

```shell
dotnet add package RecipeExtractor --version 0.1.0
```

## Usage

```csharp
using RecipeExtraction;

string html = """
    <script type="application/ld+json">
    {
        "@type": "Recipe",
        "name": "Example Recipe",
        "recipeIngredient": [
            "200 g flour",
            "100 ml water"
        ],
        "recipeInstructions": [
            "Mix the ingredients.",
            "Bake the mixture."
        ]
    }
    </script>
    """;

IRecipeExtractor extractor = new RecipeExtractor();

RecipeExtractionResult result = extractor.Extract(
    html,
    "https://example.com/recipes/example-recipe");

if (result.Success && result.Recipe is not null)
{
    Console.WriteLine(result.Recipe.Name);

    foreach (string ingredient in result.Recipe.Ingredients)
    {
        Console.WriteLine(ingredient);
    }
}
else
{
    Console.WriteLine($"Extraction failed: {result.FailureReason}");
}
```

To allow a title-only result when no structured or visible recipe content can be extracted, pass `allowTitleOnly: true`:

```csharp
RecipeExtractionResult result = extractor.Extract(
    html,
    "https://example.com/recipes/example-recipe",
    allowTitleOnly: true);
```

## Results

`RecipeExtractionResult` provides:

- `Success`: whether extraction succeeded.
- `Recipe`: the extracted neutral recipe model when successful.
- `ExtractionMethod`: `JsonLd`, `VisibleHtml`, `TitleOnly`, or `None`.
- `FailureReason`: the reason extraction failed.

`ExtractedRecipe` contains:

- `Name`
- `SourceUrl`
- `Ingredients`
- `Steps`

Application-specific values such as persistence identifiers, saved dates, statuses, and localized messages belong in the consuming application.

## License

RecipeExtractor is licensed under the MIT License.

Copyright © 2026 Anca Daczo
