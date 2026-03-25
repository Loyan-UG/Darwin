namespace Darwin.Application.Common.DTOs
{
    /// <summary>
    /// Lightweight lookup item used by admin forms and filters.
    /// </summary>
    public sealed class LookupItemDto
    {
        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the primary label rendered in dropdowns.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional secondary label used for extra context.
        /// </summary>
        public string? SecondaryLabel { get; set; }
    }
}
