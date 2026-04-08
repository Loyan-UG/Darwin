using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
/// Ensures member commerce contracts keep action metadata and nested snapshots stable across JSON serialization.
/// </summary>
public sealed class MemberCommerceContractsSerializationCompatibilityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void MemberOrderDetail_Should_RoundTripWithActionsAndNestedCollections()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var detail = new MemberOrderDetail
        {
            Id = orderId,
            OrderNumber = "ORD-2026-1001",
            Currency = "EUR",
            PricesIncludeTax = true,
            SubtotalNetMinor = 2500,
            TaxTotalMinor = 475,
            ShippingTotalMinor = 350,
            ShippingMethodId = Guid.NewGuid(),
            ShippingMethodName = "Express",
            ShippingCarrier = "DHL",
            ShippingService = "DHL Express",
            DiscountTotalMinor = 200,
            GrandTotalGrossMinor = 3125,
            Status = "AwaitingPayment",
            BillingAddressJson = "{\"line1\":\"Billing Street 1\"}",
            ShippingAddressJson = "{\"line1\":\"Shipping Street 2\"}",
            CreatedAtUtc = new DateTime(2026, 4, 8, 11, 30, 0, DateTimeKind.Utc),
            Lines =
            [
                new MemberOrderLine
                {
                    Id = Guid.NewGuid(),
                    VariantId = Guid.NewGuid(),
                    Name = "Darwin Blend",
                    Sku = "DB-100",
                    Quantity = 2,
                    UnitPriceGrossMinor = 1400,
                    LineGrossMinor = 2800
                }
            ],
            Payments =
            [
                new MemberOrderPayment
                {
                    Id = Guid.NewGuid(),
                    Provider = "Stripe",
                    ProviderReference = "pi_12345",
                    AmountMinor = 3125,
                    Currency = "EUR",
                    Status = "Pending"
                }
            ],
            Shipments =
            [
                new MemberOrderShipment
                {
                    Id = Guid.NewGuid(),
                    Carrier = "DHL",
                    Service = "Express",
                    TrackingNumber = "JD014600000000000000",
                    Status = "LabelCreated"
                }
            ],
            Invoices =
            [
                new MemberOrderInvoice
                {
                    Id = Guid.NewGuid(),
                    Currency = "EUR",
                    TotalGrossMinor = 3125,
                    Status = "Issued",
                    DueDateUtc = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc)
                }
            ],
            Actions = new MemberOrderActions
            {
                CanRetryPayment = true,
                PaymentIntentPath = $"/api/v1/member/orders/{orderId}/payment-intent",
                ConfirmationPath = $"/api/v1/public/checkout/orders/{orderId}/confirmation",
                DocumentPath = $"/api/v1/member/orders/{orderId}/document"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(detail, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberOrderDetail>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Id.Should().Be(orderId);
        roundTrip.Actions.CanRetryPayment.Should().BeTrue();
        roundTrip.Actions.PaymentIntentPath.Should().EndWith("/payment-intent");
        roundTrip.Shipments.Should().ContainSingle();
        roundTrip.Shipments[0].Carrier.Should().Be("DHL");
        roundTrip.Lines.Should().ContainSingle();
        roundTrip.Lines[0].Sku.Should().Be("DB-100");
    }

    [Fact]
    public void MemberInvoiceDetail_Should_RoundTripWithActionsAndFinancialFields()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var relatedOrderId = Guid.NewGuid();
        var detail = new MemberInvoiceDetail
        {
            Id = invoiceId,
            BusinessId = Guid.NewGuid(),
            BusinessName = "Darwin Cafe",
            OrderId = relatedOrderId,
            OrderNumber = "ORD-2026-1002",
            Currency = "EUR",
            TotalGrossMinor = 4999,
            RefundedAmountMinor = 0,
            SettledAmountMinor = 0,
            BalanceMinor = 4999,
            Status = "Open",
            DueDateUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PaidAtUtc = null,
            CreatedAtUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc),
            TotalNetMinor = 4200,
            TotalTaxMinor = 799,
            PaymentSummary = "Unpaid",
            Lines =
            [
                new MemberInvoiceLine
                {
                    Id = Guid.NewGuid(),
                    Description = "Subscription fee",
                    Quantity = 1,
                    UnitPriceNetMinor = 4200,
                    TaxRate = 0.19m,
                    TotalNetMinor = 4200,
                    TotalGrossMinor = 4999
                }
            ],
            Actions = new MemberInvoiceActions
            {
                CanRetryPayment = true,
                PaymentIntentPath = $"/api/v1/member/invoices/{invoiceId}/payment-intent",
                OrderPath = $"/api/v1/member/orders/{relatedOrderId}",
                DocumentPath = $"/api/v1/member/invoices/{invoiceId}/document"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(detail, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberInvoiceDetail>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Id.Should().Be(invoiceId);
        roundTrip.OrderId.Should().Be(relatedOrderId);
        roundTrip.TotalTaxMinor.Should().Be(799);
        roundTrip.Actions.CanRetryPayment.Should().BeTrue();
        roundTrip.Actions.DocumentPath.Should().EndWith("/document");
        roundTrip.Lines.Should().ContainSingle();
        roundTrip.Lines[0].TaxRate.Should().Be(0.19m);
    }

    [Fact]
    public void MemberOrderDetail_Should_PreserveNullOptionalFields_AndCamelCaseActionNames()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var detail = new MemberOrderDetail
        {
            Id = orderId,
            OrderNumber = "ORD-2026-1003",
            Currency = "EUR",
            Status = "Placed",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            CreatedAtUtc = new DateTime(2026, 4, 8, 13, 0, 0, DateTimeKind.Utc),
            ShippingMethodId = null,
            ShippingMethodName = null,
            ShippingCarrier = null,
            ShippingService = null,
            Payments =
            [
                new MemberOrderPayment
                {
                    Id = Guid.NewGuid(),
                    Provider = "Stripe",
                    ProviderReference = null,
                    AmountMinor = 1000,
                    Currency = "EUR",
                    Status = "Pending",
                    PaidAtUtc = null
                }
            ],
            Actions = new MemberOrderActions
            {
                CanRetryPayment = false,
                PaymentIntentPath = null,
                ConfirmationPath = $"/api/v1/public/checkout/orders/{orderId}/confirmation",
                DocumentPath = $"/api/v1/member/orders/{orderId}/document"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(detail, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberOrderDetail>(json, JsonOptions);

        // Assert
        json.Should().Contain("\"paymentIntentPath\"");
        json.Should().Contain("\"confirmationPath\"");
        json.Should().Contain("\"documentPath\"");

        roundTrip.Should().NotBeNull();
        roundTrip!.ShippingMethodId.Should().BeNull();
        roundTrip.ShippingCarrier.Should().BeNull();
        roundTrip.Payments.Should().ContainSingle();
        roundTrip.Payments[0].ProviderReference.Should().BeNull();
        roundTrip.Actions.PaymentIntentPath.Should().BeNull();
        roundTrip.Actions.CanRetryPayment.Should().BeFalse();
    }

    [Fact]
    public void MemberInvoiceDetail_Should_PreserveNullOptionalActionPaths_AndSummaryFields()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var detail = new MemberInvoiceDetail
        {
            Id = invoiceId,
            Currency = "EUR",
            TotalGrossMinor = 1500,
            RefundedAmountMinor = 300,
            SettledAmountMinor = 1200,
            BalanceMinor = 0,
            Status = "Paid",
            DueDateUtc = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            PaidAtUtc = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = new DateTime(2026, 4, 8, 14, 0, 0, DateTimeKind.Utc),
            TotalNetMinor = 1260,
            TotalTaxMinor = 240,
            PaymentSummary = "Paid in full",
            Actions = new MemberInvoiceActions
            {
                CanRetryPayment = false,
                PaymentIntentPath = null,
                OrderPath = null,
                DocumentPath = $"/api/v1/member/invoices/{invoiceId}/document"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(detail, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberInvoiceDetail>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Id.Should().Be(invoiceId);
        roundTrip.BalanceMinor.Should().Be(0);
        roundTrip.PaidAtUtc.Should().Be(new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc));
        roundTrip.Actions.PaymentIntentPath.Should().BeNull();
        roundTrip.Actions.OrderPath.Should().BeNull();
        roundTrip.Actions.DocumentPath.Should().EndWith("/document");
    }
}
