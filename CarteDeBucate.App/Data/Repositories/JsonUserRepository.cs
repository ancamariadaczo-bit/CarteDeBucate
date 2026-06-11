using System.Text.Json;

public class JsonUserRepository : IUserRepository
{
    private readonly string _filePath;

    public JsonUserRepository(string filePath)
    {
        _filePath = filePath;
    }

    public void Add(User user)
    {
        List<User> users = LoadUsers();

        users.Add(user);

        SaveUsers(users);
    }

    public User? GetByUsername(string username)
    {
        List<User> users = LoadUsers();

        return users.FirstOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    private List<User> LoadUsers()
    {
        if (!File.Exists(_filePath))
        {
            return new List<User>();
        }

        string json = File.ReadAllText(_filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<User>();
        }

        return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
    }

    private void SaveUsers(List<User> users)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(users, options);

        File.WriteAllText(_filePath, json);
    }
}