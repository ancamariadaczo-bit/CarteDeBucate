using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

public class RecipeHtmlImporter
{
    public Recipe? TryImport(string html, string url)
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

    public static string ExtractTitle(string html)
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

    private static string ExtractMainTitle(HtmlDocument document)
    {
        HtmlNode? h1 = document.DocumentNode.SelectSingleNode(RecipeImportConstants.H1Tag);

        if (h1 == null)
        {
            return "";
        }

        return CleanText(h1.InnerText);
    }

    private static List<string> ExtractSectionItems(HtmlDocument document, string[] startKeywords, string[] stopKeywords)
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

    private static HtmlNode? FindSectionStartNode(HtmlDocument document, string[] keywords)
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

    private static bool IsStopNode(HtmlNode node, string text, string[] stopKeywords)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        bool isHeadingLikeNode = RecipeImportConstants.HeadingLikeTags.Contains(node.Name);

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

    private static void AddItemsFromNode(HtmlNode node, List<string> items)
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

    private static bool ContainsAnyKeyword(string text, string[] keywords)
    {
        string normalizedText = TextHelper.NormalizeForSearch(text);

        return keywords.Any(keyword =>
            normalizedText.Contains(TextHelper.NormalizeForSearch(keyword)));
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        string decoded = WebUtility.HtmlDecode(text);
        decoded = Regex.Replace(decoded, @"\s+", " ");

        return decoded.Trim();
    }
}
