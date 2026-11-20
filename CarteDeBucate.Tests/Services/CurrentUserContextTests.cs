public class CurrentUserContextTests
{
    [Fact]
    public void NewContext_ShouldNotBeAuthenticated()
    {
        CurrentUserContext context = new CurrentUserContext();

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Email);
    }

    [Fact]
    public void SetCurrentUser_ShouldStoreUserIdentity()
    {
        CurrentUserContext context = new CurrentUserContext();

        context.SetCurrentUser(new User
        {
            Id = 12,
            Email = "anca@example.com"
        });

        Assert.True(context.IsAuthenticated);
        Assert.Equal(12, context.UserId);
        Assert.Equal("anca@example.com", context.Email);
    }

    [Fact]
    public void Clear_ShouldRemoveCurrentUser()
    {
        CurrentUserContext context = new CurrentUserContext();
        context.SetCurrentUser(new User
        {
            Id = 12,
            Email = "anca@example.com"
        });

        context.Clear();

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Email);
    }
}
