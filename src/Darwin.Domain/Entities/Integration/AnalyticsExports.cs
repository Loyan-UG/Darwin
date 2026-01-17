using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Integration
{
    /// <summary>
    /// Represents an analytics export job requested by a business user (Business app).
    /// The job can generate one or more output files (CSV/PDF/etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This entity is intentionally provider-agnostic. Storage and generation pipelines
    /// are handled outside the Domain layer.
    /// </para>
    /// <para>
    /// Parameters are stored as JSON to support evolving export requirements without schema churn.
    /// </para>
    /// </remarks>
    public sealed class AnalyticsExportJob : BaseEntity
    {
        /// <summary>
        /// Business scope for this export.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// The user who requested the export.
        /// </summary>
        public Guid RequestedByUserId { get; set; }

        /// <summary>
        /// High-level export type (report category).
        /// </summary>
        public AnalyticsReportType ReportType { get; set; } = AnalyticsReportType.Custom;

        /// <summary>
        /// Output format for the export.
        /// </summary>
        public AnalyticsExportFormat Format { get; set; } = AnalyticsExportFormat.Csv;

        /// <summary>
        /// Job lifecycle status.
        /// </summary>
        public AnalyticsExportStatus Status { get; set; } = AnalyticsExportStatus.Pending;

        /// <summary>
        /// JSON parameters for the export (date ranges, filters, etc.).
        /// Example: {"fromUtc":"2026-01-01T00:00:00Z","toUtc":"2026-01-31T23:59:59Z"}.
        /// </summary>
        public string ParametersJson { get; set; } = "{}";

        /// <summary>
        /// When the job started processing (UTC).
        /// </summary>
        public DateTime? StartedAtUtc { get; set; }

        /// <summary>
        /// When the job finished (UTC) (success or failure).
        /// </summary>
        public DateTime? FinishedAtUtc { get; set; }

        /// <summary>
        /// Optional error message for failed jobs (store short; details belong to logs).
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Optional idempotency key to deduplicate repeated user requests.
        /// Enforce uniqueness per business in Infrastructure if used.
        /// </summary>
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Optional retention timestamp for cleanup. If set, files/jobs can be purged after this time.
        /// </summary>
        public DateTime? RetainUntilUtc { get; set; }
    }

    /// <summary>
    /// Represents a generated output file for an <see cref="AnalyticsExportJob"/>.
    /// </summary>
    public sealed class AnalyticsExportFile : BaseEntity
    {
        /// <summary>
        /// Foreign key to the owning export job.
        /// </summary>
        public Guid AnalyticsExportJobId { get; set; }

        /// <summary>
        /// Storage key or path (e.g., blob key). Not necessarily a public URL.
        /// </summary>
        public string StorageKey { get; set; } = string.Empty;

        /// <summary>
        /// Suggested download file name (e.g., "loyalty-visits-2026-01.csv").
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME content type (e.g., "text/csv", "application/pdf").
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Size in bytes (optional but useful for UI).
        /// </summary>
        public long? SizeBytes { get; set; }

        /// <summary>
        /// Optional content hash (e.g., SHA-256 hex) for integrity verification.
        /// </summary>
        public string? ContentHash { get; set; }

        /// <summary>
        /// Optional expiration timestamp for time-limited file access.
        /// </summary>
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
