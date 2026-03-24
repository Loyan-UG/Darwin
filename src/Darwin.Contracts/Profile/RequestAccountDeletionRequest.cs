namespace Darwin.Contracts.Profile;

/// <summary>
/// Represents an authenticated current-user request to irreversibly deactivate and anonymize the account.
/// </summary>
/// <remarks>
/// This request does not perform a hard delete. The server preserves the user row and related business/history
/// records while anonymizing personally identifiable information as far as safely possible.
/// </remarks>
/// <param name="ConfirmIrreversibleDeletion">
/// Indicates that the user explicitly confirmed the irreversible anonymization and deactivation workflow.
/// </param>
public sealed record RequestAccountDeletionRequest(bool ConfirmIrreversibleDeletion);
