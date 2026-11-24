public enum AccountEmailVerificationReviewResultCode
{
    Valid,
    InvalidToken,
    ExpiredToken
}

public sealed record AccountEmailVerificationReviewResult(
    AccountEmailVerificationReviewResultCode Code);

public enum AccountEmailVerificationResultCode
{
    Success,
    InvalidToken,
    ExpiredToken
}

public sealed record AccountEmailVerificationResult(
    AccountEmailVerificationResultCode Code);
