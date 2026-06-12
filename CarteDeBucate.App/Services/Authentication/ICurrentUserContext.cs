public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Username { get; }

    void SetCurrentUser(User user);
    void Clear();
}