public class FakeUserRepository : IUserRepository
{
    public bool AddWasCalled { get; private set; }

    public User? AddedUser { get; private set; }

    public List<User> Users { get; } = new List<User>();

    public void Add(User user)
    {
        AddWasCalled = true;
        AddedUser = user;
        Users.Add(user);
    }

    public User? GetByUsername(string username)
    {
        return Users.FirstOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));
    }
}
