namespace CarteDeBucate.Web.Models;

public sealed class SettingsIndexViewModel
{
    public string CurrentEmail { get; set; } = string.Empty;

    public PreferredCultureViewModel PreferredCulture { get; set; } = new();

    public EmailChangeViewModel EmailChange { get; set; } = new();
}
