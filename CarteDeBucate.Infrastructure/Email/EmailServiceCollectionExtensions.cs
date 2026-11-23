using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddResendEmailDelivery(
        this IServiceCollection services,
        ResendOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(Options.Create(options));
        services.AddResend(resendOptions => resendOptions.ApiToken = options.ApiToken);
        services.AddTransient<IEmailSender, ResendEmailSender>();

        return services;
    }
}
