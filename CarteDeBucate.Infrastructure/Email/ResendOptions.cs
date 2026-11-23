public sealed class ResendOptions
{
    public string ApiToken { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public TimeSpan DeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
