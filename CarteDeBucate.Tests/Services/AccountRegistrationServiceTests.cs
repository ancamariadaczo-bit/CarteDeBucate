using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

public class AccountRegistrationServiceTests
{
    private static readonly DateTimeOffset IssuedAt =
        new(2026, 11, 23, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegisterAsync_WithValidInput_ShouldPersistThenSendVerificationEmail()
    {
        RecordingAccountRepository repository = new();
        RecordingLinkBuilder linkBuilder = new();
        RecordingTemplateRenderer renderer = new();
        FakeEmailSender emailSender = new();
        AccountRegistrationService service = CreateService(
            repository,
            linkBuilder,
            renderer,
            emailSender);

        AccountRegistrationServiceResult result = await service.RegisterAsync(
            new AccountRegistrationInput(
                "  Anca@Example.com  ",
                "a-secure-password",
                "a-secure-password",
                "ro-RO"));

        Assert.Equal(AccountRegistrationServiceResultCode.Success, result.Code);
        Assert.True(result.IsSuccess);
        Assert.True(result.RegistrationWasPersisted);

        AccountRegistrationRequest persistedRequest =
            Assert.IsType<AccountRegistrationRequest>(repository.RegistrationRequest);
        Assert.Equal("anca@example.com", persistedRequest.Email);
        Assert.Equal(IssuedAt.UtcDateTime, persistedRequest.CreatedAt);
        Assert.Null(persistedRequest.PreferredCulture);
        Assert.NotEqual("a-secure-password", persistedRequest.PasswordHash);
        Assert.NotEqual("a-secure-password", persistedRequest.PasswordSalt);
        Assert.True(new PasswordHasher().VerifyPassword(
            "a-secure-password",
            persistedRequest.PasswordHash,
            persistedRequest.PasswordSalt));
        Assert.Equal(
            EmailTokenPurpose.RegistrationVerification,
            persistedRequest.VerificationToken.Purpose);
        Assert.Equal("anca@example.com", persistedRequest.VerificationToken.TargetEmail);
        Assert.Equal("ro-RO", persistedRequest.VerificationToken.Culture);
        Assert.Equal(IssuedAt.UtcDateTime, persistedRequest.VerificationToken.CreatedAtUtc);
        Assert.Equal(
            IssuedAt.UtcDateTime.AddHours(24),
            persistedRequest.VerificationToken.ExpiresAtUtc);

        string rawToken = Assert.IsType<string>(linkBuilder.Token);
        byte[] rawTokenBytes = WebEncoders.Base64UrlDecode(rawToken);
        Assert.Equal(32, rawTokenBytes.Length);
        Assert.Equal(
            SHA256.HashData(rawTokenBytes),
            persistedRequest.VerificationToken.TokenHash);
        Assert.Equal("/Account/VerifyEmail", linkBuilder.RelativePath);

        EmailTemplateRenderRequest renderRequest =
            Assert.IsType<EmailTemplateRenderRequest>(renderer.Request);
        Assert.Equal(EmailTemplateId.RegistrationVerification, renderRequest.TemplateId);
        Assert.Equal("ro-RO", renderRequest.Culture);
        Assert.Equal(linkBuilder.ConfirmationUrl, renderRequest.ConfirmationUrl);

        EmailMessage message = Assert.Single(emailSender.SentMessages);
        Assert.Equal("anca@example.com", message.Recipient);
        Assert.Contains(linkBuilder.ConfirmationUrl, message.HtmlBody);
        Assert.Contains(linkBuilder.ConfirmationUrl, message.PlainTextBody);
        Assert.DoesNotContain(rawToken, message.IdempotencyKey);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldNotRenderOrSendEmail()
    {
        RecordingAccountRepository repository = new()
        {
            RegistrationResult = new AccountRegistrationResult(
                AccountRegistrationResultCode.DuplicateEmail,
                null)
        };
        RecordingLinkBuilder linkBuilder = new();
        RecordingTemplateRenderer renderer = new();
        FakeEmailSender emailSender = new();
        AccountRegistrationService service = CreateService(
            repository,
            linkBuilder,
            renderer,
            emailSender);

        AccountRegistrationServiceResult result = await service.RegisterAsync(
            ValidInput());

        Assert.Equal(AccountRegistrationServiceResultCode.DuplicateEmail, result.Code);
        Assert.False(result.RegistrationWasPersisted);
        Assert.Null(linkBuilder.Token);
        Assert.Null(renderer.Request);
        Assert.Empty(emailSender.SentMessages);
    }

    [Fact]
    public async Task RegisterAsync_WhenDeliveryFails_ShouldKeepRecoverableRegistrationState()
    {
        RecordingAccountRepository repository = new();
        ThrowingEmailSender emailSender = new();
        AccountRegistrationService service = CreateService(
            repository,
            new RecordingLinkBuilder(),
            new RecordingTemplateRenderer(),
            emailSender);

        AccountRegistrationServiceResult result = await service.RegisterAsync(
            ValidInput());

        Assert.Equal(AccountRegistrationServiceResultCode.DeliveryFailed, result.Code);
        Assert.False(result.IsSuccess);
        Assert.True(result.RegistrationWasPersisted);
        Assert.NotNull(repository.RegistrationRequest);
        Assert.Equal(1, emailSender.SendAttempts);
    }

    [Theory]
    [InlineData("not-an-email", "a-secure-password", "a-secure-password", AccountRegistrationServiceResultCode.InvalidEmail)]
    [InlineData("anca@example.com", "too-short", "too-short", AccountRegistrationServiceResultCode.PasswordTooShort)]
    [InlineData("anca@example.com", "a-secure-password", "different-password", AccountRegistrationServiceResultCode.PasswordConfirmationMismatch)]
    public async Task RegisterAsync_WithInvalidInput_ShouldRejectBeforePersistence(
        string email,
        string password,
        string passwordConfirmation,
        AccountRegistrationServiceResultCode expectedCode)
    {
        RecordingAccountRepository repository = new();
        FakeEmailSender emailSender = new();
        AccountRegistrationService service = CreateService(
            repository,
            new RecordingLinkBuilder(),
            new RecordingTemplateRenderer(),
            emailSender);

        AccountRegistrationServiceResult result = await service.RegisterAsync(
            new AccountRegistrationInput(
                email,
                password,
                passwordConfirmation,
                "en-US"));

        Assert.Equal(expectedCode, result.Code);
        Assert.Null(repository.RegistrationRequest);
        Assert.Empty(emailSender.SentMessages);
    }

    private static AccountRegistrationService CreateService(
        IAccountRepository repository,
        IAccountLinkBuilder linkBuilder,
        IEmailTemplateRenderer renderer,
        IEmailSender emailSender)
    {
        return new AccountRegistrationService(
            repository,
            new PasswordHasher(),
            linkBuilder,
            renderer,
            emailSender,
            new FixedTimeProvider(IssuedAt));
    }

    private static AccountRegistrationInput ValidInput()
    {
        return new AccountRegistrationInput(
            "anca@example.com",
            "a-secure-password",
            "a-secure-password",
            "en-US");
    }

    private sealed class RecordingAccountRepository : IAccountRepository
    {
        public AccountRegistrationRequest? RegistrationRequest { get; private set; }

        public AccountRegistrationResult RegistrationResult { get; init; } =
            new(AccountRegistrationResultCode.Success, 42);

        public AccountRegistrationResult Register(AccountRegistrationRequest request)
        {
            RegistrationRequest = request;
            return RegistrationResult;
        }

        public EmailTokenReplacementResult ReplaceEmailToken(
            int userId,
            EmailTokenIssuance token) => throw new NotSupportedException();

        public EmailVerificationResult ConfirmRegistrationEmail(
            byte[] tokenHash,
            DateTime confirmedAtUtc) => throw new NotSupportedException();

        public EmailChangeRequestResult RequestEmailChange(
            int userId,
            EmailTokenIssuance token) => throw new NotSupportedException();

        public EmailChangeConfirmationResult ConfirmEmailChange(
            byte[] tokenHash,
            DateTime confirmedAtUtc) => throw new NotSupportedException();
    }

    private sealed class RecordingLinkBuilder : IAccountLinkBuilder
    {
        public string? RelativePath { get; private set; }

        public string? Token { get; private set; }

        public string ConfirmationUrl { get; } =
            "https://recipes.example.com/Account/VerifyEmail?token=raw-token";

        public string BuildConfirmationUrl(string relativePath, string token)
        {
            RelativePath = relativePath;
            Token = token;
            return ConfirmationUrl;
        }
    }

    private sealed class RecordingTemplateRenderer : IEmailTemplateRenderer
    {
        public EmailTemplateRenderRequest? Request { get; private set; }

        public RenderedEmailTemplate Render(EmailTemplateRenderRequest request)
        {
            Request = request;
            return new RenderedEmailTemplate(
                "Verify your email",
                $"<a href=\"{request.ConfirmationUrl}\">Verify</a>",
                $"Verify: {request.ConfirmationUrl}");
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public int SendAttempts { get; private set; }

        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            SendAttempts++;
            throw new EmailDeliveryException(
                EmailDeliveryFailureCode.ProviderFailure);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
