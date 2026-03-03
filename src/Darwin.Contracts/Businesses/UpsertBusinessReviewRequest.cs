namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Request payload for creating or updating the current user's review for a business.
    /// </summary>
    public sealed class UpsertBusinessReviewRequest
    {
        public byte Rating { get; init; }
        public string? Comment { get; init; }
    }
}