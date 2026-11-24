using System.Resources;

namespace CarteDeBucate.Web.Resources;

public static class ValidationMessages
{
    private static readonly ResourceManager ResourceManager = new(
        "CarteDeBucate.Web.Resources.ValidationMessages",
        typeof(ValidationMessages).Assembly);

    public static string UsernameRequired => Get(nameof(UsernameRequired));
    public static string UsernameLength => Get(nameof(UsernameLength));
    public static string UsernameCharacters => Get(nameof(UsernameCharacters));
    public static string EmailRequired => Get(nameof(EmailRequired));
    public static string EmailInvalid => Get(nameof(EmailInvalid));
    public static string EmailLength => Get(nameof(EmailLength));
    public static string PasswordRequired => Get(nameof(PasswordRequired));
    public static string PasswordLength => Get(nameof(PasswordLength));
    public static string RegistrationPasswordLength =>
        Get(nameof(RegistrationPasswordLength));
    public static string ConfirmPasswordRequired => Get(nameof(ConfirmPasswordRequired));
    public static string PasswordMismatch => Get(nameof(PasswordMismatch));
    public static string CultureRequired => Get(nameof(CultureRequired));
    public static string CultureInvalid => Get(nameof(CultureInvalid));
    public static string CurrentPasswordInvalid => Get(nameof(CurrentPasswordInvalid));
    public static string EmailAlreadyInUse => Get(nameof(EmailAlreadyInUse));

    private static string Get(string name) => ResourceManager.GetString(name) ?? name;
}
