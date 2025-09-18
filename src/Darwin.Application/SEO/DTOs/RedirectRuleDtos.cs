using System;

namespace Darwin.Application.SEO.DTOs
{
    /// <summary>
    /// Create payload for a redirect rule. FromPath is app-relative (starts with '/').
    /// </summary>
    public sealed class RedirectRuleCreateDto
    {
        public string FromPath { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public bool IsPermanent { get; set; } = true;
    }

    /// <summary>
    /// Edit payload with optimistic concurrency.
    /// </summary>
    public sealed class RedirectRuleEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string FromPath { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public bool IsPermanent { get; set; } = true;
    }

    /// <summary>
    /// Admin list item.
    /// </summary>
    public sealed class RedirectRuleListItemDto
    {
        public Guid Id { get; set; }
        public string FromPath { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public bool IsPermanent { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    /// <summary>
    /// Resolve result returned to Web layer: null if no match.
    /// </summary>
    public sealed class RedirectResolveResult
    {
        public string To { get; set; } = string.Empty;
        public bool IsPermanent { get; set; }
    }
}
