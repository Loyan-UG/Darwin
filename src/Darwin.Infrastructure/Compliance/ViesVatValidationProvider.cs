using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Darwin.Application.Abstractions.Compliance;
using Darwin.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Compliance;

public sealed partial class ViesVatValidationProvider : IVatValidationProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ViesVatValidationOptions> _options;

    public ViesVatValidationProvider(HttpClient httpClient, IOptions<ViesVatValidationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<VatValidationProviderResult> ValidateAsync(string vatId, CancellationToken ct = default)
    {
        var options = _options.Value;
        var parsed = ParseVatId(vatId);
        if (parsed is null)
        {
            return new VatValidationProviderResult
            {
                Status = CustomerVatValidationStatus.Invalid,
                Source = "vies.format",
                Message = "VAT ID must start with a two-letter EU country code followed by the national VAT number."
            };
        }

        if (!options.Enabled)
        {
            return new VatValidationProviderResult
            {
                Status = CustomerVatValidationStatus.Unknown,
                Source = "vies.disabled",
                Message = "VIES VAT validation provider is disabled in configuration."
            };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new StringContent(BuildEnvelope(parsed.Value.CountryCode, parsed.Value.Number), Encoding.UTF8, "text/xml")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            request.Headers.Add("SOAPAction", string.Empty);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderUnavailable($"VIES returned HTTP {(int)response.StatusCode}.");
            }

            return ParseResponse(body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Xml.XmlException)
        {
            return ProviderUnavailable("VIES VAT validation request failed.");
        }
    }

    private static VatValidationProviderResult ParseResponse(string body)
    {
        var document = XDocument.Parse(body);
        var valid = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "valid")?.Value;
        if (bool.TryParse(valid, out var isValid))
        {
            return new VatValidationProviderResult
            {
                Status = isValid ? CustomerVatValidationStatus.Valid : CustomerVatValidationStatus.Invalid,
                Source = "vies",
                Message = isValid ? "VIES confirmed the VAT ID." : "VIES reported the VAT ID as invalid."
            };
        }

        return ProviderUnavailable("VIES response did not include a valid result.");
    }

    private static VatValidationProviderResult ProviderUnavailable(string message) =>
        new()
        {
            Status = CustomerVatValidationStatus.Unknown,
            Source = "vies.unavailable",
            Message = message
        };

    private static string BuildEnvelope(string countryCode, string number) =>
        $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:urn="urn:ec.europa.eu:taxud:vies:services:checkVat:types">
          <soapenv:Header/>
          <soapenv:Body>
            <urn:checkVat>
              <urn:countryCode>{{countryCode}}</urn:countryCode>
              <urn:vatNumber>{{number}}</urn:vatNumber>
            </urn:checkVat>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static ParsedVatId? ParseVatId(string vatId)
    {
        var normalized = VatIdCleanupRegex().Replace(vatId ?? string.Empty, string.Empty).ToUpperInvariant();
        if (normalized.Length < 4 || !char.IsLetter(normalized[0]) || !char.IsLetter(normalized[1]))
        {
            return null;
        }

        var countryCode = normalized[..2];
        var number = normalized[2..];
        return VatNumberRegex().IsMatch(number) ? new ParsedVatId(countryCode, number) : null;
    }

    [GeneratedRegex("[\\s.\\-_/]")]
    private static partial Regex VatIdCleanupRegex();

    [GeneratedRegex("^[A-Z0-9]{2,20}$")]
    private static partial Regex VatNumberRegex();

    private readonly record struct ParsedVatId(string CountryCode, string Number);
}
