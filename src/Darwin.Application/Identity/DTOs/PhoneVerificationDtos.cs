namespace Darwin.Application.Identity.DTOs;

public enum PhoneVerificationChannel
{
    Sms = 1,
    WhatsApp = 2
}

public sealed class RequestPhoneVerificationDto
{
    public PhoneVerificationChannel? Channel { get; set; }
}

public sealed class ConfirmPhoneVerificationDto
{
    public string Code { get; set; } = string.Empty;
}
