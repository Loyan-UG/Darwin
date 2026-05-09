using System.Net;
using System.Text;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Shipping.Dhl;
using FluentAssertions;

namespace Darwin.WebApi.Tests.Services;

public sealed class DhlShipmentProviderClientTests
{
    [Fact]
    public async Task CreateShipmentAsync_Should_SendDhlRequest_And_ParseProviderReferencesAndLabel()
    {
        var labelBytes = Encoding.UTF8.GetBytes("pdf");
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""
                {
                  "items": [
                    {
                      "shipmentNo": "00340434292135100100",
                      "trackingNumber": "JVGL0601234567890123",
                      "label": { "b64": "{{Convert.ToBase64String(labelBytes)}}" }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var client = new DhlShipmentProviderClient(new HttpClient(handler));
        var result = await client.CreateShipmentAsync(
            BuildSettings(),
            new Order { OrderNumber = "ORD-1001" },
            new Shipment { Service = "V01PAK", TotalWeight = 1500 },
            new CheckoutAddressDto
            {
                FullName = "Ada Lovelace",
                Street1 = "Teststrasse 12",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            },
            TestContext.Current.CancellationToken);

        result.ProviderShipmentReference.Should().Be("00340434292135100100");
        result.TrackingNumber.Should().Be("JVGL0601234567890123");
        result.LabelPdfBytes.Should().Equal(labelBytes);
        handler.Request.Should().NotBeNull();
        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.ToString().Should().Be("https://dhl.example.test/orders?validate=false");
        handler.Request.Headers.Contains("dhl-api-key").Should().BeTrue();
        handler.Request.Headers.Authorization!.Scheme.Should().Be("Bearer");

        handler.RequestBody.Should().Contain("\"shipments\"");
        handler.RequestBody.Should().Contain("\"billingNumber\":\"22222222220101\"");
        handler.RequestBody.Should().Contain("\"refNo\":\"ORD-1001\"");
        handler.RequestBody.Should().Contain("\"addressHouse\":\"12\"");
    }

    [Fact]
    public async Task GetLabelAsync_Should_RequestLabel_And_ParseProviderLabelUrl()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                { "labelUrl": "https://dhl.example.test/labels/00340434292135100100.pdf" }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var client = new DhlShipmentProviderClient(new HttpClient(handler));
        var result = await client.GetLabelAsync(
            BuildSettings(),
            new Shipment { ProviderShipmentReference = "00340434292135100100" },
            TestContext.Current.CancellationToken);

        result.ProviderLabelUrl.Should().Be("https://dhl.example.test/labels/00340434292135100100.pdf");
        handler.Request.Should().NotBeNull();
        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri!.ToString().Should().Be("https://dhl.example.test/orders?shipment=00340434292135100100");
    }

    private static SiteSetting BuildSettings()
    {
        return new SiteSetting
        {
            DhlApiBaseUrl = "https://dhl.example.test/",
            DhlApiKey = "test-api-key",
            DhlApiSecret = "test-api-token",
            DhlAccountNumber = "22222222220101",
            DhlShipperName = "Darwin Shop",
            DhlShipperStreet = "Senderstrasse 1",
            DhlShipperPostalCode = "10115",
            DhlShipperCity = "Berlin",
            DhlShipperCountry = "DE",
            DhlShipperEmail = "shipper@example.test",
            DhlShipperPhoneE164 = "+491111111111"
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            if (request.Content is not null)
            {
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return _response;
        }
    }
}
