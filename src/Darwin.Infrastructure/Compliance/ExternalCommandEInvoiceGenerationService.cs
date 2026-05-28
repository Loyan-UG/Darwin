using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Domain.Entities.CRM;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Compliance;

public sealed class ExternalCommandEInvoiceGenerationService : IEInvoiceGenerationService
{
    private const int MaxOutputChars = 4096;
    private readonly IOptions<ExternalCommandEInvoiceOptions> _options;

    public ExternalCommandEInvoiceGenerationService(IOptions<ExternalCommandEInvoiceOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<EInvoiceGenerationResult> GenerateAsync(
        Invoice invoice,
        EInvoiceGenerationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(request);

        var options = Normalize(_options.Value);
        if (!options.Enabled)
        {
            return NotConfigured();
        }

        if (!IsSupported(request.Format, options))
        {
            return new EInvoiceGenerationResult(
                EInvoiceGenerationStatus.UnsupportedFormat,
                "The configured e-invoice command does not support the requested format.");
        }

        if (string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson))
        {
            return new EInvoiceGenerationResult(
                EInvoiceGenerationStatus.SourceSnapshotUnavailable,
                "Issued invoice snapshot is required before generating an e-invoice artifact.");
        }

        if (string.IsNullOrWhiteSpace(options.ExecutablePath) || !Path.IsPathRooted(options.ExecutablePath) || !File.Exists(options.ExecutablePath))
        {
            return NotConfigured();
        }

        var tempRoot = ResolveTempDirectory(options.TempDirectory);
        Directory.CreateDirectory(tempRoot);
        var workId = Guid.NewGuid().ToString("N");
        var inputPath = Path.Combine(tempRoot, $"darwin-einvoice-{workId}-source.json");
        var outputPath = Path.Combine(tempRoot, $"darwin-einvoice-{workId}{ResolveFileExtension(request.Format)}");
        var validationReportPath = Path.Combine(tempRoot, $"darwin-einvoice-{workId}-validation-report.json");

        try
        {
            await File.WriteAllTextAsync(inputPath, invoice.IssuedSnapshotJson, Encoding.UTF8, ct).ConfigureAwait(false);
            await using (File.Create(validationReportPath))
            {
            }
            var result = await RunCommandAsync(
                options,
                request.Format,
                inputPath,
                outputPath,
                validationReportPath,
                ct).ConfigureAwait(false);
            if (!result.Success)
            {
                return new EInvoiceGenerationResult(
                    EInvoiceGenerationStatus.ValidationFailed,
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "The configured e-invoice command failed."
                        : result.Message);
            }

            if (!File.Exists(outputPath))
            {
                return new EInvoiceGenerationResult(
                    EInvoiceGenerationStatus.ValidationFailed,
                    "The configured e-invoice command completed without creating an artifact.");
            }

            var fileInfo = new FileInfo(outputPath);
            if (fileInfo.Length > options.MaxArtifactBytes)
            {
                return new EInvoiceGenerationResult(
                    EInvoiceGenerationStatus.ValidationFailed,
                    "The configured e-invoice command created an artifact larger than the configured maximum.");
            }

            var content = await File.ReadAllBytesAsync(outputPath, ct).ConfigureAwait(false);
            if (content.Length == 0)
            {
                return new EInvoiceGenerationResult(
                    EInvoiceGenerationStatus.ValidationFailed,
                    "The configured e-invoice command created an empty artifact.");
            }

            var validationMessage = ValidateArtifactContent(request.Format, content);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return new EInvoiceGenerationResult(
                    EInvoiceGenerationStatus.ValidationFailed,
                    validationMessage);
            }

            var reportMessage = ValidateValidationReport(validationReportPath, options.RequireValidationReport);
            if (!string.IsNullOrWhiteSpace(reportMessage))
            {
                return new EInvoiceGenerationResult(
                    EInvoiceGenerationStatus.ValidationFailed,
                    reportMessage);
            }

            return new EInvoiceGenerationResult(
                EInvoiceGenerationStatus.Generated,
                "The e-invoice artifact was generated by the configured command.",
                new EInvoiceArtifact(
                    invoice.Id,
                    request.Format,
                    ResolveContentType(request.Format),
                    BuildFileName(invoice.Id, request.Format),
                    content,
                    options.ValidationProfile,
                    DateTime.UtcNow));
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(outputPath);
            DeleteIfExists(validationReportPath);
        }
    }

    private static async Task<(bool Success, string Message)> RunCommandAsync(
        ExternalCommandEInvoiceOptions options,
        EInvoiceArtifactFormat format,
        string inputPath,
        string outputPath,
        string validationReportPath,
        CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo.FileName = options.ExecutablePath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.ArgumentList.Add("--input");
        process.StartInfo.ArgumentList.Add(inputPath);
        process.StartInfo.ArgumentList.Add("--output");
        process.StartInfo.ArgumentList.Add(outputPath);
        process.StartInfo.ArgumentList.Add("--format");
        process.StartInfo.ArgumentList.Add(ResolveFormatArgument(format));
        process.StartInfo.ArgumentList.Add(options.ValidationProfile);
        process.StartInfo.ArgumentList.Add(validationReportPath);

        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            process.StartInfo.WorkingDirectory = options.WorkingDirectory.Trim();
        }

        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) => AppendBounded(output, args.Data);
        process.ErrorDataReceived += (_, args) => AppendBounded(output, args.Data);

        if (!process.Start())
        {
            return (false, "The configured e-invoice command could not be started.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            TryKill(process);
            return (false, "The configured e-invoice command timed out.");
        }

        return process.ExitCode == 0
            ? (true, output.ToString())
            : (false, output.ToString());
    }

    private static ExternalCommandEInvoiceOptions Normalize(ExternalCommandEInvoiceOptions options)
    {
        return new ExternalCommandEInvoiceOptions
        {
            Enabled = options.Enabled,
            ExecutablePath = options.ExecutablePath?.Trim() ?? string.Empty,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? null : options.WorkingDirectory.Trim(),
            TempDirectory = string.IsNullOrWhiteSpace(options.TempDirectory) ? null : options.TempDirectory.Trim(),
            TimeoutSeconds = Math.Clamp(options.TimeoutSeconds, 5, 300),
            MaxArtifactBytes = Math.Clamp(options.MaxArtifactBytes, 1024, 100 * 1024 * 1024),
            SupportsZugferdFacturX = options.SupportsZugferdFacturX,
            SupportsXRechnung = options.SupportsXRechnung,
            ValidationProfile = string.IsNullOrWhiteSpace(options.ValidationProfile) ? "external-command" : options.ValidationProfile.Trim(),
            RequireValidationReport = options.RequireValidationReport
        };
    }

    private static bool IsSupported(EInvoiceArtifactFormat format, ExternalCommandEInvoiceOptions options)
        => format switch
        {
            EInvoiceArtifactFormat.ZugferdFacturX => options.SupportsZugferdFacturX,
            EInvoiceArtifactFormat.XRechnung => options.SupportsXRechnung,
            _ => false
        };

    private static EInvoiceGenerationResult NotConfigured()
        => new(
            EInvoiceGenerationStatus.NotConfigured,
            "A compliant e-invoice generator command is not configured.");

    private static string ResolveTempDirectory(string? configured)
        => string.IsNullOrWhiteSpace(configured)
            ? Path.GetTempPath()
            : configured.Trim();

    private static string ResolveFileExtension(EInvoiceArtifactFormat format)
        => format == EInvoiceArtifactFormat.XRechnung ? ".xml" : ".pdf";

    private static string ResolveContentType(EInvoiceArtifactFormat format)
        => format == EInvoiceArtifactFormat.XRechnung ? "application/xml" : "application/pdf";

    private static string ResolveFormatArgument(EInvoiceArtifactFormat format)
        => format == EInvoiceArtifactFormat.XRechnung ? "xrechnung" : "zugferd-factur-x";

    private static string BuildFileName(Guid invoiceId, EInvoiceArtifactFormat format)
        => format == EInvoiceArtifactFormat.XRechnung
            ? $"invoice-{invoiceId:N}-xrechnung.xml"
            : $"invoice-{invoiceId:N}-zugferd-factur-x.pdf";

    private static string? ValidateArtifactContent(EInvoiceArtifactFormat format, byte[] content)
    {
        return format switch
        {
            EInvoiceArtifactFormat.ZugferdFacturX => LooksLikePdf(content)
                ? null
                : "The configured e-invoice command did not create a PDF artifact for ZUGFeRD/Factur-X.",
            EInvoiceArtifactFormat.XRechnung => LooksLikeXml(content)
                ? null
                : "The configured e-invoice command did not create a valid XML artifact for XRechnung.",
            _ => "The requested e-invoice format is not supported."
        };
    }

    private static string? ValidateValidationReport(string validationReportPath, bool requireValidationReport)
    {
        if (!File.Exists(validationReportPath))
        {
            return requireValidationReport
                ? "The configured e-invoice command did not produce the required validation report."
                : null;
        }

        if (new FileInfo(validationReportPath).Length == 0 ||
            string.IsNullOrWhiteSpace(File.ReadAllText(validationReportPath, Encoding.UTF8)))
        {
            return requireValidationReport
                ? "The configured e-invoice command did not produce the required validation report."
                : null;
        }

        try
        {
            using var stream = File.OpenRead(validationReportPath);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "The configured e-invoice command produced an invalid validation report format.";
            }

            if (!TryGetValidationResult(document.RootElement, out var isValid))
            {
                return requireValidationReport
                    ? "The configured e-invoice command produced a validation report without a recognized pass/fail result."
                    : null;
            }

            if (isValid)
            {
                return null;
            }

            var message = ExtractValidationIssues(document.RootElement);
            return string.IsNullOrWhiteSpace(message)
                ? "The configured e-invoice command reported failed legal validation."
                : $"The configured e-invoice command reported failed legal validation: {message}";
        }
        catch (JsonException)
        {
            return "The configured e-invoice command produced an invalid validation report.";
        }
    }

    private static bool TryGetValidationResult(JsonElement report, out bool isValid)
    {
        isValid = false;
        var parsedResults = new List<bool>();

        if (TryCollectValidationIndicators(report, parsedResults))
        {
            // continue collecting; explicit nested indicators can only narrow confidence.
        }

        if (TryCollectValidationIndicatorsFromContainer(report, "validationResult", parsedResults))
        {
            // keeps behavior aligned for existing nested output.
        }

        if (TryCollectValidationIndicatorsFromContainer(report, "signature", parsedResults))
        {
            // signature-level failures should fail the legal validation path.
        }

        if (TryCollectValidationIndicatorsFromContainer(report, "result", parsedResults))
        {
            // generic command result wrappers are supported.
        }

        if (parsedResults.Count == 0)
        {
            return false;
        }

        isValid = !parsedResults.Any(result => !result);
        return true;
    }

    private static bool TryCollectValidationIndicatorsFromContainer(JsonElement report, string containerName, List<bool> values)
    {
        if (!report.TryGetProperty(containerName, out var containerElement))
        {
            return false;
        }

        return containerElement.ValueKind == JsonValueKind.Object &&
            TryCollectValidationIndicators(containerElement, values);
    }

    private static bool TryCollectValidationIndicators(JsonElement report, List<bool> values)
    {
        var foundValue = false;
        var foundStatus = false;

        foreach (var propertyName in new[] { "isValid", "valid", "passed", "success", "succeeded" })
        {
            if (report.TryGetProperty(propertyName, out var value) &&
                TryGetBooleanValue(value, out var parsed))
            {
                values.Add(parsed);
                foundValue = true;
            }
        }

        foreach (var statusName in new[] { "status", "state" })
        {
            if (!report.TryGetProperty(statusName, out var value) ||
                value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var status = value.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(status))
            {
                continue;
            }

            if (TryGetValidationStatusValue(status, out var parsedStatus))
            {
                values.Add(parsedStatus);
                foundStatus = true;
            }
        }

        return foundValue || foundStatus;
    }

    private static bool TryGetValidationStatusValue(string status, out bool parsed)
    {
        parsed = false;

        if (string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "passed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "pass", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "valid", StringComparison.OrdinalIgnoreCase))
        {
            parsed = true;
            return true;
        }

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "fail", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "error", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "invalid", StringComparison.OrdinalIgnoreCase))
        {
            parsed = false;
            return true;
        }

        return false;
    }

    private static bool TryGetBooleanValue(JsonElement value, out bool parsed)
    {
        parsed = false;

        return value.ValueKind switch
        {
            JsonValueKind.True => (parsed = true, true).Item2,
            JsonValueKind.False => true,
            JsonValueKind.String => bool.TryParse(value.GetString(), out parsed),
            JsonValueKind.Number when value.TryGetDouble(out var valueAsDouble) => (parsed = Math.Abs(valueAsDouble) > double.Epsilon, true).Item2,
            _ => false
        };
    }

    private static string? ExtractValidationIssues(JsonElement report)
    {
        var messages = new List<string>();
        CollectValidationIssueMessages(report, messages);
        return messages.Count == 0 ? null : string.Join("; ", messages);
    }

    private static void CollectValidationIssueMessages(JsonElement report, List<string> messages)
    {
        foreach (var issueName in new[] { "result", "signature" })
        {
            if (!report.TryGetProperty(issueName, out var issueElement))
            {
                continue;
            }

            if (issueElement.ValueKind == JsonValueKind.Object)
            {
                CollectValidationIssueMessages(issueElement, messages);
            }
        }

        foreach (var issueName in new[] { "issues", "errors", "messages", "validationMessages" })
        {
            if (!report.TryGetProperty(issueName, out var value))
            {
                continue;
            }

            CollectValidationIssueMessagesFromValue(value, messages);
        }

        if (report.TryGetProperty("validationResult", out var nestedValidationResult) &&
            nestedValidationResult.ValueKind == JsonValueKind.Object)
        {
            CollectValidationIssueMessages(nestedValidationResult, messages);
        }
    }

    private static void CollectValidationIssueMessagesFromValue(JsonElement value, List<string> messages)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var message = item.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        messages.Add(message);
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    TryAddIssueObjectMessage(item, messages);
                }
            }
            return;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var message = value.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message);
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            TryAddIssueObjectMessage(value, messages);
        }
    }

    private static void TryAddIssueObjectMessage(JsonElement item, List<string> messages)
    {
        foreach (var messageField in new[] { "message", "text", "description" })
        {
            if (item.TryGetProperty(messageField, out var messageProperty) &&
                messageProperty.ValueKind == JsonValueKind.String)
            {
                var message = messageProperty.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    messages.Add(message);
                }

                return;
            }
        }
    }

    private static bool LooksLikePdf(byte[] content)
    {
        var offset = SkipBomAndWhitespace(content);
        return content.Length - offset >= 4 &&
            content[offset] == (byte)'%' &&
            content[offset + 1] == (byte)'P' &&
            content[offset + 2] == (byte)'D' &&
            content[offset + 3] == (byte)'F';
    }

    private static bool LooksLikeXml(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            _ = XDocument.Load(stream, LoadOptions.None);
            return true;
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException)
        {
            return false;
        }
    }

    private static int SkipBomAndWhitespace(byte[] content)
    {
        var offset = content.Length >= 3 &&
            content[0] == 0xEF &&
            content[1] == 0xBB &&
            content[2] == 0xBF
            ? 3
            : 0;

        while (offset < content.Length &&
            (content[offset] == (byte)' ' ||
             content[offset] == (byte)'\t' ||
             content[offset] == (byte)'\r' ||
             content[offset] == (byte)'\n'))
        {
            offset++;
        }

        return offset;
    }

    private static void AppendBounded(StringBuilder builder, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || builder.Length >= MaxOutputChars)
        {
            return;
        }

        var sanitized = value.Trim();
        var remaining = MaxOutputChars - builder.Length;
        builder.AppendLine(sanitized.Length <= remaining ? sanitized : sanitized[..remaining]);
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
