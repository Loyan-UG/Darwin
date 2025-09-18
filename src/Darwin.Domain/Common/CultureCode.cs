using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Darwin.Domain.Common
{
    /// <summary>
    /// Represents a BCP-47 culture code (e.g., "de-DE", "en-US").
    /// Provides strict validation and normalization on construction
    /// so that all persisted/compared values follow the same canonical form.
    /// 
    /// Design notes:
    /// - Type-safe alternative to raw strings to reduce runtime mistakes.
    /// - Validation uses <see cref="CultureInfo.GetCultureInfo(string)"/> which throws
    ///   if the culture is unknown to the runtime.
    /// - Normalization: stores the <see cref="CultureInfo.Name"/> (canonical BCP-47),
    ///   e.g., "de-de" → "de-DE".
    /// - Comparable and equatable (ordinal-ignore-case semantics).
    /// - Intentionally immutable (readonly struct) to behave as a value object.
    /// 
    /// Usage:
    /// <code>
    /// var c1 = new CultureCode("de-de");  // normalized to "de-DE"
    /// var c2 = CultureCode.Parse("en-US");
    /// if (c1.EqualsIgnoreCase(c2)) { ... }
    /// </code>
    /// </summary>
    public readonly struct CultureCode : IEquatable<CultureCode>, IComparable<CultureCode>
    {
        /// <summary>
        /// Canonical BCP-47 culture name (e.g., "de-DE").
        /// Guaranteed non-null/non-empty if the instance is constructed via valid APIs.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Creates a new <see cref="CultureCode"/> after validating and normalizing the input.
        /// Throws <see cref="ArgumentNullException"/> for null/empty,
        /// and <see cref="CultureNotFoundException"/> for unknown cultures.
        /// </summary>
        public CultureCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "Culture code cannot be null or empty.");

            // Normalize via CultureInfo to canonical BCP-47 form (e.g., "de-de" -> "de-DE").
            var culture = CultureInfo.GetCultureInfo(value.Trim());
            Value = culture.Name; // canonicalized
        }

        /// <summary>
        /// Parses the input and returns a <see cref="CultureCode"/>, throwing on invalid input.
        /// </summary>
        public static CultureCode Parse(string value) => new CultureCode(value);

        /// <summary>
        /// Tries to parse the input into a <see cref="CultureCode"/> without throwing.
        /// Returns false if null/empty/unknown.
        /// </summary>
        public static bool TryParse(string? value, out CultureCode result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                result = new CultureCode(value);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the canonical culture string (e.g., "de-DE").
        /// </summary>
        public override string ToString() => Value;

        /// <summary>
        /// Case-insensitive equality based on the canonical Value (ordinal-ignore-case).
        /// </summary>
        public bool Equals(CultureCode other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is CultureCode cc && Equals(cc);

        /// <inheritdoc />
        public override int GetHashCode() =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

        /// <summary>
        /// Compares two culture codes using ordinal-ignore-case of their canonical values.
        /// </summary>
        public int CompareTo(CultureCode other) =>
            string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Case-insensitive equality check helper.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualsIgnoreCase(CultureCode other) => Equals(other);

        public static bool operator ==(CultureCode left, CultureCode right) => left.Equals(right);
        public static bool operator !=(CultureCode left, CultureCode right) => !left.Equals(right);

        /// <summary>
        /// Implicit conversion to string for convenience in logging/serialization.
        /// Avoid using this in public APIs that expect strict types.
        /// </summary>
        public static implicit operator string(CultureCode code) => code.Value;
    }
}
