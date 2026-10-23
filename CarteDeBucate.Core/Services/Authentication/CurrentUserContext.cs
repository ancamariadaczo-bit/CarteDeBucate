public class CurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => UserId.HasValue;

    public int? UserId { get; private set; }

    public string? Username { get; private set; }

    public void SetCurrentUser(User user)
    {
        UserId = user.Id;
        Username = user.Username;
    }

    public void Clear()
    {
        UserId = null;
        Username = null;
    }
}