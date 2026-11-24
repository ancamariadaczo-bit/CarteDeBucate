public sealed record AccountEmailChangeRequestInput(
    int UserId,
    string? CurrentPassword,
    string? NewEmail,
    string? Culture);

public enum AccountPasswordVerificationResultCode
{
    Valid,
    Invalid
}

public sealed record AccountPasswordVerificationResult(
    AccountPasswordVerificationResultCode Code);

public enum AccountEmailChangeRequestResultCode
{
    Sent,
    InvalidCurrentPassword,
    InvalidEmail,
    DuplicateEmail,
    UserNotFound,
    DeliveryFailed
}

public sealed record AccountEmailChangeRequestResult(
    AccountEmailChangeRequestResultCode Code);

public enum AccountEmailChangeResendResultCode
{
    Sent,
    NotEligible,
    Cooldown,
    DailyLimit,
    DeliveryFailed
}

public sealed record AccountEmailChangeResendResult(
    AccountEmailChangeResendResultCode Code);

public enum AccountEmailChangeReviewResultCode
{
    Valid,
    InvalidToken,
    ExpiredToken
}

public sealed record AccountEmailChangeReviewResult(
    AccountEmailChangeReviewResultCode Code);

public enum AccountEmailChangeConfirmationResultCode
{
    Success,
    DuplicateEmail,
    InvalidToken,
    ExpiredToken
}

public sealed record AccountEmailChangeConfirmationResult(
    AccountEmailChangeConfirmationResultCode Code);
