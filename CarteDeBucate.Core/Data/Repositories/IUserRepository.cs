public interface IUserRepository
{
    void Add(User user);
    User? GetByEmail(string email);
    User? GetById(int userId);
    long? GetSecurityVersion(int userId);
    bool IncrementSecurityVersion(int userId);
    bool UpdatePreferredCulture(int userId, string preferredCulture);
}
