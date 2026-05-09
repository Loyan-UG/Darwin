namespace Darwin.Infrastructure.Compliance;

public sealed class ViesVatValidationOptions
{
    public bool Enabled { get; set; }
    public string EndpointUrl { get; set; } = "https://ec.europa.eu/taxation_customs/vies/services/checkVatService";
    public int TimeoutSeconds { get; set; } = 15;
}
