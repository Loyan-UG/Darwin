using Darwin.Application.Abstractions.Invoicing;
using Darwin.Domain.Entities.CRM;
using Darwin.Infrastructure.Compliance;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Tests.Compliance;

public sealed class ExternalCommandEInvoiceGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_Should_Reject_Invalid_Pdf_Output_For_ZugferdFacturX()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, "echo not a pdf>%output%");
            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("did not create a PDF artifact");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Accept_Well_Formed_XRechnung_Xml_Output()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, "echo ^<Invoice^>^<Id^>1^</Id^>^</Invoice^>>%output%");
            var service = CreateService(command, root, supportsXRechnung: true);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.XRechnung),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
            result.Artifact!.ContentType.Should().Be("application/xml");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_Output_Larger_Than_Configured_Maximum()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, """
            powershell -NoProfile -ExecutionPolicy Bypass -Command "$content='%%PDF' + ('x' * 2048); [System.IO.File]::WriteAllText($env:output, $content)"
            """);
            var service = CreateService(command, root, maxArtifactBytes: 1024);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("larger than the configured maximum");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static ExternalCommandEInvoiceGenerationService CreateService(
        string commandPath,
        string tempDirectory,
        bool supportsXRechnung = false,
        long maxArtifactBytes = 1024 * 1024)
        => new(Options.Create(new ExternalCommandEInvoiceOptions
        {
            Enabled = true,
            ExecutablePath = commandPath,
            TempDirectory = tempDirectory,
            TimeoutSeconds = 10,
            SupportsZugferdFacturX = true,
            SupportsXRechnung = supportsXRechnung,
            MaxArtifactBytes = maxArtifactBytes,
            ValidationProfile = "test-profile"
        }));

    private static Invoice CreateInvoice()
        => new()
        {
            Id = Guid.NewGuid(),
            IssuedSnapshotJson = "{}",
            RowVersion = new byte[] { 1 }
        };

    private static string WriteGeneratorScript(string root, string body)
    {
        var path = Path.Combine(root, "generator.cmd");
        File.WriteAllText(path, $$"""
        @echo off
        set output=%4
        {{body}}
        exit /b 0
        """);
        return path;
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_einvoice_command_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteDirectory(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
