public sealed record AccountRegistrationRequest(
    string Email,
    string PasswordHash,
    string PasswordSalt,
    DateTime CreatedAt,
    string? PreferredCulture,
    EmailTokenIssuance VerificationToken);

public sealed record EmailTokenIssuance(
    EmailTokenPurpose Purpose,
    byte[] TokenHash,
    string TargetEmail,
    string Culture,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);

public enum AccountRegistrationResultCode
{
    Success,
    DuplicateEmail
}

public sealed record AccountRegistrationResult(
    AccountRegistrationResultCode Code,
    int? UserId);

public enum EmailTokenReplacementResultCode
{
    Success,
    UserNotFound
}

public sealed record EmailTokenReplacementResult(EmailTokenReplacementResultCode Code);

public sealed record AccountRegistrationResendRequest(
    string Email,
    EmailTokenIssuance VerificationToken);

public enum AccountRegistrationResendResultCode
{
    Issued,
    NotEligible,
    Cooldown,
    DailyLimit
}

public sealed record AccountRegistrationResendResult(
    AccountRegistrationResendResultCode Code);

public enum EmailVerificationReviewResultCode
{
    Valid,
    InvalidToken,
    ExpiredToken
}

public sealed record EmailVerificationReviewResult(
    EmailVerificationReviewResultCode Code);

public enum EmailVerificationResultCode
{
    Success,
    InvalidToken,
    ExpiredToken
}

public sealed record EmailVerificationResult(EmailVerificationResultCode Code);

public enum EmailChangeRequestResultCode
{
    Success,
    UserNotFound
}

public sealed record EmailChangeRequestResult(EmailChangeRequestResultCode Code);

public enum EmailChangeConfirmationResultCode
{
    Success,
    DuplicateEmail,
    InvalidToken,
    ExpiredToken
}

public sealed record EmailChangeConfirmationResult(EmailChangeConfirmationResultCode Code);
