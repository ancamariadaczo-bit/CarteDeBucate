public class FakeUserRepository : IUserRepository
{
    public bool AddWasCalled { get; private set; }

    public User? AddedUser { get; private set; }

    public List<User> Users { get; } = new List<User>();

    public void Add(User user)
    {
        user.Email = EmailNormalizer.Normalize(user.Email);

        if (user.Id == 0)
        {
            user.Id = Users.Count == 0
                ? 1
                : Users.Max(existingUser => existingUser.Id) + 1;
        }

        AddWasCalled = true;
        AddedUser = user;
        Users.Add(user);
    }

    public User? GetByEmail(string email)
    {
        string normalizedEmail = EmailNormalizer.Normalize(email);

        return Users.FirstOrDefault(user =>
            string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal));
    }

    public User? GetById(int userId)
    {
        return Users.FirstOrDefault(user => user.Id == userId);
    }

    public long? GetSecurityVersion(int userId)
    {
        return GetById(userId)?.SecurityVersion;
    }

    public bool IncrementSecurityVersion(int userId)
    {
        User? user = GetById(userId);

        if (user is null)
        {
            return false;
        }

        user.SecurityVersion++;
        return true;
    }

    public bool UpdatePreferredCulture(int userId, string preferredCulture)
    {
        User? user = GetById(userId);

        if (user is null)
        {
            return false;
        }

        user.PreferredCulture = preferredCulture;
        return true;
    }
}
