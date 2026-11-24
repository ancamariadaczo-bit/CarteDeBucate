using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;

public sealed class AccountEmailServiceTests
{
    private static readonly DateTimeOffset IssuedAt =
        new(2026, 11, 24, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void VerifyCurrentPassword_ShouldUseTheAuthenticatedUserId()
    {
        PasswordHasher hasher = new();
        (string hash, string salt) = hasher.HashPassword("correct-password");
        FakeUserRepository users = new();
        users.Users.Add(new User
        {
            Id = 7,
            Email = "anca@example.com",
            PasswordHash = hash,
            PasswordSalt = salt
        });
        AccountEmailService service = CreateService(users, new RecordingAccountRepository());

        AccountPasswordVerificationResult valid =
            service.VerifyCurrentPassword(7, "correct-password");
        AccountPasswordVerificationResult invalid =
            service.VerifyCurrentPassword(7, "incorrect-password");

        Assert.Equal(AccountPasswordVerificationResultCode.Valid, valid.Code);
        Assert.Equal(AccountPasswordVerificationResultCode.Invalid, invalid.Code);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_ShouldPersistThenSendToTheNewAddress()
    {
        FakeUserRepository users = CreateUsers();
        RecordingAccountRepository repository = new();
        FakeEmailSender sender = new();
        AccountEmailService service = CreateService(users, repository, sender);

        AccountEmailChangeRequestResult result = await service.RequestEmailChangeAsync(
            new AccountEmailChangeRequestInput(
                7,
                "correct-password",
                " New@Example.com ",
                "en-US"));

        Assert.Equal(AccountEmailChangeRequestResultCode.Sent, result.Code);
        Assert.Equal("new@example.com", repository.RequestedToken?.TargetEmail);
        Assert.Equal(EmailTokenPurpose.EmailChangeVerification, repository.RequestedToken?.Purpose);
        Assert.Equal("anca@example.com", users.GetById(7)?.Email);
        EmailMessage message = Assert.Single(sender.SentMessages);
        Assert.Equal("new@example.com", message.Recipient);
        Assert.DoesNotContain("raw-token", message.IdempotencyKey);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_WithInvalidCurrentPassword_ShouldNotPersistAToken()
    {
        FakeUserRepository users = CreateUsers();
        RecordingAccountRepository repository = new();
        AccountEmailService service = CreateService(users, repository);

        AccountEmailChangeRequestResult result = await service.RequestEmailChangeAsync(
            new AccountEmailChangeRequestInput(
                7,
                "incorrect-password",
                "new@example.com",
                "en-US"));

        Assert.Equal(AccountEmailChangeRequestResultCode.InvalidCurrentPassword, result.Code);
        Assert.Null(repository.RequestedToken);
    }

    [Fact]
    public async Task ResendEmailChangeAsync_ShouldSendOnlyToThePendingAddress()
    {
        FakeUserRepository users = CreateUsers();
        RecordingAccountRepository repository = new()
        {
            ResendResult = new EmailChangeResendResult(
                EmailChangeResendResultCode.Issued,
                "pending@example.com")
        };
        FakeEmailSender sender = new();
        AccountEmailService service = CreateService(users, repository, sender);

        AccountEmailChangeResendResult result = await service.ResendEmailChangeAsync(
            7,
            "en-US");

        Assert.Equal(AccountEmailChangeResendResultCode.Sent, result.Code);
        Assert.Equal(7, repository.ResendUserId);
        EmailMessage message = Assert.Single(sender.SentMessages);
        Assert.Equal("pending@example.com", message.Recipient);
        Assert.NotEqual(users.GetById(7)?.Email, message.Recipient);
    }

    [Fact]
    public void ReviewEmailChange_ShouldPassTheAuthenticatedUserIdToPersistence()
    {
        RecordingAccountRepository repository = new();
        AccountEmailService service = CreateService(CreateUsers(), repository);
        string token = WebEncoders.Base64UrlEncode(Enumerable.Repeat((byte)7, 32).ToArray());

        AccountEmailChangeReviewResult result = service.ReviewEmailChange(7, token);

        Assert.Equal(AccountEmailChangeReviewResultCode.Valid, result.Code);
        Assert.Equal(7, repository.ReviewUserId);
        Assert.Equal(
            SHA256.HashData(WebEncoders.Base64UrlDecode(token)),
            repository.ReviewTokenHash);
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_ShouldNotifyThePreviousAddressAfterCommit()
    {
        FakeUserRepository users = CreateUsers();
        RecordingAccountRepository repository = new()
        {
            ConfirmationResult = new EmailChangeConfirmationResult(
                EmailChangeConfirmationResultCode.Success,
                "anca@example.com",
                "new@example.com")
        };
        FakeEmailSender sender = new();
        RecordingTemplateRenderer renderer = new();
        AccountEmailService service = CreateService(users, repository, sender, renderer);
        string token = WebEncoders.Base64UrlEncode(Enumerable.Repeat((byte)8, 32).ToArray());

        AccountEmailChangeConfirmationResult result =
            await service.ConfirmEmailChangeAsync(7, token);

        Assert.Equal(AccountEmailChangeConfirmationResultCode.Success, result.Code);
        Assert.Equal(7, repository.ConfirmationUserId);
        Assert.Equal(EmailTemplateId.PreviousAddressChangeNotification, renderer.LastRequest?.TemplateId);
        Assert.Equal("new@example.com", renderer.LastRequest?.NewEmail);
        EmailMessage notification = Assert.Single(sender.SentMessages);
        Assert.Equal("anca@example.com", notification.Recipient);
        Assert.DoesNotContain(token, notification.HtmlBody);
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenNotificationFails_ShouldKeepTheCompletedChange()
    {
        RecordingAccountRepository repository = new()
        {
            ConfirmationResult = new EmailChangeConfirmationResult(
                EmailChangeConfirmationResultCode.Success,
                "anca@example.com",
                "new@example.com")
        };
        AccountEmailService service = CreateService(
            CreateUsers(),
            repository,
            new ThrowingEmailSender());
        string token = WebEncoders.Base64UrlEncode(Enumerable.Repeat((byte)9, 32).ToArray());

        AccountEmailChangeConfirmationResult result =
            await service.ConfirmEmailChangeAsync(7, token);

        Assert.Equal(AccountEmailChangeConfirmationResultCode.Success, result.Code);
        Assert.Equal(7, repository.ConfirmationUserId);
    }

    private static AccountEmailService CreateService(
        FakeUserRepository users,
        RecordingAccountRepository repository,
        IEmailSender? sender = null,
        RecordingTemplateRenderer? renderer = null)
    {
        return new AccountEmailService(
            users,
            repository,
            new PasswordHasher(),
            new FixedLinkBuilder(),
            renderer ?? new RecordingTemplateRenderer(),
            sender ?? new FakeEmailSender(),
            new FixedTimeProvider(IssuedAt),
            NullLogger<AccountEmailService>.Instance);
    }

    private static FakeUserRepository CreateUsers()
    {
        PasswordHasher hasher = new();
        (string hash, string salt) = hasher.HashPassword("correct-password");
        FakeUserRepository users = new();
        users.Users.Add(new User
        {
            Id = 7,
            Email = "anca@example.com",
            PasswordHash = hash,
            PasswordSalt = salt,
            PreferredCulture = "ro-RO"
        });
        return users;
    }

    private sealed class RecordingAccountRepository : IAccountRepository
    {
        public EmailTokenIssuance? RequestedToken { get; private set; }
        public int? ResendUserId { get; private set; }
        public int? ReviewUserId { get; private set; }
        public byte[]? ReviewTokenHash { get; private set; }
        public int? ConfirmationUserId { get; private set; }
        public EmailChangeResendResult ResendResult { get; init; } =
            new(EmailChangeResendResultCode.NotEligible);
        public EmailChangeReviewResult ReviewResult { get; init; } =
            new(EmailChangeReviewResultCode.Valid);
        public EmailChangeConfirmationResult ConfirmationResult { get; init; } =
            new(EmailChangeConfirmationResultCode.InvalidToken);

        public AccountRegistrationResult Register(AccountRegistrationRequest request) =>
            throw new NotSupportedException();

        public EmailTokenReplacementResult ReplaceEmailToken(int userId, EmailTokenIssuance token) =>
            throw new NotSupportedException();

        public AccountRegistrationResendResult ResendRegistrationEmail(
            AccountRegistrationResendRequest request) => throw new NotSupportedException();

        public EmailVerificationReviewResult ReviewRegistrationEmail(
            byte[] tokenHash,
            DateTime reviewedAtUtc) => throw new NotSupportedException();

        public EmailVerificationResult ConfirmRegistrationEmail(
            byte[] tokenHash,
            DateTime confirmedAtUtc) => throw new NotSupportedException();

        public EmailChangeRequestResult RequestEmailChange(int userId, EmailTokenIssuance token)
        {
            RequestedToken = token;
            return new EmailChangeRequestResult(EmailChangeRequestResultCode.Success);
        }

        public EmailChangeResendResult ResendEmailChange(
            int userId,
            EmailChangeResendIssuance issuance)
        {
            ResendUserId = userId;
            return ResendResult;
        }

        public EmailChangeReviewResult ReviewEmailChange(
            int userId,
            byte[] tokenHash,
            DateTime reviewedAtUtc)
        {
            ReviewUserId = userId;
            ReviewTokenHash = tokenHash.ToArray();
            return ReviewResult;
        }

        public EmailChangeConfirmationResult ConfirmEmailChange(
            int userId,
            byte[] tokenHash,
            DateTime confirmedAtUtc)
        {
            ConfirmationUserId = userId;
            return ConfirmationResult;
        }
    }

    private sealed class FixedLinkBuilder : IAccountLinkBuilder
    {
        public string BuildConfirmationUrl(string relativePath, string token) =>
            $"https://recipes.example.com{relativePath}?token={token}";
    }

    private sealed class RecordingTemplateRenderer : IEmailTemplateRenderer
    {
        public EmailTemplateRenderRequest? LastRequest { get; private set; }

        public RenderedEmailTemplate Render(EmailTemplateRenderRequest request)
        {
            LastRequest = request;
            return new RenderedEmailTemplate(
                "Subject",
                request.ConfirmationUrl ?? request.NewEmail ?? string.Empty,
                request.ConfirmationUrl ?? request.NewEmail ?? string.Empty);
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            throw new EmailDeliveryException(EmailDeliveryFailureCode.ProviderFailure);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
