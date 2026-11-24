using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;

public sealed class AccountRegistrationService : IAccountRegistrationService
{
    public const int MinimumPasswordLength = 12;

    private const int TokenSize = 32;
    private const int EncodedTokenLength = 43;
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

    public AccountEmailVerificationReviewResult ReviewEmailVerification(
        string? token)
    {
        if (!TryHashVerificationToken(token, out byte[] tokenHash))
        {
            return new AccountEmailVerificationReviewResult(
                AccountEmailVerificationReviewResultCode.InvalidToken);
        }

        EmailVerificationReviewResult result =
            _accountRepository.ReviewRegistrationEmail(
                tokenHash,
                _timeProvider.GetUtcNow().UtcDateTime);

        AccountEmailVerificationReviewResultCode code = result.Code switch
        {
            EmailVerificationReviewResultCode.Valid =>
                AccountEmailVerificationReviewResultCode.Valid,
            EmailVerificationReviewResultCode.InvalidToken =>
                AccountEmailVerificationReviewResultCode.InvalidToken,
            EmailVerificationReviewResultCode.ExpiredToken =>
                AccountEmailVerificationReviewResultCode.ExpiredToken,
            _ => throw new InvalidOperationException(
                $"Unexpected email verification review result: {result.Code}.")
        };

        return new AccountEmailVerificationReviewResult(code);
    }

    public async Task<AccountRegistrationResendServiceResult> ResendVerificationAsync(
        AccountRegistrationResendInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryNormalizeValidEmail(input.Email, out string normalizedEmail))
        {
            return new AccountRegistrationResendServiceResult(
                AccountRegistrationResendServiceResultCode.InvalidEmail);
        }

        string culture = ResolveCulture(input.Culture);
        DateTime issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        byte[] rawTokenBytes = RandomNumberGenerator.GetBytes(TokenSize);
        string rawToken = WebEncoders.Base64UrlEncode(rawTokenBytes);
        byte[] tokenHash = SHA256.HashData(rawTokenBytes);
        EmailTokenIssuance token = new(
            EmailTokenPurpose.RegistrationVerification,
            tokenHash,
            normalizedEmail,
            culture,
            issuedAtUtc,
            issuedAtUtc.Add(TokenLifetime));

        AccountRegistrationResendResult persistenceResult =
            _accountRepository.ResendRegistrationEmail(
                new AccountRegistrationResendRequest(normalizedEmail, token));

        if (persistenceResult.Code != AccountRegistrationResendResultCode.Issued)
        {
            return new AccountRegistrationResendServiceResult(
                persistenceResult.Code switch
                {
                    AccountRegistrationResendResultCode.NotEligible =>
                        AccountRegistrationResendServiceResultCode.NotEligible,
                    AccountRegistrationResendResultCode.Cooldown =>
                        AccountRegistrationResendServiceResultCode.Cooldown,
                    AccountRegistrationResendResultCode.DailyLimit =>
                        AccountRegistrationResendServiceResultCode.DailyLimit,
                    _ => throw new InvalidOperationException(
                        $"Unexpected registration resend result: {persistenceResult.Code}.")
                });
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
            return new AccountRegistrationResendServiceResult(
                AccountRegistrationResendServiceResultCode.DeliveryFailed);
        }

        return new AccountRegistrationResendServiceResult(
            AccountRegistrationResendServiceResultCode.Sent);
    }

    public AccountEmailVerificationResult ConfirmEmailVerification(string? token)
    {
        if (!TryHashVerificationToken(token, out byte[] tokenHash))
        {
            return new AccountEmailVerificationResult(
                AccountEmailVerificationResultCode.InvalidToken);
        }

        EmailVerificationResult result =
            _accountRepository.ConfirmRegistrationEmail(
                tokenHash,
                _timeProvider.GetUtcNow().UtcDateTime);

        AccountEmailVerificationResultCode code = result.Code switch
        {
            EmailVerificationResultCode.Success =>
                AccountEmailVerificationResultCode.Success,
            EmailVerificationResultCode.InvalidToken =>
                AccountEmailVerificationResultCode.InvalidToken,
            EmailVerificationResultCode.ExpiredToken =>
                AccountEmailVerificationResultCode.ExpiredToken,
            _ => throw new InvalidOperationException(
                $"Unexpected email verification result: {result.Code}.")
        };

        return new AccountEmailVerificationResult(code);
    }

    private static AccountRegistrationServiceResult? Validate(
        AccountRegistrationInput input)
    {
        if (!TryNormalizeValidEmail(input.Email, out _))
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

    private static bool TryNormalizeValidEmail(
        string? email,
        out string normalizedEmail)
    {
        normalizedEmail = string.Empty;

        return EmailNormalizer.TryNormalize(email, out normalizedEmail) &&
            MailAddress.TryCreate(normalizedEmail, out MailAddress? mailAddress) &&
            string.Equals(
                mailAddress.Address,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase);
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

    private static bool TryHashVerificationToken(
        string? token,
        out byte[] tokenHash)
    {
        tokenHash = [];

        if (token is null || token.Length != EncodedTokenLength)
        {
            return false;
        }

        byte[] rawToken;

        try
        {
            rawToken = WebEncoders.Base64UrlDecode(token);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            if (rawToken.Length != TokenSize)
            {
                return false;
            }

            tokenHash = SHA256.HashData(rawToken);
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawToken);
        }
    }
}
