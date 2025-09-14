using System;
using Darwin.Application.Common.Html;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Common
{
    /// <summary>
    /// Unit tests for the <see cref="HtmlSanitizerHelper"/> helper.  These
    /// tests exercise the helper's ability to call a provided
    /// <see cref="IHtmlSanitizer"/> implementation, remove script tags and
    /// their contents, and return a trimmed result.  A simple fake
    /// sanitizer is used to avoid pulling in the full Ganss.Xss library.
    /// </summary>
    public sealed class HtmlSanitizerHelperTests
    {
        /// <summary>
        /// Fake sanitizer that mimics removing script tags and their
        /// contents.  This implementation is intentionally naive and
        /// intended only for testing; production code should use a
        /// robust sanitizer such as the one configured in
        /// <see cref="HtmlSanitizerFactory"/>.
        /// </summary>
        private sealed class FakeSanitizer : IHtmlSanitizer
        {
            public string Sanitize(string html)
            {
                if (string.IsNullOrEmpty(html)) return string.Empty;
                // Remove entire <script>...</script> blocks (case‑insensitive)
                var cleaned = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    "<script[^>]*>.*?</script>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                // Remove any stray script tags left behind
                cleaned = cleaned.Replace("<script>", string.Empty, StringComparison.OrdinalIgnoreCase);
                cleaned = cleaned.Replace("</script>", string.Empty, StringComparison.OrdinalIgnoreCase);
                return cleaned;
            }
        }

        /// <summary>
        /// Sanitize a string containing a script tag and verify that
        /// <see cref="HtmlSanitizerHelper.SanitizeOrNull"/> removes the
        /// script block and trims the result.  The helper should return
        /// only the remaining HTML, free of any script tags.
        /// </summary>
        [Fact]
        public void SanitizeOrNull_Should_Remove_Scripts_And_Trim()
        {
            // Arrange: a string containing a script tag around malicious content
            var input = " <script>alert(1)</script> <b>ok</b> ";
            var sanitizer = new FakeSanitizer();
            // Act: call the helper
            var clean = HtmlSanitizerHelper.SanitizeOrNull(sanitizer, input);
            // Assert: the returned string should not contain any script tags
            clean.Should().NotBeNull();
            clean!.Contains("<script>").Should().BeFalse("the sanitizer must remove script tags entirely");
            // The returned string should be trimmed and equal to the expected HTML
            clean.Should().Be("<b>ok</b>", because: "after removing the script block and trimming, only the <b>ok</b> element should remain");
        }

        /// <summary>
        /// Passing a null string should return null from the helper.  This
        /// protects consumers from null reference exceptions and mirrors
        /// the implementation of <see cref="HtmlSanitizerHelper.SanitizeOrNull"/>.
        /// </summary>
        [Fact]
        public void SanitizeOrNull_Should_Return_Null_For_Null_Input()
        {
            string? input = null;
            var sanitizer = new FakeSanitizer();
            var result = HtmlSanitizerHelper.SanitizeOrNull(sanitizer, input);
            result.Should().BeNull("null input should produce a null output");
        }
    }
}