using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.SEO
{
    /// <summary>
    /// Custom redirect rules to support SEO-friendly 301/302 mappings after slug changes or content moves.
    /// </summary>
    public sealed class RedirectRule : BaseEntity
    {
        /// <summary>Source path (app-relative), e.g., "/old-url". Should not include domain.</summary>
        public string FromPath { get; set; } = string.Empty;
        /// <summary>Destination path or absolute URL, e.g., "/new-url".</summary>
        public string To { get; set; } = string.Empty;
        /// <summary>When true, issue HTTP 301 (permanent); otherwise 302 (temporary).</summary>
        public bool IsPermanent { get; set; } = true;
    }
}