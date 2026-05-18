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
    public async Task GenerateAsync_Should_Reject_Malformed_XRechnung_Xml_Output()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, "echo ^<Invoice^>^<Id^>1^</Invoice^>>%output%");
            var service = CreateService(command, root, supportsXRechnung: true);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.XRechnung),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("did not create a valid XML artifact");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Theory]
    [InlineData(EInvoiceArtifactFormat.ZugferdFacturX, "zugferd-factur-x", "%%PDF-1.7")]
    [InlineData(EInvoiceArtifactFormat.XRechnung, "xrechnung", "^<Invoice^>^<Id^>1^</Id^>^</Invoice^>")]
    public async Task GenerateAsync_Should_Pass_Expected_Format_Argument_To_Command(
        EInvoiceArtifactFormat format,
        string expectedCommandFormat,
        string artifactContent)
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, $$"""
            if not "%6"=="{{expectedCommandFormat}}" exit /b 7
            echo {{artifactContent}}>%output%
            """);
            var service = CreateService(command, root, supportsXRechnung: true);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(format),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
            result.Artifact!.Format.Should().Be(format);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Return_UnsupportedFormat_When_XRechnung_Is_Not_Enabled()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, "exit /b 9");
            var service = CreateService(command, root, supportsXRechnung: false);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.XRechnung),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.UnsupportedFormat);
            result.Message.Should().Contain("does not support the requested format");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Return_NotConfigured_When_Command_Path_Is_Not_Absolute()
    {
        var service = CreateService("generator.cmd", Path.GetTempPath());

        var result = await service.GenerateAsync(
            CreateInvoice(),
            new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(EInvoiceGenerationStatus.NotConfigured);
        result.Artifact.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAsync_Should_Bound_Command_Failure_Output()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, """
            powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Output ('x' * 5000); exit 7"
            """);
            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Length.Should().BeLessThanOrEqualTo(4098);
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Delete_Temporary_Input_And_Output_Files_After_Success()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, "echo %%PDF-1.7>%output%");
            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            Directory.GetFiles(root, "darwin-einvoice-*")
                .Should()
                .BeEmpty("the adapter should not leave source snapshots or generated artifacts in the temp directory");
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
