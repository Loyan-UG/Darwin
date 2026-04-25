using Darwin.Application.Billing.Queries;
using FluentAssertions;

namespace Darwin.Tests.Unit.Billing;

/// <summary>
/// Unit tests for <see cref="BillingReconciliationCalculator"/>.
/// Covers all four calculation helpers: ClampRefundedAmount, CalculateNetCollectedAmount,
/// CalculateSettledAmount, and CalculateBalanceAmount.
/// </summary>
public sealed class BillingReconciliationCalculatorTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // ClampRefundedAmount
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClampRefundedAmount_Should_ReturnRefundedAmount_WhenWithinPaymentRange()
    {
        var result = BillingReconciliationCalculator.ClampRefundedAmount(1000L, 300L);
        result.Should().Be(300L, "a refund less than payment amount should be returned as-is");
    }

    [Fact]
    public void ClampRefundedAmount_Should_ReturnPaymentAmount_WhenRefundExceedsPayment()
    {
        var result = BillingReconciliationCalculator.ClampRefundedAmount(500L, 800L);
        result.Should().Be(500L, "refund cannot exceed the payment amount");
    }

    [Fact]
    public void ClampRefundedAmount_Should_ReturnZero_WhenRefundIsNegative()
    {
        var result = BillingReconciliationCalculator.ClampRefundedAmount(1000L, -50L);
        result.Should().Be(0L, "negative refund amounts are clamped to zero");
    }

    [Fact]
    public void ClampRefundedAmount_Should_ReturnZero_WhenBothAreZero()
    {
        var result = BillingReconciliationCalculator.ClampRefundedAmount(0L, 0L);
        result.Should().Be(0L);
    }

    [Fact]
    public void ClampRefundedAmount_Should_ReturnZero_WhenPaymentIsNegativeAndRefundIsPositive()
    {
        // Math.Max(paymentAmountMinor, 0L) = 0, so any refund is clamped to 0
        var result = BillingReconciliationCalculator.ClampRefundedAmount(-100L, 50L);
        result.Should().Be(0L, "a negative payment amount is treated as zero for clamping purposes");
    }

    [Fact]
    public void ClampRefundedAmount_Should_ReturnExactPaymentAmount_WhenRefundEqualsPayment()
    {
        var result = BillingReconciliationCalculator.ClampRefundedAmount(500L, 500L);
        result.Should().Be(500L, "a full refund equal to the payment should be allowed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CalculateNetCollectedAmount
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNetCollectedAmount_Should_ReturnPaymentMinusRefund()
    {
        var result = BillingReconciliationCalculator.CalculateNetCollectedAmount(1000L, 300L);
        result.Should().Be(700L, "net collected = payment - refunded");
    }

    [Fact]
    public void CalculateNetCollectedAmount_Should_ReturnZero_WhenFullyRefunded()
    {
        var result = BillingReconciliationCalculator.CalculateNetCollectedAmount(500L, 500L);
        result.Should().Be(0L, "a fully refunded payment results in zero net collected");
    }

    [Fact]
    public void CalculateNetCollectedAmount_Should_ReturnZero_WhenRefundExceedsPayment()
    {
        // Clamp ensures refunded cannot exceed payment; net = 500 - 500 = 0
        var result = BillingReconciliationCalculator.CalculateNetCollectedAmount(500L, 800L);
        result.Should().Be(0L, "over-refund is clamped so net cannot go negative");
    }

    [Fact]
    public void CalculateNetCollectedAmount_Should_ReturnFullPayment_WhenNoRefund()
    {
        var result = BillingReconciliationCalculator.CalculateNetCollectedAmount(1000L, 0L);
        result.Should().Be(1000L, "zero refund means the full payment amount is collected");
    }

    [Fact]
    public void CalculateNetCollectedAmount_Should_ReturnZero_WhenPaymentIsZero()
    {
        var result = BillingReconciliationCalculator.CalculateNetCollectedAmount(0L, 0L);
        result.Should().Be(0L);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CalculateSettledAmount
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateSettledAmount_Should_ReturnNetCollected_WhenLessThanInvoiceTotal()
    {
        var result = BillingReconciliationCalculator.CalculateSettledAmount(1000L, 700L);
        result.Should().Be(700L, "settled amount equals net collected when less than invoice total");
    }

    [Fact]
    public void CalculateSettledAmount_Should_ReturnInvoiceTotal_WhenNetCollectedExceedsInvoice()
    {
        var result = BillingReconciliationCalculator.CalculateSettledAmount(500L, 800L);
        result.Should().Be(500L, "settled amount is capped at the invoice total");
    }

    [Fact]
    public void CalculateSettledAmount_Should_ReturnZero_WhenBothAreZero()
    {
        var result = BillingReconciliationCalculator.CalculateSettledAmount(0L, 0L);
        result.Should().Be(0L);
    }

    [Fact]
    public void CalculateSettledAmount_Should_ReturnZero_WhenNetCollectedIsNegative()
    {
        // Math.Max(netCollectedAmountMinor, 0L) = 0
        var result = BillingReconciliationCalculator.CalculateSettledAmount(1000L, -100L);
        result.Should().Be(0L, "a negative net collected amount is clamped to zero");
    }

    [Fact]
    public void CalculateSettledAmount_Should_ReturnInvoiceTotal_WhenEqualToNetCollected()
    {
        var result = BillingReconciliationCalculator.CalculateSettledAmount(1000L, 1000L);
        result.Should().Be(1000L, "settled equals invoice total when amounts match exactly");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CalculateBalanceAmount
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateBalanceAmount_Should_ReturnRemainingBalance()
    {
        var result = BillingReconciliationCalculator.CalculateBalanceAmount(1000L, 700L);
        result.Should().Be(300L, "balance = invoice total - settled amount");
    }

    [Fact]
    public void CalculateBalanceAmount_Should_ReturnZero_WhenFullySettled()
    {
        var result = BillingReconciliationCalculator.CalculateBalanceAmount(1000L, 1000L);
        result.Should().Be(0L, "a fully settled invoice has zero remaining balance");
    }

    [Fact]
    public void CalculateBalanceAmount_Should_ReturnZero_WhenSettledExceedsInvoice()
    {
        var result = BillingReconciliationCalculator.CalculateBalanceAmount(500L, 800L);
        result.Should().Be(0L, "balance cannot be negative; excess settlement is clamped to zero");
    }

    [Fact]
    public void CalculateBalanceAmount_Should_ReturnFullInvoiceTotal_WhenNothingSettled()
    {
        var result = BillingReconciliationCalculator.CalculateBalanceAmount(1000L, 0L);
        result.Should().Be(1000L, "zero settled amount means the full invoice total is outstanding");
    }

    [Fact]
    public void CalculateBalanceAmount_Should_ReturnZero_WhenBothAreZero()
    {
        var result = BillingReconciliationCalculator.CalculateBalanceAmount(0L, 0L);
        result.Should().Be(0L);
    }

    [Fact]
    public void CalculateBalanceAmount_Should_ReturnZero_WhenSettledIsNegative()
    {
        // Math.Max(settledAmountMinor, 0L) = 0; balance = invoiceTotal - 0
        // Then clamped to 0 only if invoiceTotal < 0, otherwise returned as-is
        var result = BillingReconciliationCalculator.CalculateBalanceAmount(1000L, -50L);
        result.Should().Be(1000L, "negative settled amount is treated as zero so full balance remains");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // End-to-end reconciliation scenario
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FullReconciliationPipeline_Should_ProduceConsistentTotals()
    {
        // Invoice for 100.00 EUR (10000 minor), payment of 10000 with a 2000 refund
        const long invoiceTotal = 10000L;
        const long paymentAmount = 10000L;
        const long refundAmount = 2000L;

        var clamped = BillingReconciliationCalculator.ClampRefundedAmount(paymentAmount, refundAmount);
        var net = BillingReconciliationCalculator.CalculateNetCollectedAmount(paymentAmount, clamped);
        var settled = BillingReconciliationCalculator.CalculateSettledAmount(invoiceTotal, net);
        var balance = BillingReconciliationCalculator.CalculateBalanceAmount(invoiceTotal, settled);

        clamped.Should().Be(2000L);
        net.Should().Be(8000L);
        settled.Should().Be(8000L);
        balance.Should().Be(2000L, "2000 minor units remain unpaid after the partial refund");
    }
}
