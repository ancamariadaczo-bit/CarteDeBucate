using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

public class RecipeImporter : IRecipeImporter
{
    private readonly HttpClient _httpClient;

    public RecipeImporter()
        : this(CreateDefaultHttpClient())
    {
    }

    public RecipeImporter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        HttpClient httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; CarteDeBucateApp/1.0)");

        return httpClient;
    }

    public async Task<RecipeImportResult> ImportFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new RecipeImportResult
            {
                Success = false,
                Message = AppTexts.ImportFailedEmptyUrl
            };
        }

        url = url.Trim();

        if (!UrlValidator.IsValidHttpUrl(url))
        {
            return new RecipeImportResult
            {
                Success = false,
                Message = AppTexts.ImportFailedInvalidUrl
            };
        }

        try
        {
            string html = await _httpClient.GetStringAsync(url);

            if (LooksLikeBlockedPage(html))
            {
                return new RecipeImportResult
                {
                    Success = false,
                    Message = AppTexts.ImportFailedBlockedPage
                };
            }

            Recipe? recipeFromJsonLd = TryExtractRecipeFromJsonLd(html, url);

            if (recipeFromJsonLd != null && LooksLikeRecipePage(html, url, recipeFromJsonLd))
            {
                return new RecipeImportResult
                {
                    Success = true,
                    Recipe = recipeFromJsonLd,
                    Message = AppTexts.ImportSuccessJsonLd
                };
            }

            Recipe? recipeFromVisibleHtml = TryExtractRecipeFromVisibleHtml(html, url);

            if (recipeFromVisibleHtml != null && LooksLikeRecipePage(html, url, recipeFromVisibleHtml))
            {
                return new RecipeImportResult
                {
                    Success = true,
                    Recipe = recipeFromVisibleHtml,
                    Message = AppTexts.ImportSuccessVisibleHtml
                };
            }

            string title = ExtractTitle(html);

            Recipe titleOnlyRecipe = new Recipe
            {
                Name = string.IsNullOrWhiteSpace(title)
                    ? AppTexts.ImportedTitleNotFound
                    : title,

                SourceUrl = url.Trim(),
                SavedAt = DateTime.Now
            };

            if (!LooksLikeRecipePage(html, url, titleOnlyRecipe))
            {
                return new RecipeImportResult
                {
                    Success = false,
                    Message = AppTexts.ImportFailedNotRecipePage
                };
            }

            return new RecipeImportResult
            {
                Success = true,
                Recipe = titleOnlyRecipe,
                Message = AppTexts.ImportSuccessTitleOnly
            };
        }
        catch
        {
            return new RecipeImportResult
            {
                Success = false,
                Message = AppTexts.ImportFailedCouldNotDownloadPage
            };
        }
    }

    private Recipe? TryExtractRecipeFromJsonLd(string html, string url)
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
                // Dacă un bloc JSON-LD nu poate fi citit, îl ignorăm și trecem la următorul.
            }
        }

        return null;
    }

    private List<string> ExtractJsonLdBlocks(string html)
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

    private JsonElement? FindRecipeElement(JsonElement element)
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

    private bool IsRecipeElement(JsonElement element)
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

    private Recipe CreateRecipeFromJsonLd(JsonElement recipeElement, string url)
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

    private string GetStringProperty(JsonElement element, string propertyName)
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

    private List<string> GetStringListProperty(JsonElement element, string propertyName)
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

    private List<string> GetInstructions(JsonElement recipeElement)
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

    private void ExtractInstructionText(JsonElement instruction, List<string> steps)
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

    private string ExtractTitle(string html)
    {
        Match match = Regex.Match(
            html,
            @"<title>\s*(.*?)\s*</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
        {
            return "";
        }

        string title = match.Groups[1].Value;

        title = WebUtility.HtmlDecode(title);
        title = Regex.Replace(title, @"\s+", " ").Trim();

        return title;
    }

    private Recipe? TryExtractRecipeFromVisibleHtml(string html, string url)
    {
        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(html);

        string title = ExtractMainTitle(document);

        if (string.IsNullOrWhiteSpace(title))
        {
            title = ExtractTitle(html);
        }

        List<string> ingredients = ExtractSectionItems(
            document,
            startKeywords: RecipeImportKeywords.IngredientSectionStartKeywords,
            stopKeywords: RecipeImportKeywords.IngredientSectionStopKeywords);

        List<string> steps = ExtractSectionItems(
            document,
            startKeywords: RecipeImportKeywords.StepsSectionStartKeywords,
            stopKeywords: RecipeImportKeywords.StepsSectionStopKeywords);

        if (ingredients.Count == 0 && steps.Count == 0)
        {
            return null;
        }

        return new Recipe
        {
            Name = string.IsNullOrWhiteSpace(title)
                ? AppTexts.ImportedTitleNotFound
                : title,

            SourceUrl = url.Trim(),
            Ingredients = ingredients,
            Steps = steps,
            SavedAt = DateTime.Now
        };
    }

    private string ExtractMainTitle(HtmlDocument document)
    {
        HtmlNode? h1 = document.DocumentNode.SelectSingleNode(RecipeImportConstants.H1Tag);

        if (h1 == null)
        {
            return "";
        }

        return CleanText(h1.InnerText);
    }

    private List<string> ExtractSectionItems(HtmlDocument document, string[] startKeywords, string[] stopKeywords)
    {
        List<string> items = new List<string>();

        HtmlNode? startNode = FindSectionStartNode(document, startKeywords);

        if (startNode == null)
        {
            return items;
        }

        HtmlNode? currentNode = startNode.NextSibling;

        while (currentNode != null)
        {
            string currentText = CleanText(currentNode.InnerText);

            if (IsStopNode(currentNode, currentText, stopKeywords))
            {
                break;
            }

            AddItemsFromNode(currentNode, items);

            currentNode = currentNode.NextSibling;
        }

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private HtmlNode? FindSectionStartNode(HtmlDocument document, string[] keywords)
    {
        IEnumerable<HtmlNode> candidateNodes = document.DocumentNode
            .Descendants()
            .Where(node => RecipeImportConstants.SectionCandidateTags.Contains(node.Name));

        foreach (HtmlNode node in candidateNodes)
        {
            string text = CleanText(node.InnerText);

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (text.Length > 80)
            {
                continue;
            }

            if (ContainsAnyKeyword(text, keywords))
            {
                return node;
            }
        }

        return null;
    }

    private bool IsStopNode(HtmlNode node, string text, string[] stopKeywords)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        bool isHeadingLikeNode = RecipeImportConstants.HeadingLikeTags.Contains(node.Name); ;

        if (isHeadingLikeNode && ContainsAnyKeyword(text, stopKeywords))
        {
            return true;
        }

        if (text.Length <= 80 && ContainsAnyKeyword(text, stopKeywords))
        {
            return true;
        }

        return false;
    }

    private void AddItemsFromNode(HtmlNode node, List<string> items)
    {
        IEnumerable<HtmlNode> listItems = node.SelectNodes(".//li") ?? Enumerable.Empty<HtmlNode>();

        foreach (HtmlNode listItem in listItems)
        {
            string text = CleanText(listItem.InnerText);

            if (!string.IsNullOrWhiteSpace(text))
            {
                items.Add(text);
            }
        }

        if (listItems.Any())
        {
            return;
        }

        string htmlWithLineBreaks = Regex.Replace(
            node.InnerHtml,
            @"<br\s*/?>",
            "\n",
            RegexOptions.IgnoreCase);

        string plainText = HtmlEntity.DeEntitize(Regex.Replace(htmlWithLineBreaks, "<.*?>", " "));

        string[] lines = plainText
            .Split('\n')
            .Select(CleanText)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        foreach (string line in lines)
        {
            if (line.Length > 2)
            {
                items.Add(line);
            }
        }
    }

    private bool ContainsAnyKeyword(string text, string[] keywords)
    {
        string normalizedText = TextHelper.NormalizeForSearch(text);

        return keywords.Any(keyword =>
            normalizedText.Contains(TextHelper.NormalizeForSearch(keyword)));
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        string decoded = WebUtility.HtmlDecode(text);
        decoded = Regex.Replace(decoded, @"\s+", " ");

        return decoded.Trim();
    }

    private bool LooksLikeRecipePage(string html, string url, Recipe recipe)
    {
        int score = 0;

        bool hasValidName =
            !string.IsNullOrWhiteSpace(recipe.Name) &&
            recipe.Name != AppTexts.ImportedTitleNotFound;

        if (hasValidName)
        {
            score += 1;
        }

        if (recipe.Ingredients.Count >= 2)
        {
            score += 4;
        }

        if (recipe.Steps.Count >= 2)
        {
            score += 4;
        }

        string normalizedHtml = TextHelper.NormalizeForSearch(html);
        string normalizedUrl = TextHelper.NormalizeForSearch(url);

        if (ContainsAny(normalizedUrl, RecipeImportKeywords.RecipeUrlKeywords))
        {
            score += 2;
        }

        if (ContainsAny(normalizedHtml, RecipeImportKeywords.IngredientSectionStartKeywords))
        {
            score += 2;
        }

        if (ContainsAny(normalizedHtml, RecipeImportKeywords.StepsSectionStartKeywords))
        {
            score += 2;
        }

        bool hasJsonLdRecipe =
            normalizedHtml.Contains("application/ld+json") &&
            normalizedHtml.Contains("recipe");

        if (hasJsonLdRecipe)
        {
            score += 3;
        }

        return score >= 5;
    }

    private bool ContainsAny(string normalizedText, string[] keywords)
    {
        return keywords.Any(keyword =>
            normalizedText.Contains(TextHelper.NormalizeForSearch(keyword)));
    }

    private bool LooksLikeBlockedPage(string html)
    {
        string normalizedHtml = TextHelper.NormalizeForSearch(html);

        bool hasStrongVerificationText = ContainsAny(
            normalizedHtml,
            RecipeImportKeywords.StrongBlockedPageKeywords);

        bool hasCloudflareRayId = normalizedHtml.Contains(
            TextHelper.NormalizeForSearch(RecipeImportKeywords.CloudflareRayIdKeyword));

        bool hasCloudflareBlockSignal = ContainsAny(
            normalizedHtml,
            RecipeImportKeywords.CloudflareBlockedPageKeywords);

        int matchedBlockedKeywordsCount =
            CountMatches(normalizedHtml, RecipeImportKeywords.StrongBlockedPageKeywords) +
            CountMatches(normalizedHtml, RecipeImportKeywords.CloudflareBlockedPageKeywords);

        return
            hasStrongVerificationText ||
            (hasCloudflareRayId && hasCloudflareBlockSignal) ||
            matchedBlockedKeywordsCount >= RecipeImportKeywords.BlockedPageMinimumKeywordScore;
    }

    private int CountMatches(string normalizedText, string[] keywords)
    {
        int count = 0;

        foreach (string keyword in keywords)
        {
            string normalizedKeyword = TextHelper.NormalizeForSearch(keyword);

            if (normalizedText.Contains(normalizedKeyword))
            {
                count++;
            }
        }

        return count;
    }
}