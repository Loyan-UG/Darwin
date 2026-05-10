using System.Net;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Compliance;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Darwin.WebApi.Tests.Services;

/// <summary>
///     Covers the phase-one VIES policy without making external network calls.
/// </summary>
public sealed class ViesVatValidationProviderTests
{
    [Fact]
    public async Task ValidateAsync_ShouldReturnUnknown_WhenProviderIsDisabled()
    {
        var provider = CreateProvider(enabled: false, new StaticResponseHandler(HttpStatusCode.OK, ValidSoapResponse));

        var result = await provider.ValidateAsync("DE123456789", TestContext.Current.CancellationToken);

        result.Status.Should().Be(CustomerVatValidationStatus.Unknown);
        result.Source.Should().Be("vies.disabled");
        result.Message.Should().Contain("disabled");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenVatIdFormatIsInvalid()
    {
        var provider = CreateProvider(enabled: true, new ThrowingHandler());

        var result = await provider.ValidateAsync("123", TestContext.Current.CancellationToken);

        result.Status.Should().Be(CustomerVatValidationStatus.Invalid);
        result.Source.Should().Be("vies.format");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnUnknown_WhenHttpStatusIsFailure()
    {
        var provider = CreateProvider(enabled: true, new StaticResponseHandler(HttpStatusCode.ServiceUnavailable, "down"));

        var result = await provider.ValidateAsync("DE123456789", TestContext.Current.CancellationToken);

        result.Status.Should().Be(CustomerVatValidationStatus.Unknown);
        result.Source.Should().Be("vies.unavailable");
        result.Message.Should().Contain("HTTP 503");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnUnknown_WhenResponseIsMalformed()
    {
        var provider = CreateProvider(enabled: true, new StaticResponseHandler(HttpStatusCode.OK, "<not-xml"));

        var result = await provider.ValidateAsync("DE123456789", TestContext.Current.CancellationToken);

        result.Status.Should().Be(CustomerVatValidationStatus.Unknown);
        result.Source.Should().Be("vies.unavailable");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnValid_WhenSoapResponseSaysTrue()
    {
        var provider = CreateProvider(enabled: true, new StaticResponseHandler(HttpStatusCode.OK, ValidSoapResponse));

        var result = await provider.ValidateAsync("DE123456789", TestContext.Current.CancellationToken);

        result.Status.Should().Be(CustomerVatValidationStatus.Valid);
        result.Source.Should().Be("vies");
        result.Message.Should().Contain("confirmed");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenSoapResponseSaysFalse()
    {
        var provider = CreateProvider(enabled: true, new StaticResponseHandler(HttpStatusCode.OK, InvalidSoapResponse));

        var result = await provider.ValidateAsync("DE123456789", TestContext.Current.CancellationToken);

        result.Status.Should().Be(CustomerVatValidationStatus.Invalid);
        result.Source.Should().Be("vies");
        result.Message.Should().Contain("invalid");
    }

    [Fact]
    public async Task ValidateAsync_ShouldNormalizeVatIdBeforeSendingRequest()
    {
        var handler = new CapturingResponseHandler(HttpStatusCode.OK, ValidSoapResponse);
        var provider = CreateProvider(enabled: true, handler);

        await provider.ValidateAsync(" de 123-456 789 ", TestContext.Current.CancellationToken);

        handler.RequestBody.Should().Contain("<urn:countryCode>DE</urn:countryCode>");
        handler.RequestBody.Should().Contain("<urn:vatNumber>123456789</urn:vatNumber>");
    }

    private static ViesVatValidationProvider CreateProvider(bool enabled, HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://vies.example.test/")
        };

        return new ViesVatValidationProvider(
            httpClient,
            Options.Create(new ViesVatValidationOptions
            {
                Enabled = enabled,
                EndpointUrl = "https://vies.example.test/"
            }));
    }

    private const string ValidSoapResponse = """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <checkVatResponse xmlns="urn:ec.europa.eu:taxud:vies:services:checkVat:types">
              <valid>true</valid>
            </checkVatResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private const string InvalidSoapResponse = """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <checkVatResponse xmlns="urn:ec.europa.eu:taxud:vies:services:checkVat:types">
              <valid>false</valid>
            </checkVatResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public StaticResponseHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body)
            });
        }
    }

    private sealed class CapturingResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public CapturingResponseHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body)
            };
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The HTTP handler should not be called for invalid input.");
        }
    }
}
