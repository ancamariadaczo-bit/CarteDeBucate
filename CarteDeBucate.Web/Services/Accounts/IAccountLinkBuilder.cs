public interface IAccountLinkBuilder
{
    string BuildConfirmationUrl(string relativePath, string token);
}
