public class GlobalSessionInvalidationServiceTests
{
    [Fact]
    public void InvalidateAllSessions_WhenUserExists_ShouldAdvanceOnlyThatUsersSecurityVersion()
    {
        FakeUserRepository userRepository = new();
        User targetUser = new() { Id = 7, SecurityVersion = 3 };
        User otherUser = new() { Id = 8, SecurityVersion = 4 };
        userRepository.Users.AddRange([targetUser, otherUser]);
        GlobalSessionInvalidationService service = new(userRepository);

        bool wasInvalidated = service.InvalidateAllSessions(targetUser.Id);

        Assert.True(wasInvalidated);
        Assert.Equal(4L, targetUser.SecurityVersion);
        Assert.Equal(4L, otherUser.SecurityVersion);
    }

    [Fact]
    public void InvalidateAllSessions_WhenUserDoesNotExist_ShouldReturnFalse()
    {
        FakeUserRepository userRepository = new();
        GlobalSessionInvalidationService service = new(userRepository);

        bool wasInvalidated = service.InvalidateAllSessions(7);

        Assert.False(wasInvalidated);
    }
}
