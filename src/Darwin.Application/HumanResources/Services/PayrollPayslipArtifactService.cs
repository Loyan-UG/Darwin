using System.Net;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.Commands;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Services;

public sealed class PayrollPayslipArtifactService
{
    public const string EntityType = "PayrollPayslip";
    public const string ProfileName = "PayrollPayslips";
    public const string ContainerName = "payroll-payslips";
    public const string HtmlContentType = "text/html";
    public const string PdfContentType = "application/pdf";
    public const string TemplateCode = "darwin-payroll-payslip";
    public const string TemplateVersion = "v1";

    private readonly IAppDbContext _db;
    private readonly IObjectStorageService _storage;
    private readonly DocumentRecordService _documents;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public PayrollPayslipArtifactService(IAppDbContext db, IObjectStorageService storage, DocumentRecordService documents, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public bool IsStorageReady()
        => _storage.GetCapabilities(new ObjectStorageContainerSelection(ContainerName, ProfileName: ProfileName)).Provider != ObjectStorageProviderKind.Database;

    public async Task<Result<int>> GenerateForRunAsync(GeneratePayrollPayslipsDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.PayrollRunId == Guid.Empty || dto.RowVersion.Length == 0) return Result<int>.Fail("PayrollPayslipRunRequired");
        if (!IsStorageReady()) return Result<int>.Fail("PayrollPayslipStorageNotReady");

        var run = await _db.Set<PayrollRun>()
            .Include(x => x.Lines)
            .ThenInclude(x => x.Components)
            .FirstOrDefaultAsync(x => x.Id == dto.PayrollRunId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (run is null) return Result<int>.Fail("PayrollRunNotFound");
        if (!((run.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion))) return Result<int>.Fail("ItemConcurrencyConflict");
        if (run.Status != PayrollRunStatus.Approved) return Result<int>.Fail("PayrollPayslipRunMustBeApproved");

        var lines = run.Lines.Where(x => !x.IsDeleted).OrderBy(x => x.EmployeeNumber).ThenBy(x => x.EmployeeName).ToList();
        if (lines.Count == 0) return Result<int>.Fail("PayrollPayslipRunHasNoLines");

        var lineIds = lines.Select(x => x.Id).ToList();
        var existingLineIds = await _db.Set<PayrollPayslip>()
            .Where(x => lineIds.Contains(x.PayrollRunLineId) && !x.IsDeleted)
            .Select(x => x.PayrollRunLineId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existing = existingLineIds.ToHashSet();

        var generated = 0;
        foreach (var line in lines.Where(x => !existing.Contains(x.Id)))
        {
            var payslip = CreatePayslipShell(run, line);
            var htmlBytes = Encoding.UTF8.GetBytes(RenderHtml(run, line, payslip));
            await using var htmlStream = new MemoryStream(htmlBytes);
            var htmlFileName = $"{payslip.PayslipNumber}-source.html";
            var htmlWrite = await _storage.SaveAsync(new ObjectStorageWriteRequest(
                ContainerName,
                BuildObjectKey(run.BusinessId, run.Id, payslip.Id, htmlFileName),
                HtmlContentType,
                htmlFileName,
                htmlStream,
                htmlBytes.LongLength,
                Metadata: new Dictionary<string, string>
                {
                    ["entity-type"] = EntityType,
                    ["artifact-format"] = "html-source",
                    ["template-code"] = TemplateCode,
                    ["template-version"] = TemplateVersion,
                    ["business-id"] = run.BusinessId.ToString("N"),
                    ["payroll-run-id"] = run.Id.ToString("N"),
                    ["employee-id"] = line.EmployeeId.ToString("N")
                },
                OverwritePolicy: ObjectOverwritePolicy.Disallow,
                ProfileName: ProfileName), ct).ConfigureAwait(false);

            var htmlDocument = await _documents.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
                EntityType,
                payslip.Id,
                DocumentRecordKind.StaffDocument,
                $"Payslip {payslip.PayslipNumber} source",
                htmlFileName,
                HtmlContentType,
                htmlWrite.ContentLength,
                htmlWrite.Sha256Hash,
                ProfileName,
                htmlWrite.ContainerName,
                htmlWrite.ObjectKey,
                Visibility: FoundationVisibility.Internal,
                MetadataJson: BuildMetadataJson(run, line, "html-source")), ct).ConfigureAwait(false);
            if (!htmlDocument.Succeeded) return Result<int>.Fail(htmlDocument.Error ?? "PayrollPayslipSourceDocumentRegisterFailed");

            var pdfBytes = RenderPdf(run, line, payslip);
            await using var pdfStream = new MemoryStream(pdfBytes);
            var pdfFileName = $"{payslip.PayslipNumber}.pdf";
            var pdfWrite = await _storage.SaveAsync(new ObjectStorageWriteRequest(
                ContainerName,
                BuildObjectKey(run.BusinessId, run.Id, payslip.Id, pdfFileName),
                PdfContentType,
                pdfFileName,
                pdfStream,
                pdfBytes.LongLength,
                Metadata: new Dictionary<string, string>
                {
                    ["entity-type"] = EntityType,
                    ["artifact-format"] = "pdf",
                    ["template-code"] = TemplateCode,
                    ["template-version"] = TemplateVersion,
                    ["business-id"] = run.BusinessId.ToString("N"),
                    ["payroll-run-id"] = run.Id.ToString("N"),
                    ["employee-id"] = line.EmployeeId.ToString("N")
                },
                OverwritePolicy: ObjectOverwritePolicy.Disallow,
                ProfileName: ProfileName), ct).ConfigureAwait(false);

            var document = await _documents.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
                EntityType,
                payslip.Id,
                DocumentRecordKind.StaffDocument,
                $"Payslip {payslip.PayslipNumber}",
                pdfFileName,
                PdfContentType,
                pdfWrite.ContentLength,
                pdfWrite.Sha256Hash,
                ProfileName,
                pdfWrite.ContainerName,
                pdfWrite.ObjectKey,
                Visibility: FoundationVisibility.Internal,
                MetadataJson: BuildMetadataJson(run, line, "pdf")), ct).ConfigureAwait(false);
            if (!document.Succeeded) return Result<int>.Fail(document.Error ?? "PayrollPayslipDocumentRegisterFailed");

            payslip.DocumentRecordId = document.Value;
            _db.Set<PayrollPayslip>().Add(payslip);
            generated++;
        }

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, run.BusinessId, "PayrollRun", run.Id, "hr.payroll_payslip.generated", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return Result<int>.Ok(generated);
    }

    public async Task<IReadOnlyList<PayrollPayslipListItemDto>> GetForRunAsync(Guid payrollRunId, CancellationToken ct = default)
    {
        if (payrollRunId == Guid.Empty) return Array.Empty<PayrollPayslipListItemDto>();
        return await (
            from payslip in _db.Set<PayrollPayslip>().AsNoTracking()
            join line in _db.Set<PayrollRunLine>().AsNoTracking() on payslip.PayrollRunLineId equals line.Id
            join document in _db.Set<DocumentRecord>().AsNoTracking() on payslip.DocumentRecordId equals document.Id
            where payslip.PayrollRunId == payrollRunId && !payslip.IsDeleted && !line.IsDeleted && !document.IsDeleted
            orderby line.EmployeeNumber, line.EmployeeName
            select new PayrollPayslipListItemDto
            {
                Id = payslip.Id,
                RowVersion = payslip.RowVersion,
                PayrollRunId = payslip.PayrollRunId,
                PayrollRunLineId = payslip.PayrollRunLineId,
                EmployeeId = payslip.EmployeeId,
                DocumentRecordId = payslip.DocumentRecordId,
                PayslipNumber = payslip.PayslipNumber,
                Status = payslip.Status,
                EmployeeNumber = line.EmployeeNumber,
                EmployeeName = line.EmployeeName,
                Currency = payslip.Currency,
                PeriodStartUtc = payslip.PeriodStartUtc,
                PeriodEndUtc = payslip.PeriodEndUtc,
                GrossPayMinor = payslip.GrossPayMinor,
                EmployeeDeductionMinor = payslip.EmployeeDeductionMinor,
                EmployerCostMinor = payslip.EmployerCostMinor,
                NetPayMinor = payslip.NetPayMinor,
                GeneratedAtUtc = payslip.GeneratedAtUtc,
                FileName = document.FileName,
                ContentHash = document.ContentHash ?? string.Empty
            }).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Result<PayrollPayslipDownloadResult>> DownloadAsync(Guid businessId, Guid payslipId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || payslipId == Guid.Empty) return Result<PayrollPayslipDownloadResult>.Fail("PayrollPayslipNotFound");
        var row = await (
            from payslip in _db.Set<PayrollPayslip>().AsNoTracking()
            join document in _db.Set<DocumentRecord>().AsNoTracking() on payslip.DocumentRecordId equals document.Id
            where payslip.Id == payslipId && payslip.BusinessId == businessId && !payslip.IsDeleted && !document.IsDeleted
            select new { Payslip = payslip, Document = document }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return Result<PayrollPayslipDownloadResult>.Fail("PayrollPayslipNotFound");

        var read = await _storage.ReadAsync(new ObjectStorageObjectReference(row.Document.StorageContainer, row.Document.StorageKey, ProfileName: ProfileName), ct).ConfigureAwait(false);
        if (read is null) return Result<PayrollPayslipDownloadResult>.Fail("PayrollPayslipObjectNotFound");
        return Result<PayrollPayslipDownloadResult>.Ok(new PayrollPayslipDownloadResult(
            read.Content,
            string.IsNullOrWhiteSpace(read.ContentType) ? row.Document.ContentType : read.ContentType,
            string.IsNullOrWhiteSpace(read.FileName) ? row.Document.FileName : read.FileName!,
            read.ContentLength ?? row.Document.SizeBytes,
            read.Sha256Hash ?? row.Document.ContentHash));
    }

    private PayrollPayslip CreatePayslipShell(PayrollRun run, PayrollRunLine line)
    {
        var now = _clock.UtcNow;
        return new PayrollPayslip
        {
            Id = Guid.NewGuid(),
            BusinessId = run.BusinessId,
            PayrollRunId = run.Id,
            PayrollRunLineId = line.Id,
            EmployeeId = line.EmployeeId,
            PayslipNumber = BuildPayslipNumber(run, line),
            Status = PayrollPayslipStatus.Generated,
            Currency = run.Currency,
            PeriodStartUtc = run.PeriodStartUtc,
            PeriodEndUtc = run.PeriodEndUtc,
            GrossPayMinor = line.GrossPayMinor,
            EmployeeDeductionMinor = line.EmployeeDeductionMinor,
            EmployerCostMinor = line.EmployerCostMinor,
            NetPayMinor = line.NetPayMinor,
            GeneratedAtUtc = now,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                source = "approved-payroll-run",
                run.Id,
                run.RunNumber,
                run.RuleSetCode,
                run.RuleVersion,
                line.EmployeeId,
                line.EmployeeNumber,
                line.EmployeeName,
                templateCode = TemplateCode,
                templateVersion = TemplateVersion,
                officialFormat = "pdf",
                components = line.Components.Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).Select(x => new { x.ComponentCode, x.DisplayName, x.ComponentType, x.AmountMinor })
            }),
            MetadataJson = JsonSerializer.Serialize(new
            {
                source = "payroll-payslip-artifact",
                format = "pdf",
                templateCode = TemplateCode,
                templateVersion = TemplateVersion,
                visibility = FoundationVisibility.Internal.ToString()
            })
        };
    }

    private static string RenderHtml(PayrollRun run, PayrollRunLine line, PayrollPayslip payslip)
    {
        static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        static string Money(long minor, string currency) => $"{minor / 100m:N2} {H(currency)}";

        var components = new StringBuilder();
        foreach (var component in line.Components.Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ThenBy(x => x.ComponentCode))
        {
            components.Append("<tr><td>")
                .Append(H(component.ComponentCode))
                .Append("</td><td>")
                .Append(H(component.DisplayName))
                .Append("</td><td>")
                .Append(H(component.ComponentType.ToString()))
                .Append("</td><td class=\"num\">")
                .Append(Money(component.AmountMinor, run.Currency))
                .Append("</td></tr>");
        }

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>{{H(payslip.PayslipNumber)}}</title>
  <style>
    body { font-family: Arial, sans-serif; color: #17202a; margin: 32px; }
    header { border-bottom: 1px solid #d7dde5; margin-bottom: 20px; padding-bottom: 12px; }
    h1 { font-size: 22px; margin: 0 0 8px; }
    h2 { font-size: 16px; margin: 24px 0 8px; }
    table { border-collapse: collapse; width: 100%; }
    th, td { border-bottom: 1px solid #e5e9ef; padding: 8px; text-align: left; }
    th { background: #f5f7fa; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .label { color: #667085; font-size: 12px; }
    .value { font-weight: 600; }
    .num { text-align: right; }
  </style>
</head>
<body>
  <header>
    <h1>Payroll Payslip</h1>
    <div class="label">Internal payroll artifact generated from approved payroll run snapshots.</div>
  </header>
  <section class="grid">
    <div><div class="label">Payslip number</div><div class="value">{{H(payslip.PayslipNumber)}}</div></div>
    <div><div class="label">Payroll run</div><div class="value">{{H(run.RunNumber)}}</div></div>
    <div><div class="label">Employee</div><div class="value">{{H(line.EmployeeNumber)}} - {{H(line.EmployeeName)}}</div></div>
    <div><div class="label">Period</div><div class="value">{{run.PeriodStartUtc:yyyy-MM-dd}} - {{run.PeriodEndUtc:yyyy-MM-dd}}</div></div>
    <div><div class="label">Rule version</div><div class="value">{{H(run.JurisdictionCode)}} / {{H(run.RuleSetCode)}} / {{H(run.RuleVersion)}}</div></div>
    <div><div class="label">Generated UTC</div><div class="value">{{payslip.GeneratedAtUtc:O}}</div></div>
  </section>
  <h2>Totals</h2>
  <table>
    <tr><th>Gross pay</th><td class="num">{{Money(line.GrossPayMinor, run.Currency)}}</td></tr>
    <tr><th>Employee deductions</th><td class="num">{{Money(line.EmployeeDeductionMinor, run.Currency)}}</td></tr>
    <tr><th>Employer cost</th><td class="num">{{Money(line.EmployerCostMinor, run.Currency)}}</td></tr>
    <tr><th>Net pay</th><td class="num">{{Money(line.NetPayMinor, run.Currency)}}</td></tr>
  </table>
  <h2>Components</h2>
  <table>
    <thead><tr><th>Code</th><th>Name</th><th>Type</th><th class="num">Amount</th></tr></thead>
    <tbody>{{components}}</tbody>
  </table>
</body>
</html>
""";
    }

    private static byte[] RenderPdf(PayrollRun run, PayrollRunLine line, PayrollPayslip payslip)
    {
        var lines = BuildPdfLines(run, line, payslip);
        var content = BuildPdfTextStream(lines);

        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n",
            $"4 0 obj\n<< /Length {content.Length.ToString(CultureInfo.InvariantCulture)} >>\nstream\n{content}\nendstream\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(builder.Length);
            builder.Append(obj);
        }

        var xrefStart = builder.Length;
        builder.Append("xref\n0 ").Append(objects.Length + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            builder.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append("<< /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append(xrefStart.ToString(CultureInfo.InvariantCulture));
        builder.Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static IReadOnlyList<string> BuildPdfLines(PayrollRun run, PayrollRunLine line, PayrollPayslip payslip)
    {
        static string Money(long minor, string currency) => (minor / 100m).ToString("N2", CultureInfo.InvariantCulture) + " " + currency;

        var lines = new List<string>
        {
            "Payroll Payslip",
            $"Template: {TemplateCode} {TemplateVersion}",
            $"Payslip number: {payslip.PayslipNumber}",
            $"Payroll run: {run.RunNumber}",
            $"Employee: {line.EmployeeNumber} - {line.EmployeeName}",
            $"Period: {run.PeriodStartUtc:yyyy-MM-dd} - {run.PeriodEndUtc:yyyy-MM-dd}",
            $"Jurisdiction/rules: {run.JurisdictionCode} / {run.RuleSetCode} / {run.RuleVersion}",
            $"Generated UTC: {payslip.GeneratedAtUtc:O}",
            string.Empty,
            "Totals",
            $"Gross pay: {Money(line.GrossPayMinor, run.Currency)}",
            $"Employee deductions: {Money(line.EmployeeDeductionMinor, run.Currency)}",
            $"Employer cost: {Money(line.EmployerCostMinor, run.Currency)}",
            $"Net pay: {Money(line.NetPayMinor, run.Currency)}",
            string.Empty,
            "Components"
        };

        foreach (var component in line.Components.Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ThenBy(x => x.ComponentCode))
        {
            lines.Add($"{component.ComponentCode} | {component.DisplayName} | {component.ComponentType} | {Money(component.AmountMinor, run.Currency)}");
        }

        lines.Add(string.Empty);
        lines.Add("Generated from approved payroll run snapshots. Internal payroll document.");
        return lines;
    }

    private static string BuildPdfTextStream(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.Append("BT\n/F1 10 Tf\n14 TL\n40 800 Td\n");

        var remainingLines = 52;
        foreach (var rawLine in lines)
        {
            if (remainingLines <= 0)
            {
                break;
            }

            content.Append('(').Append(EscapePdfText(rawLine)).Append(") Tj\nT*\n");
            remainingLines--;
        }

        content.Append("ET");
        return content.ToString();
    }

    private static string EscapePdfText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            normalized.Append(ch <= 0x7F ? ch : '?');
        }

        return normalized
            .ToString()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static string BuildPayslipNumber(PayrollRun run, PayrollRunLine line)
        => $"{SafeToken(HrCoreSupport.NormalizeCode(run.RunNumber))}-{SafeToken(HrCoreSupport.NormalizeCode(line.EmployeeNumber))}";

    private static string SafeToken(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-');
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "PAYSLIP" : normalized;
    }

    private static string BuildObjectKey(Guid businessId, Guid payrollRunId, Guid payslipId, string fileName)
        => ObjectStorageKeyBuilder.Build("hr", "payroll-payslips", businessId.ToString("N"), payrollRunId.ToString("N"), payslipId.ToString("N") + Path.GetExtension(fileName).ToLowerInvariant());

    private static string BuildMetadataJson(PayrollRun run, PayrollRunLine line, string format)
        => JsonSerializer.Serialize(new
        {
            source = "payroll-payslip-artifact",
            format,
            templateCode = TemplateCode,
            templateVersion = TemplateVersion,
            payrollRunId = run.Id,
            payrollRunNumber = run.RunNumber,
            employeeId = line.EmployeeId,
            privacy = HrPrivacyClassification.Restricted.ToString()
        });
}
