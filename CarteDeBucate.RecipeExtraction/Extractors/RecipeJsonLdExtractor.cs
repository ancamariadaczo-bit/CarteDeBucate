using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RecipeExtraction;

public class RecipeJsonLdExtractor
{
    public ExtractedRecipe? TryExtract(string html, string url)
    {
        List<string> jsonLdBlocks = ExtractJsonLdBlocks(html);

        foreach (string jsonLd in jsonLdBlocks)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(jsonLd);

                JsonElement? recipeElement = FindRecipeElement(document.RootElement);

                if (recipeElement == null)
                {
                    continue;
                }

                ExtractedRecipe recipe = CreateRecipeFromJsonLd(recipeElement.Value, url);

                if (!string.IsNullOrWhiteSpace(recipe.Name))
                {
                    return recipe;
                }
            }
            catch
            {
                // If a JSON-LD block cannot be read, we ignore it and move to the next one.
            }
        }

        return null;
    }

    private static List<string> ExtractJsonLdBlocks(string html)
    {
        List<string> blocks = new List<string>();

        MatchCollection matches = Regex.Matches(
            html,
            @"<script[^>]*type=[""']application/ld\+json[""'][^>]*>(.*?)</script>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            string json = match.Groups[1].Value.Trim();

            if (!string.IsNullOrWhiteSpace(json))
            {
                json = WebUtility.HtmlDecode(json);
                blocks.Add(json);
            }
        }

        return blocks;
    }

    private static JsonElement? FindRecipeElement(JsonElement element)
    {
        if (IsRecipeElement(element))
        {
            return element;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                JsonElement? found = FindRecipeElement(item);

                if (found != null)
                {
                    return found;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("@graph", out JsonElement graph))
            {
                JsonElement? foundInGraph = FindRecipeElement(graph);

                if (foundInGraph != null)
                {
                    return foundInGraph;
                }
            }
        }

        return null;
    }

    private static bool IsRecipeElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!element.TryGetProperty(RecipeExtractionConstants.JsonLdTypeProperty, out JsonElement typeElement))
        {
            return false;
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() == RecipeExtractionConstants.RecipeType;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement typeItem in typeElement.EnumerateArray())
            {
                if (typeItem.ValueKind == JsonValueKind.String &&
                    typeItem.GetString() == RecipeExtractionConstants.RecipeType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static ExtractedRecipe CreateRecipeFromJsonLd(JsonElement recipeElement, string url)
    {
        ExtractedRecipe recipe = new ExtractedRecipe
        {
            Name = GetStringProperty(recipeElement, RecipeExtractionConstants.NameProperty),
            SourceUrl = url.Trim(),
            Ingredients = GetStringListProperty(recipeElement, RecipeExtractionConstants.RecipeIngredientProperty),
            Steps = GetInstructions(recipeElement)
        };

        return recipe;
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return "";
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? "";
        }

        return "";
    }

    private static List<string> GetStringListProperty(JsonElement element, string propertyName)
    {
        List<string> values = new List<string>();

        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return values;
        }

        if (property.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in property.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? value = item.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value.Trim());
                    }
                }
            }
        }

        return values;
    }

    private static List<string> GetInstructions(JsonElement recipeElement)
    {
        List<string> steps = new List<string>();

        if (!recipeElement.TryGetProperty(RecipeExtractionConstants.RecipeInstructionsProperty, out JsonElement instructions))
        {
            return steps;
        }

        if (instructions.ValueKind == JsonValueKind.String)
        {
            string? text = instructions.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                steps.Add(text.Trim());
            }

            return steps;
        }

        if (instructions.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement instruction in instructions.EnumerateArray())
            {
                ExtractInstructionText(instruction, steps);
            }
        }

        return steps;
    }

    private static void ExtractInstructionText(JsonElement instruction, List<string> steps)
    {
        if (instruction.ValueKind == JsonValueKind.String)
        {
            string? text = instruction.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                steps.Add(text.Trim());
            }

            return;
        }

        if (instruction.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (instruction.TryGetProperty(RecipeExtractionConstants.TextProperty, out JsonElement textProperty) &&
            textProperty.ValueKind == JsonValueKind.String)
        {
            string? text = textProperty.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                steps.Add(text.Trim());
            }

            return;
        }

        if (instruction.TryGetProperty(RecipeExtractionConstants.NameProperty, out JsonElement nameProperty) &&
            nameProperty.ValueKind == JsonValueKind.String)
        {
            string? name = nameProperty.GetString();

            if (!string.IsNullOrWhiteSpace(name))
            {
                steps.Add(name.Trim());
            }
        }

        if (instruction.TryGetProperty(RecipeExtractionConstants.ItemListElementProperty, out JsonElement itemListElement) &&
            itemListElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in itemListElement.EnumerateArray())
            {
                ExtractInstructionText(item, steps);
            }
        }
    }
}
