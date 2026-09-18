
public interface IExtensionAuthenticationCodeService
{
    string CreateCode(string userId, string username);

    ExtensionAuthenticationCodeData? ConsumeCode(string code);
}