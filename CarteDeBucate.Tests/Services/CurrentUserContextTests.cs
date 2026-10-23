public class CurrentUserContextTests
{
    [Fact]
    public void NewContext_ShouldNotBeAuthenticated()
    {
        CurrentUserContext context = new CurrentUserContext();

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Username);
    }

    [Fact]
    public void SetCurrentUser_ShouldStoreUserIdentity()
    {
        CurrentUserContext context = new CurrentUserContext();

        context.SetCurrentUser(new User
        {
            Id = 12,
            Username = "anca"
        });

        Assert.True(context.IsAuthenticated);
        Assert.Equal(12, context.UserId);
        Assert.Equal("anca", context.Username);
    }

    [Fact]
    public void Clear_ShouldRemoveCurrentUser()
    {
        CurrentUserContext context = new CurrentUserContext();
        context.SetCurrentUser(new User
        {
            Id = 12,
            Username = "anca"
        });

        context.Clear();

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Username);
    }
}
