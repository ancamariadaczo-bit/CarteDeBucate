using System.Globalization;
using System.Resources;

namespace CarteDeBucate.Web.Resources;

internal static class EmailTemplateResources
{
    private static readonly ResourceManager ResourceManager = new(
        "CarteDeBucate.Web.Resources.EmailTemplates",
        typeof(EmailTemplateResources).Assembly);

    public static string Get(string name, CultureInfo culture)
    {
        return ResourceManager.GetString(name, culture) ??
            throw new InvalidOperationException($"The email template resource '{name}' is missing.");
    }
}
