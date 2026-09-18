using System.Collections.Concurrent;
using System.Security.Cryptography;

public class ExtensionAuthenticationCodeService
    : IExtensionAuthenticationCodeService
{
    private readonly TimeProvider _timeProvider;

    private readonly ConcurrentDictionary<
        string,
        ExtensionAuthenticationCodeData> _codes = new();

    public ExtensionAuthenticationCodeService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string CreateCode(string userId, string username)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (KeyValuePair<string, ExtensionAuthenticationCodeData> entry in _codes)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _codes.TryRemove(entry);
            }
        }

        string code = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        _codes[code] = new ExtensionAuthenticationCodeData
        {
            UserId = userId,
            Username = username,
            ExpiresAt = now.AddMinutes(2)
        };

        return code;
    }

    public ExtensionAuthenticationCodeData? ConsumeCode(string code)
    {
        if (!_codes.TryRemove(code, out ExtensionAuthenticationCodeData? data))
        {
            return null;
        }

        if (data.ExpiresAt <= _timeProvider.GetUtcNow())
        {
            return null;
        }

        return data;
    }
}
