using Microsoft.AspNetCore.WebUtilities;
using System.Net.Mail;
using System.Security.Cryptography;

public sealed class AccountEmailService : IAccountEmailService
{
    private const int TokenSize = 32;
    private const int EncodedTokenLength = 43;
    private const string ConfirmationPath = "/Settings/ConfirmEmailChange";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly IAccountLinkBuilder _linkBuilder;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountEmailService> _logger;

    public AccountEmailService(
        IUserRepository userRepository,
        IAccountRepository accountRepository,
        PasswordHasher passwordHasher,
        IAccountLinkBuilder linkBuilder,
        IEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<AccountEmailService> logger)
    {
        _userRepository = userRepository;
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _linkBuilder = linkBuilder;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public AccountPasswordVerificationResult VerifyCurrentPassword(int userId, string? password)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(password))
        {
            return new AccountPasswordVerificationResult(
                AccountPasswordVerificationResultCode.Invalid);
        }

        User? user = _userRepository.GetById(userId);

        if (user is null)
        {
            return new AccountPasswordVerificationResult(
                AccountPasswordVerificationResultCode.Invalid);
        }

        try
        {
            return _passwordHasher.VerifyPassword(
                password,
                user.PasswordHash,
                user.PasswordSalt)
                ? new AccountPasswordVerificationResult(
                    AccountPasswordVerificationResultCode.Valid)
                : new AccountPasswordVerificationResult(
                    AccountPasswordVerificationResultCode.Invalid);
        }
        catch (FormatException)
        {
            return new AccountPasswordVerificationResult(
                AccountPasswordVerificationResultCode.Invalid);
        }
    }

    public async Task<AccountEmailChangeRequestResult> RequestEmailChangeAsync(
        AccountEmailChangeRequestInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryNormalizeValidEmail(input.NewEmail, out string normalizedEmail))
        {
            return new AccountEmailChangeRequestResult(
                AccountEmailChangeRequestResultCode.InvalidEmail);
        }

        if (VerifyCurrentPassword(input.UserId, input.CurrentPassword).Code !=
            AccountPasswordVerificationResultCode.Valid)
        {
            return new AccountEmailChangeRequestResult(
                AccountEmailChangeRequestResultCode.InvalidCurrentPassword);
        }

        User? user = _userRepository.GetById(input.UserId);

        if (user is null)
        {
            return new AccountEmailChangeRequestResult(
                AccountEmailChangeRequestResultCode.UserNotFound);
        }

        string culture = ResolveCulture(user.PreferredCulture ?? input.Culture);
        DateTime issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        (string rawToken, byte[] tokenHash) = CreateToken();
        EmailTokenIssuance token = new(
            EmailTokenPurpose.EmailChangeVerification,
            tokenHash,
            normalizedEmail,
            culture,
            issuedAtUtc,
            issuedAtUtc.Add(TokenLifetime));

        EmailChangeRequestResult persistenceResult =
            _accountRepository.RequestEmailChange(input.UserId, token);

        if (persistenceResult.Code != EmailChangeRequestResultCode.Success)
        {
            return new AccountEmailChangeRequestResult(persistenceResult.Code switch
            {
                EmailChangeRequestResultCode.DuplicateEmail =>
                    AccountEmailChangeRequestResultCode.DuplicateEmail,
                EmailChangeRequestResultCode.UserNotFound =>
                    AccountEmailChangeRequestResultCode.UserNotFound,
                _ => throw new InvalidOperationException(
                    $"Unexpected email-change request result: {persistenceResult.Code}.")
            });
        }

        return await SendVerificationEmailAsync(
            normalizedEmail,
            culture,
            rawToken,
            tokenHash,
            cancellationToken)
            ? new AccountEmailChangeRequestResult(AccountEmailChangeRequestResultCode.Sent)
            : new AccountEmailChangeRequestResult(
                AccountEmailChangeRequestResultCode.DeliveryFailed);
    }

    public async Task<AccountEmailChangeResendResult> ResendEmailChangeAsync(
        int userId,
        string? requestCulture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        User? user = _userRepository.GetById(userId);

        if (user is null)
        {
            return new AccountEmailChangeResendResult(
                AccountEmailChangeResendResultCode.NotEligible);
        }

        string culture = ResolveCulture(user.PreferredCulture ?? requestCulture);
        DateTime issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        (string rawToken, byte[] tokenHash) = CreateToken();
        EmailChangeResendResult persistenceResult = _accountRepository.ResendEmailChange(
            userId,
            new EmailChangeResendIssuance(
                tokenHash,
                culture,
                issuedAtUtc,
                issuedAtUtc.Add(TokenLifetime)));

        if (persistenceResult.Code != EmailChangeResendResultCode.Issued ||
            string.IsNullOrWhiteSpace(persistenceResult.TargetEmail))
        {
            return new AccountEmailChangeResendResult(persistenceResult.Code switch
            {
                EmailChangeResendResultCode.NotEligible =>
                    AccountEmailChangeResendResultCode.NotEligible,
                EmailChangeResendResultCode.Cooldown =>
                    AccountEmailChangeResendResultCode.Cooldown,
                EmailChangeResendResultCode.DailyLimit =>
                    AccountEmailChangeResendResultCode.DailyLimit,
                _ => throw new InvalidOperationException(
                    $"Unexpected email-change resend result: {persistenceResult.Code}.")
            });
        }

        return await SendVerificationEmailAsync(
            persistenceResult.TargetEmail,
            culture,
            rawToken,
            tokenHash,
            cancellationToken)
            ? new AccountEmailChangeResendResult(AccountEmailChangeResendResultCode.Sent)
            : new AccountEmailChangeResendResult(
                AccountEmailChangeResendResultCode.DeliveryFailed);
    }

    public AccountEmailChangeReviewResult ReviewEmailChange(int userId, string? token)
    {
        if (!TryHashToken(token, out byte[] tokenHash))
        {
            return new AccountEmailChangeReviewResult(
                AccountEmailChangeReviewResultCode.InvalidToken);
        }

        EmailChangeReviewResult result = _accountRepository.ReviewEmailChange(
            userId,
            tokenHash,
            _timeProvider.GetUtcNow().UtcDateTime);

        return new AccountEmailChangeReviewResult(result.Code switch
        {
            EmailChangeReviewResultCode.Valid => AccountEmailChangeReviewResultCode.Valid,
            EmailChangeReviewResultCode.InvalidToken =>
                AccountEmailChangeReviewResultCode.InvalidToken,
            EmailChangeReviewResultCode.ExpiredToken =>
                AccountEmailChangeReviewResultCode.ExpiredToken,
            _ => throw new InvalidOperationException(
                $"Unexpected email-change review result: {result.Code}.")
        });
    }

    public async Task<AccountEmailChangeConfirmationResult> ConfirmEmailChangeAsync(
        int userId,
        string? token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryHashToken(token, out byte[] tokenHash))
        {
            return new AccountEmailChangeConfirmationResult(
                AccountEmailChangeConfirmationResultCode.InvalidToken);
        }

        EmailChangeConfirmationResult result = _accountRepository.ConfirmEmailChange(
            userId,
            tokenHash,
            _timeProvider.GetUtcNow().UtcDateTime);

        AccountEmailChangeConfirmationResultCode code = result.Code switch
        {
            EmailChangeConfirmationResultCode.Success =>
                AccountEmailChangeConfirmationResultCode.Success,
            EmailChangeConfirmationResultCode.DuplicateEmail =>
                AccountEmailChangeConfirmationResultCode.DuplicateEmail,
            EmailChangeConfirmationResultCode.InvalidToken =>
                AccountEmailChangeConfirmationResultCode.InvalidToken,
            EmailChangeConfirmationResultCode.ExpiredToken =>
                AccountEmailChangeConfirmationResultCode.ExpiredToken,
            _ => throw new InvalidOperationException(
                $"Unexpected email-change confirmation result: {result.Code}.")
        };

        if (code != AccountEmailChangeConfirmationResultCode.Success)
        {
            return new AccountEmailChangeConfirmationResult(code);
        }

        if (string.IsNullOrWhiteSpace(result.PreviousEmail) ||
            string.IsNullOrWhiteSpace(result.NewEmail))
        {
            throw new InvalidOperationException(
                "A successful email-change confirmation must include both email addresses.");
        }

        string culture = ResolveCulture(_userRepository.GetById(userId)?.PreferredCulture);

        try
        {
            RenderedEmailTemplate notification = _templateRenderer.Render(
                new EmailTemplateRenderRequest(
                    EmailTemplateId.PreviousAddressChangeNotification,
                    culture,
                    NewEmail: result.NewEmail));
            await _emailSender.SendAsync(
                new EmailMessage(
                    result.PreviousEmail!,
                    notification.Subject,
                    notification.HtmlBody,
                    notification.PlainTextBody,
                    CreateIdempotencyKey("previous-address-change", tokenHash)),
                CancellationToken.None);
        }
        catch (EmailDeliveryException)
        {
            _logger.LogWarning(
                "Previous-address email-change notification delivery failed after confirmation");
        }

        return new AccountEmailChangeConfirmationResult(code);
    }

    private async Task<bool> SendVerificationEmailAsync(
        string targetEmail,
        string culture,
        string rawToken,
        byte[] tokenHash,
        CancellationToken cancellationToken)
    {
        string confirmationUrl = _linkBuilder.BuildConfirmationUrl(ConfirmationPath, rawToken);
        RenderedEmailTemplate template = _templateRenderer.Render(
            new EmailTemplateRenderRequest(
                EmailTemplateId.EmailChangeVerification,
                culture,
                confirmationUrl));

        try
        {
            await _emailSender.SendAsync(
                new EmailMessage(
                    targetEmail,
                    template.Subject,
                    template.HtmlBody,
                    template.PlainTextBody,
                    CreateIdempotencyKey("email-change-verification", tokenHash)),
                cancellationToken);
            return true;
        }
        catch (EmailDeliveryException)
        {
            return false;
        }
    }

    private static (string RawToken, byte[] TokenHash) CreateToken()
    {
        byte[] rawTokenBytes = RandomNumberGenerator.GetBytes(TokenSize);

        try
        {
            return (
                WebEncoders.Base64UrlEncode(rawTokenBytes),
                SHA256.HashData(rawTokenBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawTokenBytes);
        }
    }

    private static bool TryNormalizeValidEmail(string? email, out string normalizedEmail)
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
        return ApplicationLocalization.GetSupportedCultureOrFallback(culture).Name;
    }

    private static string CreateIdempotencyKey(string purpose, byte[] tokenHash)
    {
        return $"{purpose}-{Convert.ToHexString(tokenHash)}";
    }

    private static bool TryHashToken(string? token, out byte[] tokenHash)
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
