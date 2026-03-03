namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Response returned by like/favorite toggle operations.
    /// </summary>
    public sealed class ToggleBusinessReactionResponse
    {
        public bool IsActive { get; init; }
        public int TotalCount { get; init; }
    }
}