using System;

namespace Darwin.Application.Identity.DTOs
{
    public sealed class LinkExternalLoginDto
    {
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;    // "Google", "Microsoft"
        public string ProviderKey { get; set; } = string.Empty; // provider user id
        public string? DisplayName { get; set; }
    }

    public sealed class UnlinkExternalLoginDto
    {
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
    }
}
