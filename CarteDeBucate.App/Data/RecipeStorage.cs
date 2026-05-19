using System.Text.Json;

public class RecipeStorage
{
    private readonly string _filePath;

    public RecipeStorage(string filePath)
    {
        _filePath = filePath;
    }

    public List<Recipe> LoadRecipes()
    {
        if (!File.Exists(_filePath))
        {
            return new List<Recipe>();
        }

        string json = File.ReadAllText(_filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Recipe>();
        }

        return JsonSerializer.Deserialize<List<Recipe>>(json) ?? new List<Recipe>();
    }

    public void SaveRecipes(List<Recipe> recipes)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(recipes, options);

        File.WriteAllText(_filePath, json);
    }
}