
public interface IExtensionAuthenticationCodeService
{
    string CreateCode(int userId, long securityVersion);

    ExtensionAuthenticationCodeData? ConsumeCode(string code);
}
