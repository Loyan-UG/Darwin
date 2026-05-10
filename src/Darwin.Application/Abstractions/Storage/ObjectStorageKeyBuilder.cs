using System.Globalization;
using System.Text;

namespace Darwin.Application.Abstractions.Storage;

/// <summary>
/// Builds normalized provider-neutral object keys from trusted application segments.
/// </summary>
public static class ObjectStorageKeyBuilder
{
    public static string Build(params string[] segments)
    {
        if (segments is null || segments.Length == 0)
        {
            throw new ArgumentException("At least one object key segment is required.", nameof(segments));
        }

        return string.Join("/", segments.Select(NormalizeSegment));
    }

    public static string ForInvoiceArchive(Guid invoiceId, DateTime issuedAtUtc, string artifactType, Guid artifactId)
    {
        if (invoiceId == Guid.Empty)
        {
            throw new ArgumentException("Invoice id is required.", nameof(invoiceId));
        }

        if (artifactId == Guid.Empty)
        {
            throw new ArgumentException("Artifact id is required.", nameof(artifactId));
        }

        var issuedUtc = issuedAtUtc.Kind == DateTimeKind.Utc
            ? issuedAtUtc
            : DateTime.SpecifyKind(issuedAtUtc, DateTimeKind.Utc);

        return Build(
            "invoices",
            issuedUtc.Year.ToString("0000", CultureInfo.InvariantCulture),
            issuedUtc.Month.ToString("00", CultureInfo.InvariantCulture),
            invoiceId.ToString("N"),
            artifactType,
            artifactId.ToString("N"));
    }

    public static string NormalizeSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            throw new ArgumentException("Object key segment is required.", nameof(segment));
        }

        var value = segment.Trim();
        if (value is "." or ".." ||
            value.Contains("..", StringComparison.Ordinal) ||
            value.Contains('/', StringComparison.Ordinal) ||
            value.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("Object key segment must not contain path traversal characters.", nameof(segment));
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                throw new ArgumentException("Object key segment must not contain control characters.", nameof(segment));
            }

            builder.Append(char.IsWhiteSpace(ch) ? '-' : ch);
        }

        var normalized = builder.ToString();
        if (normalized.Length > 180)
        {
            throw new ArgumentException("Object key segment is too long.", nameof(segment));
        }

        return normalized;
    }
}
