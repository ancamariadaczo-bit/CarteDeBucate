using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;

public sealed class AccountRegistrationService : IAccountRegistrationService
{
    public const int MinimumPasswordLength = 12;

    private const int TokenSize = 32;
    private const string ConfirmationPath = "/Account/VerifyEmail";
    private const string FallbackCulture = "en-US";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IAccountRepository _accountRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly IAccountLinkBuilder _linkBuilder;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public AccountRegistrationService(
        IAccountRepository accountRepository,
        PasswordHasher passwordHasher,
        IAccountLinkBuilder linkBuilder,
        IEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(linkBuilder);
        ArgumentNullException.ThrowIfNull(templateRenderer);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _linkBuilder = linkBuilder;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
    }

    public async Task<AccountRegistrationServiceResult> RegisterAsync(
        AccountRegistrationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        AccountRegistrationServiceResult? validationResult = Validate(input);

        if (validationResult is not null)
        {
            return validationResult;
        }

        string normalizedEmail = EmailNormalizer.Normalize(input.Email!);
        string password = input.Password!;
        string culture = ResolveCulture(input.Culture);
        DateTime issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        byte[] rawTokenBytes = RandomNumberGenerator.GetBytes(TokenSize);
        string rawToken = WebEncoders.Base64UrlEncode(rawTokenBytes);
        byte[] tokenHash = SHA256.HashData(rawTokenBytes);
        (string passwordHash, string passwordSalt) =
            _passwordHasher.HashPassword(password);

        EmailTokenIssuance token = new(
            EmailTokenPurpose.RegistrationVerification,
            tokenHash,
            normalizedEmail,
            culture,
            issuedAtUtc,
            issuedAtUtc.Add(TokenLifetime));

        AccountRegistrationResult persistenceResult = _accountRepository.Register(
            new AccountRegistrationRequest(
                normalizedEmail,
                passwordHash,
                passwordSalt,
                issuedAtUtc,
                PreferredCulture: null,
                VerificationToken: token));

        if (persistenceResult.Code == AccountRegistrationResultCode.DuplicateEmail)
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.DuplicateEmail);
        }

        string confirmationUrl =
            _linkBuilder.BuildConfirmationUrl(ConfirmationPath, rawToken);
        RenderedEmailTemplate template = _templateRenderer.Render(
            new EmailTemplateRenderRequest(
                EmailTemplateId.RegistrationVerification,
                culture,
                confirmationUrl));
        EmailMessage message = new(
            normalizedEmail,
            template.Subject,
            template.HtmlBody,
            template.PlainTextBody,
            CreateIdempotencyKey(tokenHash));

        try
        {
            await _emailSender.SendAsync(message, cancellationToken);
        }
        catch (EmailDeliveryException)
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.DeliveryFailed);
        }

        return new AccountRegistrationServiceResult(
            AccountRegistrationServiceResultCode.Success);
    }

    private static AccountRegistrationServiceResult? Validate(
        AccountRegistrationInput input)
    {
        if (!EmailNormalizer.TryNormalize(input.Email, out string normalizedEmail) ||
            !MailAddress.TryCreate(normalizedEmail, out MailAddress? mailAddress) ||
            !string.Equals(
                mailAddress.Address,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.InvalidEmail);
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.PasswordRequired);
        }

        if (input.Password.Length < MinimumPasswordLength)
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.PasswordTooShort);
        }

        if (string.IsNullOrWhiteSpace(input.PasswordConfirmation))
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.PasswordConfirmationRequired);
        }

        if (!string.Equals(
                input.Password,
                input.PasswordConfirmation,
                StringComparison.Ordinal))
        {
            return new AccountRegistrationServiceResult(
                AccountRegistrationServiceResultCode.PasswordConfirmationMismatch);
        }

        return null;
    }

    private static string ResolveCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return FallbackCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(culture).Name;
        }
        catch (CultureNotFoundException)
        {
            return FallbackCulture;
        }
    }

    private static string CreateIdempotencyKey(byte[] tokenHash)
    {
        return $"registration-verification-{Convert.ToHexString(tokenHash)}";
    }
}
