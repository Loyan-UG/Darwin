using System;


namespace Darwin.Domain.Common
{
    /// <summary>
    /// Value object representing a monetary amount in minor units (e.g., cents) and its currency.
    /// Store integer minor units to avoid floating point/rounding drift; format by culture at presentation.
    /// </summary>
    public readonly struct Money : IEquatable<Money>
    {
        /// <summary>
        /// Amount in minor units (e.g., cents for EUR). Always an integer.
        /// </summary>
        public long AmountMinor { get; }


        /// <summary>
        /// ISO 4217 currency code, e.g., "EUR".
        /// </summary>
        public string Currency { get; }


        public Money(long amountMinor, string currency)
        {
            AmountMinor = amountMinor;
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }


        public bool Equals(Money other) => AmountMinor == other.AmountMinor && string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is Money m && Equals(m);
        public override int GetHashCode() => HashCode.Combine(AmountMinor, Currency?.ToUpperInvariant());


        /// <summary> Adds two Money values with the same currency. Throws otherwise. </summary>
        public Money Add(Money other)
        {
            if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot add Money with different currencies.");
            return new Money(AmountMinor + other.AmountMinor, Currency);
        }


        /// <summary> Multiplies the amount by an integer quantity (e.g., line total = unit price * qty). </summary>
        public Money Multiply(int qty)
        {
            if (qty < 0) throw new ArgumentOutOfRangeException(nameof(qty));
            checked { return new Money(AmountMinor * qty, Currency); }
        }
    }
}