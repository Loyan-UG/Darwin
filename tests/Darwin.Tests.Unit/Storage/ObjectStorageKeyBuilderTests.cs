using Darwin.Application.Abstractions.Storage;
using FluentAssertions;

namespace Darwin.Tests.Unit.Storage;

public sealed class ObjectStorageKeyBuilderTests
{
    [Fact]
    public void Build_Should_Normalize_Trusted_Segments()
    {
        var key = ObjectStorageKeyBuilder.Build(" invoices ", "2026", "05", "invoice 1");

        key.Should().Be("invoices/2026/05/invoice-1");
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../invoice")]
    [InlineData("invoice/one")]
    [InlineData("invoice\\one")]
    [InlineData("invoice\u0001")]
    public void Build_Should_Reject_Traversal_And_Control_Characters(string segment)
    {
        var action = () => ObjectStorageKeyBuilder.Build("invoices", segment);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForInvoiceArchive_Should_Use_Deterministic_Compliance_Prefix()
    {
        var invoiceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var artifactId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var key = ObjectStorageKeyBuilder.ForInvoiceArchive(
            invoiceId,
            new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc),
            "issued-snapshot",
            artifactId);

        key.Should().Be("invoices/2026/05/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/issued-snapshot/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
    }
}
