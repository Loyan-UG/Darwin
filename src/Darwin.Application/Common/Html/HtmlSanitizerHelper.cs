using System;
using System.Linq;

namespace Darwin.Application.Common.Html
{
    /// <summary>
    /// Thin helper around the project's existing sanitizer (wired elsewhere).
    /// Intentionally simple: sanitize nullable HTML fields; returns null if input was null/whitespace.
    /// No external libs introduced here.
    /// </summary>
    public static class HtmlSanitizerHelper
    {
        /// <summary>
        /// Sanitizes a possibly-null HTML string. Returns null when input is null/whitespace or sanitization produced empty output.
        /// </summary>
        public static string? SanitizeOrNull(IHtmlSanitizer sanitizer, string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var cleaned = sanitizer.Sanitize(html);
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned.Trim();
        }
    }

    /// <summary>
    /// Abstraction already present in the solution (as per docs). 
    /// If not present in code, define it here and wire an implementation in Web.
    /// </summary>
    public interface IHtmlSanitizer
    {
        string Sanitize(string html);
    }
}
