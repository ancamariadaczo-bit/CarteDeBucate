public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Email { get; }

    void SetCurrentUser(User user);
    void Clear();
}
