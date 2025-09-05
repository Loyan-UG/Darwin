namespace Darwin.Domain.Common
{
    /// <summary>
    /// Represents a culture code like "de-DE". Stored as normalized string; validate on assignment.
    /// </summary>
    public readonly struct CultureCode
    {
        public string Value { get; }
        public CultureCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new System.ArgumentNullException(nameof(value));
            Value = value; // Additional validation can be enforced in configuration.
        }
        public override string ToString() => Value;
    }
}