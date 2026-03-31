namespace Darwin.Contracts.Profile;

public sealed class RequestPhoneVerificationRequest
{
    public string? Channel { get; init; }
}

public sealed class ConfirmPhoneVerificationRequest
{
    public string? Code { get; init; }
}
