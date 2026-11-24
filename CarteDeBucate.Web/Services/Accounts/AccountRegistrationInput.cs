public sealed record AccountRegistrationInput(
    string? Email,
    string? Password,
    string? PasswordConfirmation,
    string? Culture);
