using System.Security.Claims;
using CarteDeBucate.Web.Configuration;

namespace CarteDeBucate.Web.Services.Authentication;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WebAppSettings _settings;
    private User? _currentRequestUser;

    public HttpCurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        WebAppSettings settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings;
    }

    public bool IsAuthenticated =>
        _settings.AuthenticationEnabled
            && (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
                || _currentRequestUser is not null);

    public int? UserId
    {
        get
        {
            if (!_settings.AuthenticationEnabled)
            {
                return null;
            }

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
        !_settings.AuthenticationEnabled
            ? null
            : _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
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
