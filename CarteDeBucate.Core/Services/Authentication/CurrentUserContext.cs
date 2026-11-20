public class CurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => UserId.HasValue;

    public int? UserId { get; private set; }

    public string? Email { get; private set; }

    public void SetCurrentUser(User user)
    {
        UserId = user.Id;
        Email = user.Email;
    }

    public void Clear()
    {
        UserId = null;
        Email = null;
    }
}
