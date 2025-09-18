using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Darwin.Domain.Common
{
    /// <summary>
    /// Value object representing a monetary amount in integer minor units and its ISO 4217 currency.
    /// 
    /// Why minor units?
    /// - Avoids floating-point rounding drift (e.g., 0.1 + 0.2 != 0.3 issues).
    /// - Keeps arithmetic exact in domain logic; formatting happens at the presentation layer.
    /// 
    /// Invariants:
    /// - <see cref="AmountMinor"/> is an integer (can be negative for adjustments/credits).
    /// - <see cref="Currency"/> is a non-empty ISO 4217 code (case-insensitive).
    /// - Mixed-currency arithmetic is disallowed (Add/Subtract will throw if currencies differ).
    /// 
    /// Helpful utilities included:
    /// - <see cref="FromMajor(decimal, string)"/> to convert major units (e.g., 10.99 EUR) into minor units,
    ///   honoring ISO minor digits (e.g., EUR=2, JPY=0).
    /// - <see cref="ToMajor()"/> to return the decimal major value (for calculations that need it).
    /// - Arithmetic: Add/Subtract/MultiplyByQuantity.
    /// - Allocation helpers <see cref="AllocateEven(int)"/> and <see cref="AllocateByWeights(int[])"/> that preserve pennies.
    /// 
    /// NOTE:
    /// - Formatting for display should be done outside the domain (UI/Application), using the user's culture.
    /// - The minor digits table includes common currencies; extend as needed.
    /// </summary>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        /// <summary>
        /// Amount in minor units (e.g., cents for EUR). Can be negative (e.g., refunds/discounts).
        /// </summary>
        public long AmountMinor { get; }

        /// <summary>
        /// ISO 4217 currency code (e.g., "EUR", "USD", "JPY"). Stored normalized uppercase.
        /// </summary>
        public string Currency { get; }

        /// <summary>
        /// Constructs a Money value with raw minor units. Currency is normalized to uppercase.
        /// </summary>
        public Money(long amountMinor, string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentNullException(nameof(currency), "Currency is required (ISO 4217).");

            AmountMinor = amountMinor;
            Currency = currency.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Creates a Money from major units (e.g., 10.99) using ISO minor digits mapping.
        /// Rounds using MidpointRounding.AwayFromZero to match typical financial expectations.
        /// </summary>
        public static Money FromMajor(decimal amountMajor, string currency)
        {
            var digits = GetMinorDigits(currency);
            var scaled = decimal.Round(amountMajor * Pow10(digits), 0, MidpointRounding.AwayFromZero);
            return new Money(checked((long)scaled), currency);
        }

        /// <summary>
        /// Returns the decimal representation in major units (e.g., 1099 -> 10.99 for EUR).
        /// </summary>
        public decimal ToMajor()
        {
            var digits = GetMinorDigits(Currency);
            return AmountMinor / Pow10(digits);
        }

        /// <summary>
        /// Adds another Money of the same currency.
        /// </summary>
        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(checked(AmountMinor + other.AmountMinor), Currency);
        }

        /// <summary>
        /// Subtracts another Money of the same currency.
        /// </summary>
        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(checked(AmountMinor - other.AmountMinor), Currency);
        }

        /// <summary>
        /// Multiplies the amount by a non-negative integer quantity (e.g., unit price * qty).
        /// </summary>
        public Money MultiplyByQuantity(int qty)
        {
            if (qty < 0) throw new ArgumentOutOfRangeException(nameof(qty), "Quantity cannot be negative.");
            return new Money(checked(AmountMinor * qty), Currency);
        }

        /// <summary>
        /// Splits the amount into <paramref name="parts"/> nearly-equal buckets preserving total sum.
        /// Useful for distributing a discount across lines without losing a cent.
        /// </summary>
        public Money[] AllocateEven(int parts)
        {
            if (parts <= 0) throw new ArgumentOutOfRangeException(nameof(parts));

            var baseShare = AmountMinor / parts;
            var remainder = AmountMinor % parts; // distribute the remainder one by one
            var result = new Money[parts];

            for (int i = 0; i < parts; i++)
            {
                var extra = i < Math.Abs(remainder) ? Math.Sign(remainder) : 0;
                result[i] = new Money(baseShare + extra, Currency);
            }
            return result;
        }

        /// <summary>
        /// Splits the amount proportionally by integer weights; preserves total.
        /// </summary>
        public Money[] AllocateByWeights(params int[] weights)
        {
            if (weights == null || weights.Length == 0)
                throw new ArgumentException("Weights are required.", nameof(weights));

            long totalWeight = 0;
            foreach (var w in weights)
            {
                if (w < 0) throw new ArgumentOutOfRangeException(nameof(weights), "Weights must be non-negative.");
                totalWeight += w;
            }
            if (totalWeight == 0) // all zero weights -> even split
                return AllocateEven(weights.Length);

            var result = new Money[weights.Length];
            long allocated = 0;

            // First pass: floor each share
            for (int i = 0; i < weights.Length; i++)
            {
                var share = (AmountMinor * weights[i]) / totalWeight;
                result[i] = new Money(share, Currency);
                allocated += share;
            }

            // Distribute remainder (positive or negative) to highest weights first
            long remainder = AmountMinor - allocated;
            var order = GetIndicesByWeightDesc(weights);
            for (int k = 0; k < Math.Abs(remainder); k++)
            {
                var idx = order[k % order.Count];
                var delta = Math.Sign(remainder);
                result[idx] = new Money(result[idx].AmountMinor + delta, Currency);
            }

            return result;
        }

        /// <summary>
        /// Case-insensitive equality of currency and exact equality of minor amount.
        /// </summary>
        public bool Equals(Money other) =>
            AmountMinor == other.AmountMinor &&
            string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Money m && Equals(m);

        /// <inheritdoc />
        public override int GetHashCode() =>
            HashCode.Combine(AmountMinor, Currency.ToUpperInvariant());

        /// <summary>
        /// Compares by currency (ordinal-ignore-case), then by amount.
        /// Do not use to order mixed currencies in financial reports unless the sort semantics are acceptable.
        /// </summary>
        public int CompareTo(Money other)
        {
            var c = string.Compare(Currency, other.Currency, StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            return AmountMinor.CompareTo(other.AmountMinor);
        }

        public static bool operator ==(Money a, Money b) => a.Equals(b);
        public static bool operator !=(Money a, Money b) => !a.Equals(b);

        /// <summary>
        /// Minor digits mapping for common currencies. Extend as required.
        /// If a code is unknown, defaults to 2 (safe for most currencies) to avoid silent mis-scaling.
        /// </summary>
        public static int GetMinorDigits(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentNullException(nameof(currency));

            currency = currency.Trim().ToUpperInvariant();
            if (MinorDigits.TryGetValue(currency, out var d))
                return d;

            // Default to 2 as a conservative choice (EUR/USD/etc.); prefer explicit extension for exotic codes.
            return 2;
        }

        /// <summary>
        /// Returns a string like "EUR 10.99" using invariant formatting for diagnostics/logging.
        /// UI formatting belongs to presentation layer (CultureInfo/NumberFormat).
        /// </summary>
        public override string ToString()
        {
            var digits = GetMinorDigits(Currency);
            var major = ToMajor().ToString($"F{digits}", CultureInfo.InvariantCulture);
            return $"{Currency} {major}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSameCurrency(Money other)
        {
            if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Currency mismatch.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal Pow10(int digits)
        {
            // digits is small (0..3 typically), safe to compute with decimal
            return digits switch
            {
                0 => 1m,
                1 => 10m,
                2 => 100m,
                3 => 1000m,
                _ => (decimal)Math.Pow(10, digits)
            };
        }

        private static List<int> GetIndicesByWeightDesc(int[] weights)
        {
            var idx = new List<int>(weights.Length);
            for (int i = 0; i < weights.Length; i++) idx.Add(i);
            idx.Sort((a, b) => weights[b].CompareTo(weights[a]));
            return idx;
        }

        /// <summary>
        /// Minimal ISO 4217 minor digits map. Extend this as required by your deployment.
        /// </summary>
        private static readonly Dictionary<string, int> MinorDigits = new(StringComparer.OrdinalIgnoreCase)
        {
            // Common
            ["EUR"] = 2,
            ["USD"] = 2,
            ["GBP"] = 2,
            ["CHF"] = 2,
            ["SEK"] = 2,
            ["NOK"] = 2,
            ["DKK"] = 2,
            ["PLN"] = 2,
            ["CZK"] = 2,
            ["HUF"] = 2,
            // Zero-decimal currencies
            ["JPY"] = 0,
            ["KRW"] = 0,
            // Three-decimal examples
            ["BHD"] = 3,
            ["KWD"] = 3,
            ["JOD"] = 3
        };
    }
}
