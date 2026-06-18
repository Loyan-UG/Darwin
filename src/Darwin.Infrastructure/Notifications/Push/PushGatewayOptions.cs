namespace Darwin.Infrastructure.Notifications.Push;

public sealed class PushGatewayOptions
{
    public bool Enabled { get; set; }
    public string? BaseUrl { get; set; }
    public string Endpoint { get; set; } = "/api/push/send";
    public string? ApiKey { get; set; }
    public string Provider { get; set; } = "Fcm";
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxAttempts { get; set; } = 2;
    public int InitialBackoffMilliseconds { get; set; } = 250;
    public string? AndroidChannelId { get; set; }
    public string? ApnsTopic { get; set; }
}
