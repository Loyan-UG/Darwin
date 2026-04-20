using Darwin.Contracts.Identity;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Cart;
using Darwin.Contracts.Catalog;
using Darwin.Contracts.Cms;
using Darwin.Contracts.Loyalty;
using Darwin.Contracts.Profile;
using Darwin.Contracts.Shipping;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Tests.Unit.Contracts;

/// <summary>
///     Ensures JSON payload shapes for critical contracts remain stable for mobile clients.
/// </summary>
public sealed class ContractSerializationCompatibilityCommerceAndContentTests : ContractSerializationCompatibilityTestBase
{
/// <summary>
    ///     Verifies that member order detail contract serializes the expected camelCase payload
    ///     fields for the new member order-history API surface.
    /// </summary>
    [Fact]
    public void MemberOrderDetail_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MemberOrderDetail
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            OrderNumber = "ORD-1001",
            Currency = "EUR",
            GrandTotalGrossMinor = 2599,
            ShippingMethodId = Guid.Parse("12121212-3434-5656-7878-909090909090"),
            ShippingMethodName = "DHL Paket",
            ShippingCarrier = "DHL",
            ShippingService = "Paket",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Lines =
            [
                new MemberOrderLine
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    VariantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "Coffee Beans",
                    Sku = "COF-001",
                    Quantity = 1,
                    UnitPriceGrossMinor = 2599,
                    LineGrossMinor = 2599
                }
            ],
            Actions = new MemberOrderActions
            {
                CanRetryPayment = true,
                PaymentIntentPath = "/api/v1/member/orders/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/payment-intent",
                ConfirmationPath = "/api/v1/public/checkout/orders/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/confirmation",
                DocumentPath = "/api/v1/member/orders/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/document"
            }
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"id\"");
        json.Should().Contain("\"orderNumber\"");
        json.Should().Contain("\"grandTotalGrossMinor\"");
        json.Should().Contain("\"shippingMethodId\"");
        json.Should().Contain("\"shippingMethodName\"");
        json.Should().Contain("\"shippingCarrier\"");
        json.Should().Contain("\"shippingService\"");
        json.Should().Contain("\"billingAddressJson\"");
        json.Should().Contain("\"shippingAddressJson\"");
        json.Should().Contain("\"lines\"");
        json.Should().Contain("\"actions\"");
        json.Should().Contain("\"canRetryPayment\"");
        json.Should().Contain("\"paymentIntentPath\"");
        json.Should().Contain("\"confirmationPath\"");
        json.Should().Contain("\"documentPath\"");
    }

/// <summary>
    ///     Verifies that member invoice detail contract serializes the expected camelCase payload
    ///     fields for the new member invoice-history API surface.
    /// </summary>
    [Fact]
    public void MemberInvoiceDetail_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MemberInvoiceDetail
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            BusinessId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            OrderId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Currency = "EUR",
            TotalGrossMinor = 2599,
            BalanceMinor = 0,
            PaymentSummary = "Stripe | EUR 25.99 | Captured",
            Lines =
            [
                new MemberInvoiceLine
                {
                    Id = Guid.Parse("12121212-3434-5656-7878-909090909090"),
                    Description = "Monthly subscription",
                    Quantity = 1,
                    UnitPriceNetMinor = 2184,
                    TaxRate = 0.19m,
                    TotalNetMinor = 2184,
                    TotalGrossMinor = 2599
                }
            ],
            Actions = new MemberInvoiceActions
            {
                CanRetryPayment = false,
                PaymentIntentPath = null,
                OrderPath = "/api/v1/member/orders/ffffffff-ffff-ffff-ffff-ffffffffffff",
                DocumentPath = "/api/v1/member/invoices/dddddddd-dddd-dddd-dddd-dddddddddddd/document"
            }
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"id\"");
        json.Should().Contain("\"businessId\"");
        json.Should().Contain("\"orderId\"");
        json.Should().Contain("\"totalGrossMinor\"");
        json.Should().Contain("\"balanceMinor\"");
        json.Should().Contain("\"paymentSummary\"");
        json.Should().Contain("\"lines\"");
        json.Should().Contain("\"actions\"");
        json.Should().Contain("\"canRetryPayment\"");
        json.Should().Contain("\"orderPath\"");
        json.Should().Contain("\"documentPath\"");
    }

/// <summary>
    ///     Verifies that public product detail contract serializes storefront-facing fields
    ///     with stable camelCase property names.
    /// </summary>
    [Fact]
    public void PublicProductDetail_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new PublicProductDetail
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Name = "Filterkaffee",
            Slug = "filterkaffee",
            PriceMinor = 1299,
            PrimaryImageUrl = "/media/filterkaffee.jpg",
            Media =
            [
                new PublicProductMedia
                {
                    Id = Guid.Parse("66666666-7777-8888-9999-000000000000"),
                    Url = "/media/filterkaffee.jpg",
                    Alt = "Filterkaffee"
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"primaryImageUrl\"");
        json.Should().Contain("\"priceMinor\"");
        json.Should().Contain("\"media\"");
        json.Should().Contain("\"slug\"");
    }

/// <summary>
    ///     Verifies that public page detail contract serializes storefront-facing fields
    ///     with stable camelCase property names.
    /// </summary>
    [Fact]
    public void PublicPageDetail_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new PublicPageDetail
        {
            Id = Guid.Parse("99999999-aaaa-bbbb-cccc-dddddddddddd"),
            Title = "Über Uns",
            Slug = "uber-uns",
            ContentHtml = "<p>Hallo</p>"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"title\"");
        json.Should().Contain("\"slug\"");
        json.Should().Contain("\"contentHtml\"");
    }

/// <summary>
    ///     Verifies that public cart summary contracts serialize storefront-facing fields
    ///     with stable camelCase names.
    /// </summary>
    [Fact]
    public void PublicCartSummary_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new PublicCartSummary
        {
            CartId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Currency = "EUR",
            SubtotalNetMinor = 1999,
            VatTotalMinor = 380,
            GrandTotalGrossMinor = 2379,
            CouponCode = "WILLKOMMEN10",
            Items =
            [
                new PublicCartItemRow
                {
                    VariantId = Guid.Parse("87654321-4321-4321-4321-210987654321"),
                    Quantity = 2,
                    UnitPriceNetMinor = 999,
                    AddOnPriceDeltaMinor = 100,
                    VatRate = 0.19m,
                    LineNetMinor = 2198,
                    LineVatMinor = 418,
                    LineGrossMinor = 2616,
                    SelectedAddOnValueIdsJson = "[\"11111111-1111-1111-1111-111111111111\"]"
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"cartId\"");
        json.Should().Contain("\"currency\"");
        json.Should().Contain("\"grandTotalGrossMinor\"");
        json.Should().Contain("\"couponCode\"");
        json.Should().Contain("\"selectedAddOnValueIdsJson\"");
    }

/// <summary>
    ///     Verifies that public shipping contracts serialize storefront-facing fields
    ///     with stable camelCase names.
    /// </summary>
    [Fact]
    public void PublicShippingOption_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new PublicShippingOption
        {
            MethodId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
            Name = "DHL Paket",
            PriceMinor = 590,
            Currency = "EUR",
            Carrier = "DHL",
            Service = "Paket"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"methodId\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"priceMinor\"");
        json.Should().Contain("\"currency\"");
        json.Should().Contain("\"carrier\"");
        json.Should().Contain("\"service\"");
    }

/// <summary>
    ///     Verifies that storefront checkout contracts serialize order-placement
    ///     fields with stable camelCase property names.
    /// </summary>
    [Fact]
    public void PlaceOrderFromCartRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new PlaceOrderFromCartRequest
        {
            CartId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            SelectedShippingMethodId = Guid.Parse("12121212-3434-5656-7878-909090909090"),
            ShippingTotalMinor = 590,
            Culture = "de-DE",
            BillingAddress = new CheckoutAddress
            {
                FullName = "Max Mustermann",
                Street1 = "Musterstraße 1",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            },
            ShippingAddress = new CheckoutAddress
            {
                FullName = "Max Mustermann",
                Street1 = "Musterstraße 1",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            }
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"cartId\"");
        json.Should().Contain("\"selectedShippingMethodId\"");
        json.Should().Contain("\"shippingTotalMinor\"");
        json.Should().Contain("\"culture\"");
        json.Should().Contain("\"billingAddress\"");
        json.Should().Contain("\"shippingAddress\"");
    }

/// <summary>
    ///     Verifies that storefront checkout responses serialize created-order
    ///     fields with stable camelCase property names.
    /// </summary>
    [Fact]
    public void PlaceOrderFromCartResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new PlaceOrderFromCartResponse
        {
            OrderId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            OrderNumber = "D-20300101-00001",
            Currency = "EUR",
            GrandTotalGrossMinor = 2590,
            Status = "Created"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"orderId\"");
        json.Should().Contain("\"orderNumber\"");
        json.Should().Contain("\"grandTotalGrossMinor\"");
        json.Should().Contain("\"status\"");
    }

/// <summary>
    ///     Verifies that storefront checkout-intent contracts serialize preview and shipping
    ///     selection fields with stable camelCase names.
    /// </summary>
    [Fact]
    public void CreateCheckoutIntentResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new CreateCheckoutIntentResponse
        {
            CartId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Currency = "EUR",
            SubtotalNetMinor = 3000,
            VatTotalMinor = 570,
            GrandTotalGrossMinor = 3570,
            ShipmentMass = 1500,
            RequiresShipping = true,
            ShippingCountryCode = "DE",
            SelectedShippingMethodId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SelectedShippingTotalMinor = 590,
            ShippingOptions =
            [
                new PublicShippingOption
                {
                    MethodId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Name = "DHL Paket",
                    PriceMinor = 590,
                    Currency = "EUR",
                    Carrier = "DHL",
                    Service = "Paket"
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"shipmentMass\"");
        json.Should().Contain("\"requiresShipping\"");
        json.Should().Contain("\"shippingCountryCode\"");
        json.Should().Contain("\"selectedShippingMethodId\"");
        json.Should().Contain("\"selectedShippingTotalMinor\"");
        json.Should().Contain("\"shippingOptions\"");
    }

/// <summary>
    ///     Verifies that storefront payment-intent contracts serialize payment session
    ///     fields with stable camelCase names.
    /// </summary>
    [Fact]
    public void CreateStorefrontPaymentIntentResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new CreateStorefrontPaymentIntentResponse
        {
            OrderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PaymentId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Provider = "DarwinCheckout",
            ProviderReference = "chk_abc123",
            AmountMinor = 4160,
            Currency = "EUR",
            Status = "Pending",
            CheckoutUrl = "https://payments.example.com/checkout?paymentId=11111111-2222-3333-4444-555555555555",
            ReturnUrl = "https://storefront.example.com/checkout/orders/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/confirmation",
            CancelUrl = "https://storefront.example.com/checkout/orders/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/confirmation?cancelled=true",
            ExpiresAtUtc = new DateTime(2030, 1, 1, 12, 30, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"orderId\"");
        json.Should().Contain("\"paymentId\"");
        json.Should().Contain("\"provider\"");
        json.Should().Contain("\"providerReference\"");
        json.Should().Contain("\"amountMinor\"");
        json.Should().Contain("\"checkoutUrl\"");
        json.Should().Contain("\"returnUrl\"");
        json.Should().Contain("\"cancelUrl\"");
        json.Should().Contain("\"expiresAtUtc\"");
    }

/// <summary>
    ///     Verifies that storefront payment-completion request contracts serialize completion
    ///     fields with stable camelCase names.
    /// </summary>
    [Fact]
    public void CompleteStorefrontPaymentRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new CompleteStorefrontPaymentRequest
        {
            OrderNumber = "D-20300101-00001",
            ProviderReference = "psp_txn_123",
            Outcome = "Succeeded",
            FailureReason = null
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"orderNumber\"");
        json.Should().Contain("\"providerReference\"");
        json.Should().Contain("\"outcome\"");
        json.Should().Contain("\"failureReason\"");
    }

/// <summary>
    ///     Verifies that storefront payment-completion response contracts serialize result
    ///     fields with stable camelCase names.
    /// </summary>
    [Fact]
    public void CompleteStorefrontPaymentResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new CompleteStorefrontPaymentResponse
        {
            OrderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PaymentId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            OrderStatus = "Paid",
            PaymentStatus = "Captured",
            PaidAtUtc = new DateTime(2030, 1, 1, 12, 45, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"orderId\"");
        json.Should().Contain("\"paymentId\"");
        json.Should().Contain("\"orderStatus\"");
        json.Should().Contain("\"paymentStatus\"");
        json.Should().Contain("\"paidAtUtc\"");
    }

/// <summary>
    ///     Verifies that storefront order-confirmation contracts serialize line and payment
    ///     snapshots with stable camelCase names.
    /// </summary>
    [Fact]
    public void StorefrontOrderConfirmationResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new StorefrontOrderConfirmationResponse
        {
            OrderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            OrderNumber = "D-20300101-00001",
            Currency = "EUR",
            ShippingMethodId = Guid.Parse("12121212-3434-5656-7878-909090909090"),
            ShippingMethodName = "DHL Paket",
            ShippingCarrier = "DHL",
            ShippingService = "Paket",
            GrandTotalGrossMinor = 4160,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Lines =
            [
                new StorefrontOrderConfirmationLine
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    VariantId = Guid.Parse("66666666-7777-8888-9999-000000000000"),
                    Name = "Filterkaffee",
                    Sku = "COF-01",
                    Quantity = 2,
                    UnitPriceGrossMinor = 2080,
                    LineGrossMinor = 4160
                }
            ],
            Payments =
            [
                new StorefrontOrderConfirmationPayment
                {
                    Id = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                    Provider = "DarwinCheckout",
                    ProviderReference = "chk_abc123",
                    AmountMinor = 4160,
                    Currency = "EUR",
                    Status = "Pending"
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"orderId\"");
        json.Should().Contain("\"orderNumber\"");
        json.Should().Contain("\"shippingMethodId\"");
        json.Should().Contain("\"shippingMethodName\"");
        json.Should().Contain("\"shippingCarrier\"");
        json.Should().Contain("\"shippingService\"");
        json.Should().Contain("\"lines\"");
        json.Should().Contain("\"payments\"");
        json.Should().Contain("\"providerReference\"");
        json.Should().Contain("\"unitPriceGrossMinor\"");
    }

/// <summary>
    ///     Verifies member order action metadata keeps canonical camelCase property names
    ///     and supports nullable payment-intent paths in serialized payloads.
    /// </summary>
    [Fact]
    public void MemberOrderActions_Should_Serialize_WithExpectedPropertyNames_AndNullability()
    {
        // Arrange
        var dto = new MemberOrderActions
        {
            CanRetryPayment = false,
            PaymentIntentPath = null,
            ConfirmationPath = "/api/v1/public/checkout/orders/11111111-1111-1111-1111-111111111111/confirmation",
            DocumentPath = "/api/v1/member/orders/11111111-1111-1111-1111-111111111111/document"
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberOrderActions>(json, JsonOptions);

        // Assert
        json.Should().Contain("\"canRetryPayment\"");
        json.Should().Contain("\"paymentIntentPath\":null");
        json.Should().Contain("\"confirmationPath\"");
        json.Should().Contain("\"documentPath\"");

        roundTrip.Should().NotBeNull();
        roundTrip!.CanRetryPayment.Should().BeFalse();
        roundTrip.PaymentIntentPath.Should().BeNull();
        roundTrip.ConfirmationPath.Should().Contain("/confirmation");
    }

/// <summary>
    ///     Verifies member invoice action metadata preserves optional paths and stable
    ///     camelCase names across JSON round-trip serialization.
    /// </summary>
    [Fact]
    public void MemberInvoiceActions_Should_Serialize_WithExpectedPropertyNames_AndNullability()
    {
        // Arrange
        var dto = new MemberInvoiceActions
        {
            CanRetryPayment = true,
            PaymentIntentPath = null,
            OrderPath = null,
            DocumentPath = "/api/v1/member/invoices/22222222-2222-2222-2222-222222222222/document"
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberInvoiceActions>(json, JsonOptions);

        // Assert
        json.Should().Contain("\"canRetryPayment\"");
        json.Should().Contain("\"paymentIntentPath\":null");
        json.Should().Contain("\"orderPath\":null");
        json.Should().Contain("\"documentPath\"");

        roundTrip.Should().NotBeNull();
        roundTrip!.CanRetryPayment.Should().BeTrue();
        roundTrip.OrderPath.Should().BeNull();
        roundTrip.DocumentPath.Should().Contain("/document");
    }

/// <summary>
    ///     Verifies member order action payloads stay backward-compatible when
    ///     additional unknown fields are present in JSON responses.
    /// </summary>
    [Fact]
    public void MemberOrderActions_Should_Deserialize_WhenUnknownFieldsExist()
    {
        // Arrange
        const string json = """
            {
              "canRetryPayment": true,
              "paymentIntentPath": "/api/v1/member/orders/1/payment-intent",
              "confirmationPath": "/api/v1/public/checkout/orders/1/confirmation",
              "documentPath": "/api/v1/member/orders/1/document",
              "futureField": "ignore-me"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<MemberOrderActions>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.CanRetryPayment.Should().BeTrue();
        dto.PaymentIntentPath.Should().Contain("/payment-intent");
        dto.ConfirmationPath.Should().Contain("/confirmation");
        dto.DocumentPath.Should().Contain("/document");
    }

/// <summary>
    ///     Verifies member invoice action payloads deserialize with safe defaults
    ///     when optional fields are omitted by older clients or proxies.
    /// </summary>
    [Fact]
    public void MemberInvoiceActions_Should_Deserialize_WhenOptionalFieldsAreMissing()
    {
        // Arrange
        const string json = """
            {
              "canRetryPayment": false,
              "documentPath": "/api/v1/member/invoices/1/document"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<MemberInvoiceActions>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.CanRetryPayment.Should().BeFalse();
        dto.PaymentIntentPath.Should().BeNull();
        dto.OrderPath.Should().BeNull();
        dto.DocumentPath.Should().Contain("/document");
    }
}
