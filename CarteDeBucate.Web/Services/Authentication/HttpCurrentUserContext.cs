using System.Security.Claims;

namespace CarteDeBucate.Web.Services.Authentication;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private User? _currentRequestUser;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
            || _currentRequestUser is not null;

    public int? UserId
    {
        get
        {
            string? userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return _currentRequestUser?.Id;
        }
    }

    public string? Username =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
            ?? _currentRequestUser?.Username;

    public void SetCurrentUser(User user)
    {
        _currentRequestUser = user;
    }

    public void Clear()
    {
        _currentRequestUser = null;
    }
}
