using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

public class RecipeJsonLdImporter
{
    public Recipe? TryImport(string html, string url)
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

                Recipe recipe = CreateRecipeFromJsonLd(recipeElement.Value, url);

                if (!string.IsNullOrWhiteSpace(recipe.Name))
                {
                    return recipe;
                }
            }
            catch
            {
                // Daca un bloc JSON-LD nu poate fi citit, il ignoram si trecem la urmatorul.
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

        if (!element.TryGetProperty(RecipeImportConstants.JsonLdTypeProperty, out JsonElement typeElement))
        {
            return false;
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() == RecipeImportConstants.RecipeType;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement typeItem in typeElement.EnumerateArray())
            {
                if (typeItem.ValueKind == JsonValueKind.String &&
                    typeItem.GetString() == RecipeImportConstants.RecipeType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Recipe CreateRecipeFromJsonLd(JsonElement recipeElement, string url)
    {
        Recipe recipe = new Recipe
        {
            SourceUrl = url.Trim(),
            SavedAt = DateTime.Now
        };

        recipe.Name = GetStringProperty(recipeElement, RecipeImportConstants.NameProperty);
        recipe.Ingredients = GetStringListProperty(recipeElement, RecipeImportConstants.RecipeIngredientProperty);
        recipe.Steps = GetInstructions(recipeElement);

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

        if (!recipeElement.TryGetProperty(RecipeImportConstants.RecipeInstructionsProperty, out JsonElement instructions))
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

        if (instruction.TryGetProperty(RecipeImportConstants.TextProperty, out JsonElement textProperty) &&
            textProperty.ValueKind == JsonValueKind.String)
        {
            string? text = textProperty.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                steps.Add(text.Trim());
            }

            return;
        }

        if (instruction.TryGetProperty(RecipeImportConstants.NameProperty, out JsonElement nameProperty) &&
            nameProperty.ValueKind == JsonValueKind.String)
        {
            string? name = nameProperty.GetString();

            if (!string.IsNullOrWhiteSpace(name))
            {
                steps.Add(name.Trim());
            }
        }

        if (instruction.TryGetProperty(RecipeImportConstants.ItemListElementProperty, out JsonElement itemListElement) &&
            itemListElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in itemListElement.EnumerateArray())
            {
                ExtractInstructionText(item, steps);
            }
        }
    }
}
