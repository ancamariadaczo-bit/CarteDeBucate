
public interface IExtensionAuthenticationCodeService
{
    string CreateCode(int userId);

    ExtensionAuthenticationCodeData? ConsumeCode(string code);
}
