public interface IUserRepository
{
    void Add(User user);
    User? GetByEmail(string email);
    User? GetById(int userId);
}
