using System.Security.Claims;
using CarteDeBucate.Web.Configuration;
using CarteDeBucate.Web.Services.Authentication;
using Microsoft.AspNetCore.Http;

public class HttpCurrentUserContextTests
{
    [Fact]
    public void NewContext_WhenRequestHasNoAuthenticatedUser_ShouldNotBeAuthenticated()
    {
        HttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        HttpCurrentUserContext context = CreateContext(httpContextAccessor);

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Username);
    }

    [Fact]
    public void Context_WhenRequestHasAuthenticatedUser_ShouldReadUserClaims()
    {
        HttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContextWithUser("12", "anca")
        };
        HttpCurrentUserContext context = CreateContext(httpContextAccessor);

        Assert.True(context.IsAuthenticated);
        Assert.Equal(12, context.UserId);
        Assert.Equal("anca", context.Username);
    }

    [Fact]
    public void SetCurrentUser_ShouldStoreUserIdentityForCurrentRequest()
    {
        HttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        HttpCurrentUserContext context = CreateContext(httpContextAccessor);

        context.SetCurrentUser(new User
        {
            Id = 21,
            Username = "maria"
        });

        Assert.True(context.IsAuthenticated);
        Assert.Equal(21, context.UserId);
        Assert.Equal("maria", context.Username);
    }

    [Fact]
    public void Clear_ShouldRemoveCurrentRequestUser()
    {
        HttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        HttpCurrentUserContext context = CreateContext(httpContextAccessor);
        context.SetCurrentUser(new User
        {
            Id = 21,
            Username = "maria"
        });

        context.Clear();

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Username);
    }

    [Fact]
    public void Context_WhenAuthenticationIsDisabled_ShouldIgnoreAuthenticatedUserClaims()
    {
        HttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContextWithUser("12", "anca")
        };
        HttpCurrentUserContext context = CreateContext(
            httpContextAccessor,
            authenticationEnabled: false);

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Username);
    }

    [Fact]
    public void SetCurrentUser_WhenAuthenticationIsDisabled_ShouldNotExposeUserIdentity()
    {
        HttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        HttpCurrentUserContext context = CreateContext(
            httpContextAccessor,
            authenticationEnabled: false);

        context.SetCurrentUser(new User
        {
            Id = 21,
            Username = "maria"
        });

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.Username);
    }

    private static HttpCurrentUserContext CreateContext(
        IHttpContextAccessor httpContextAccessor,
        bool authenticationEnabled = true)
    {
        return new HttpCurrentUserContext(
            httpContextAccessor,
            new WebAppSettings
            {
                AuthenticationEnabled = authenticationEnabled
            });
    }

    private static HttpContext CreateHttpContextWithUser(string userId, string username)
    {
        Claim[] claims =
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "Test");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);
        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            User = user
        };

        return httpContext;
    }
}
