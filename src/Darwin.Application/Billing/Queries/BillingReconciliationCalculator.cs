namespace Darwin.Application.Billing.Queries
{
    /// <summary>
    /// Provides conservative helper calculations for invoice, payment, and refund reconciliation views.
    /// The calculator does not introduce new domain states; it only derives operator-facing totals.
    /// </summary>
    internal static class BillingReconciliationCalculator
    {
        /// <summary>
        /// Clamps refunded value into the valid payment amount range.
        /// </summary>
        public static long ClampRefundedAmount(long paymentAmountMinor, long refundedAmountMinor)
        {
            return Math.Min(Math.Max(refundedAmountMinor, 0L), Math.Max(paymentAmountMinor, 0L));
        }

        /// <summary>
        /// Calculates net collected value after refunds are applied.
        /// </summary>
        public static long CalculateNetCollectedAmount(long paymentAmountMinor, long refundedAmountMinor)
        {
            return Math.Max(paymentAmountMinor - ClampRefundedAmount(paymentAmountMinor, refundedAmountMinor), 0L);
        }

        /// <summary>
        /// Calculates invoice settlement amount capped to invoice total.
        /// </summary>
        public static long CalculateSettledAmount(long invoiceTotalMinor, long netCollectedAmountMinor)
        {
            return Math.Min(Math.Max(invoiceTotalMinor, 0L), Math.Max(netCollectedAmountMinor, 0L));
        }

        /// <summary>
        /// Calculates remaining balance capped at zero.
        /// </summary>
        public static long CalculateBalanceAmount(long invoiceTotalMinor, long settledAmountMinor)
        {
            return Math.Max(invoiceTotalMinor - Math.Max(settledAmountMinor, 0L), 0L);
        }
    }
}
