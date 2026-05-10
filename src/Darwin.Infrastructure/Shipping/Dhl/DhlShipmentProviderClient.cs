using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Shipping;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;

namespace Darwin.Infrastructure.Shipping.Dhl;

/// <summary>
/// HTTP client for DHL Parcel DE shipment creation and label retrieval.
/// </summary>
public sealed class DhlShipmentProviderClient : IDhlShipmentProviderClient
{
    private readonly HttpClient _httpClient;

    public DhlShipmentProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = _httpClient.Timeout == Timeout.InfiniteTimeSpan
            ? TimeSpan.FromSeconds(45)
            : _httpClient.Timeout;
    }

    public async Task<DhlShipmentCreateResult> CreateShipmentAsync(
        SiteSetting settings,
        Order order,
        Shipment shipment,
        CheckoutAddressDto receiver,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(receiver);

        ConfigureHttpClient(settings);
        using var request = new HttpRequestMessage(HttpMethod.Post, "orders?validate=false")
        {
            Content = new StringContent(BuildCreateShipmentPayload(settings, order, shipment, receiver), Encoding.UTF8, "application/json")
        };
        AddDhlHeaders(request, settings);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DHL shipment creation failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var item = GetFirstItem(root);
        var providerShipmentReference =
            GetString(item, "shipmentNo") ??
            GetString(item, "shipmentNumber") ??
            GetString(root, "shipmentNo") ??
            GetString(root, "shipmentNumber");

        if (string.IsNullOrWhiteSpace(providerShipmentReference))
        {
            throw new InvalidOperationException("DHL shipment creation response did not include a shipment number.");
        }

        return new DhlShipmentCreateResult
        {
            ProviderShipmentReference = providerShipmentReference.Trim(),
            TrackingNumber = GetString(item, "trackingNumber") ?? providerShipmentReference.Trim(),
            LabelPdfBytes = TryGetLabelBytes(item) ?? TryGetLabelBytes(root),
            ProviderLabelUrl = GetLabelUrl(item) ?? GetLabelUrl(root)
        };
    }

    public async Task<DhlShipmentCreateResult> CreateReturnShipmentAsync(
        SiteSetting settings,
        Order order,
        Shipment shipment,
        CheckoutAddressDto returnSender,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(returnSender);

        ConfigureHttpClient(settings);
        using var request = new HttpRequestMessage(HttpMethod.Post, "orders?validate=false")
        {
            Content = new StringContent(BuildCreateReturnShipmentPayload(settings, order, shipment, returnSender), Encoding.UTF8, "application/json")
        };
        AddDhlHeaders(request, settings);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DHL return shipment creation failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var item = GetFirstItem(root);
        var providerShipmentReference =
            GetString(item, "shipmentNo") ??
            GetString(item, "shipmentNumber") ??
            GetString(root, "shipmentNo") ??
            GetString(root, "shipmentNumber");

        if (string.IsNullOrWhiteSpace(providerShipmentReference))
        {
            throw new InvalidOperationException("DHL return shipment creation response did not include a shipment number.");
        }

        return new DhlShipmentCreateResult
        {
            ProviderShipmentReference = providerShipmentReference.Trim(),
            TrackingNumber = GetString(item, "trackingNumber") ?? providerShipmentReference.Trim(),
            LabelPdfBytes = TryGetLabelBytes(item) ?? TryGetLabelBytes(root),
            ProviderLabelUrl = GetLabelUrl(item) ?? GetLabelUrl(root)
        };
    }

    public async Task<DhlShipmentLabelResult> GetLabelAsync(
        SiteSetting settings,
        Shipment shipment,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(shipment);

        if (string.IsNullOrWhiteSpace(shipment.ProviderShipmentReference))
        {
            throw new InvalidOperationException("DHL provider shipment reference is required.");
        }

        ConfigureHttpClient(settings);
        var path = $"orders?shipment={Uri.EscapeDataString(shipment.ProviderShipmentReference.Trim())}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddDhlHeaders(request, settings);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DHL label retrieval failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var item = GetFirstItem(root);
        return new DhlShipmentLabelResult
        {
            LabelPdfBytes = TryGetLabelBytes(item) ?? TryGetLabelBytes(root),
            ProviderLabelUrl = GetLabelUrl(item) ?? GetLabelUrl(root)
        };
    }

    private static string BuildCreateShipmentPayload(SiteSetting settings, Order order, Shipment shipment, CheckoutAddressDto receiver)
    {
        var product = string.IsNullOrWhiteSpace(shipment.Service) ? "V01PAK" : shipment.Service.Trim();
        var weightKg = Math.Max(0.1m, (shipment.TotalWeight ?? 1000) / 1000m);
        var street = SplitStreet(receiver.Street1);

        var payload = new
        {
            profile = string.Equals(settings.DhlEnvironment, "Production", StringComparison.OrdinalIgnoreCase)
                ? "STANDARD_GRUPPENPROFIL"
                : "STANDARD_GRUPPENPROFIL",
            shipments = new[]
            {
                new
                {
                    product,
                    billingNumber = settings.DhlAccountNumber!.Trim(),
                    refNo = order.OrderNumber,
                    shipper = new
                    {
                        name1 = settings.DhlShipperName!.Trim(),
                        addressStreet = settings.DhlShipperStreet!.Trim(),
                        postalCode = settings.DhlShipperPostalCode!.Trim(),
                        city = settings.DhlShipperCity!.Trim(),
                        country = settings.DhlShipperCountry!.Trim().ToUpperInvariant(),
                        email = settings.DhlShipperEmail!.Trim(),
                        phone = settings.DhlShipperPhoneE164!.Trim()
                    },
                    consignee = new
                    {
                        name1 = receiver.FullName.Trim(),
                        name2 = receiver.Company,
                        addressStreet = street.Street,
                        addressHouse = street.HouseNumber,
                        additionalAddressInformation1 = receiver.Street2,
                        postalCode = receiver.PostalCode.Trim(),
                        city = receiver.City.Trim(),
                        country = receiver.CountryCode.Trim().ToUpperInvariant(),
                        phone = receiver.PhoneE164
                    },
                    details = new
                    {
                        dim = new
                        {
                            uom = "mm",
                            height = 100,
                            length = 300,
                            width = 200
                        },
                        weight = new
                        {
                            uom = "kg",
                            value = weightKg
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string BuildCreateReturnShipmentPayload(SiteSetting settings, Order order, Shipment shipment, CheckoutAddressDto returnSender)
    {
        var product = string.IsNullOrWhiteSpace(shipment.Service) ? "V01PAK" : shipment.Service.Trim();
        var weightKg = Math.Max(0.1m, (shipment.TotalWeight ?? 1000) / 1000m);
        var senderStreet = SplitStreet(returnSender.Street1);
        var receiverStreet = SplitStreet(settings.DhlShipperStreet!.Trim());

        var payload = new
        {
            profile = string.Equals(settings.DhlEnvironment, "Production", StringComparison.OrdinalIgnoreCase)
                ? "STANDARD_GRUPPENPROFIL"
                : "STANDARD_GRUPPENPROFIL",
            shipments = new[]
            {
                new
                {
                    product,
                    billingNumber = settings.DhlAccountNumber!.Trim(),
                    refNo = $"RETURN-{order.OrderNumber}",
                    shipper = new
                    {
                        name1 = returnSender.FullName.Trim(),
                        name2 = returnSender.Company,
                        addressStreet = senderStreet.Street,
                        addressHouse = senderStreet.HouseNumber,
                        additionalAddressInformation1 = returnSender.Street2,
                        postalCode = returnSender.PostalCode.Trim(),
                        city = returnSender.City.Trim(),
                        country = returnSender.CountryCode.Trim().ToUpperInvariant(),
                        phone = returnSender.PhoneE164
                    },
                    consignee = new
                    {
                        name1 = settings.DhlShipperName!.Trim(),
                        addressStreet = receiverStreet.Street,
                        addressHouse = receiverStreet.HouseNumber,
                        postalCode = settings.DhlShipperPostalCode!.Trim(),
                        city = settings.DhlShipperCity!.Trim(),
                        country = settings.DhlShipperCountry!.Trim().ToUpperInvariant(),
                        email = settings.DhlShipperEmail!.Trim(),
                        phone = settings.DhlShipperPhoneE164!.Trim()
                    },
                    details = new
                    {
                        dim = new
                        {
                            uom = "mm",
                            height = 100,
                            length = 300,
                            width = 200
                        },
                        weight = new
                        {
                            uom = "kg",
                            value = weightKg
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private void ConfigureHttpClient(SiteSetting settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DhlApiBaseUrl))
        {
            throw new InvalidOperationException("DHL API base URL is not configured.");
        }

        var baseUrl = settings.DhlApiBaseUrl.Trim().TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    }

    private static void AddDhlHeaders(HttpRequestMessage request, SiteSetting settings)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(settings.DhlApiKey))
        {
            request.Headers.TryAddWithoutValidation("dhl-api-key", settings.DhlApiKey.Trim());
        }

        if (!string.IsNullOrWhiteSpace(settings.DhlApiSecret))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.DhlApiSecret.Trim());
        }
    }

    private static JsonElement GetFirstItem(JsonElement root)
    {
        if (root.TryGetProperty("items", out var items) &&
            items.ValueKind == JsonValueKind.Array &&
            items.GetArrayLength() > 0)
        {
            return items[0];
        }

        return root;
    }

    private static string? GetString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static byte[]? TryGetLabelBytes(JsonElement element)
    {
        var base64 =
            GetString(element, "label", "b64") ??
            GetString(element, "label", "base64") ??
            GetString(element, "shipmentLabel", "b64") ??
            GetString(element, "documents", "label", "b64");

        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(base64.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("DHL label response contained invalid base64 data.");
        }
    }

    private static string? GetLabelUrl(JsonElement element)
        => GetString(element, "label", "url") ??
           GetString(element, "labelUrl") ??
           GetString(element, "shipmentLabel", "url");

    private static (string Street, string? HouseNumber) SplitStreet(string streetLine)
    {
        var value = streetLine.Trim();
        var lastSpace = value.LastIndexOf(' ');
        if (lastSpace <= 0 || lastSpace == value.Length - 1)
        {
            return (value, null);
        }

        var suffix = value[(lastSpace + 1)..].Trim();
        if (!suffix.Any(char.IsDigit))
        {
            return (value, null);
        }

        return (value[..lastSpace].Trim(), suffix);
    }
}
