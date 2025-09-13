using Darwin.Application.Common.Html;
using FluentAssertions;
using Xunit;

public sealed class HtmlSanitizerHelperTests
{
    private sealed class FakeSanitizer : IHtmlSanitizer
    {
        public string Sanitize(string html) => html.Replace("<script>", "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeOrNull_Should_Remove_Scripts_And_Trim()
    {
        var clean = HtmlSanitizerHelper.SanitizeOrNull(new FakeSanitizer(), " <script>alert(1)</script> <b>ok</b> ");
        clean!.Contains("<script>").Should().BeFalse();
        clean.Should().Be("<b>ok</b>");
    }
}
