using System;

namespace Darwin.Application.Businesses.DTOs
{
    public sealed class BusinessOnboardingCustomerProfileDto
    {
        public Guid? CustomerId { get; set; }
        public bool IsProvisioned { get; set; }
        public bool CanProvision { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CompanyName { get; set; }
        public string? MissingReason { get; set; }
    }

    public sealed class EnsureBusinessOnboardingCustomerResultDto
    {
        public Guid? CustomerId { get; set; }
        public bool WasCreated { get; set; }
        public bool WasUpdated { get; set; }
        public bool CanProvision { get; set; }
        public string? MissingReason { get; set; }
    }
}
