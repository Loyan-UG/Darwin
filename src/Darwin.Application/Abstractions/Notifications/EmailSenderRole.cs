namespace Darwin.Application.Abstractions.Notifications;

/// <summary>
/// Provider-neutral sender identity role used to choose the configured From address.
/// </summary>
public enum EmailSenderRole
{
    /// <summary>Default no-reply sender for transactional and security messages.</summary>
    NoReply = 0,

    /// <summary>Billing sender for subscriptions, contracts, invoices, and payments.</summary>
    Billing = 1,

    /// <summary>Support sender for human support replies and customer-service messages.</summary>
    Support = 2,

    /// <summary>Administrative sender for internal operational alerts.</summary>
    Admin = 3
}
