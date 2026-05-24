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
    public async Task GenerateAsync_Should_Pass_Validation_Profile_Argument_To_Command()
    {
        var root = CreateTempRoot();
        const string validationProfile = "smoke-profile";
        try
        {
            var command = WriteGeneratorScript(root, $$"""
            if not "%7"=="{{validationProfile}}" exit /b 11
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root, validationProfile: validationProfile);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
            result.Artifact!.ValidationProfile.Should().Be(validationProfile);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Pass_Validation_Report_Path_To_Command()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, """
            if "%8"=="" exit /b 14
            if not exist "%8" exit /b 15
            echo %%PDF-1.7>%output%
            powershell -NoProfile -ExecutionPolicy Bypass -Command "Set-Content -Path '%8' -Value '{\"isValid\":true,\"issues\":[\"ok\"]}'"
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_Generator_Report_Failure()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, """
            echo %%PDF-1.7>%output%
            powershell -NoProfile -ExecutionPolicy Bypass -Command "Set-Content -Path '%8' -Value '{\"isValid\":false,\"issues\":[\"profile not supported\"]}'"
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("reported failed legal validation");
            result.Message.Should().Contain("profile not supported");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_Generator_Invalid_Validation_Report_Format()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, """
            if "%8"=="" exit /b 14
            if not exist "%8" exit /b 15
            echo %%PDF-1.7>%output%
            powershell -NoProfile -ExecutionPolicy Bypass -Command "Set-Content -Path '%8' -Value '[\"validation\", \"report\", \"invalid\"]'"
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("invalid validation report");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Succeed_When_Validation_Report_File_Is_Omitted()
    {
        var root = CreateTempRoot();
        try
        {
            var command = WriteGeneratorScript(root, """
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
            result.Artifact!.Format.Should().Be(EInvoiceArtifactFormat.ZugferdFacturX);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Accept_Validation_Report_Without_Recognized_Validation_Flags()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-no-recognized-keys.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_False_Numeric_Validation_Flag()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-numeric-flag.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("reported failed legal validation");
            result.Message.Should().Contain("numeric false flag");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_False_Boolean_Validation_Flag()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-bool-false.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("reported failed legal validation");
            result.Message.Should().Contain("boolean failed flag");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Theory]
    [InlineData("einvoice-validation-report-valid-alt-key.json")]
    [InlineData("einvoice-validation-report-valid-string-boolean.json")]
    [InlineData("einvoice-validation-report-valid-number-flag.json")]
    public async Task GenerateAsync_Should_Accept_Validation_Report_Truth_Alternate_Flags(string fixtureName)
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath(fixtureName);
            File.Exists(fixturePath).Should().BeTrue();
            var command = WriteGeneratorScript(root, $$"""
            if not exist "{{fixturePath}}" exit /b 14
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.Generated);
            result.Artifact.Should().NotBeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Emit_Detailed_Message_When_Report_Fails_With_String_Validation_Message()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-string-message.json");
            File.Exists(fixturePath).Should().BeTrue();
            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("The configured e-invoice command reported failed legal validation: missing accounting customer party identifier");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Theory]
    [InlineData("einvoice-validation-report-failed-array-issues.json", "malformed line-level totals; missing buyer country; buyer missing postal code")]
    [InlineData("einvoice-validation-report-failed-mixed-error-fields.json", "xml structure not compliant; legal block missing; missing accounting supplier trade party")]
    public async Task GenerateAsync_Should_Emit_Joined_Issue_Messages_From_Alternate_Fields_When_Report_Fails(
        string fixtureName,
        string expectedIssueText)
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath(fixtureName);
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("The configured e-invoice command reported failed legal validation:");
            result.Message.Should().Contain(expectedIssueText);
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Ignore_NonString_Issue_Entries_And_Keep_Only_NonEmpty_Text()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-non-string-issues.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Be("The configured e-invoice command reported failed legal validation: malformed issue array entry");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Extract_Message_From_Object_Typed_Issue_Entries()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-object-issues.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("missing buyer tax ID");
            result.Message.Should().Contain("issuer line item total out of range");
            result.Message.Should().Contain("line 2 has invalid unit amount");
            result.Message.Should().Contain("legacy validation text");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_When_Validation_Status_Reports_Failed_With_Nested_Result()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-nested-status.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("validation failed for profile profile-extended");
            result.Message.Should().Contain("EN16931: Missing accounting party identifier");
            result.Message.Should().Contain("legal identifier is required");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Extract_Issues_From_Nested_Validation_Result_Containers()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-nested-containers.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("line-level validation failed");
            result.Message.Should().Contain("tax ID missing in nested issue");
            result.Message.Should().Contain("nested container object message");
            result.Message.Should().Contain("root message");
            result.Message.Should().Contain("top level error object message");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_When_Validation_Result_Container_Reports_Failed()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-selected-tool-result.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("reported failed legal validation");
            result.Message.Should().Contain("selected-tool command reported signature-related validation errors");
            result.Message.Should().Contain("selected-tool signature validation failed");
            result.Artifact.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_When_Validation_Report_RootStatus_Reports_Failed_Uppercase()
    {
        var root = CreateTempRoot();
        try
        {
            var fixturePath = GetFixturePath("einvoice-validation-report-failed-root-status-uppercase.json");
            File.Exists(fixturePath).Should().BeTrue();

            var command = WriteGeneratorScript(root, $$"""
            copy "{{fixturePath}}" "%8" >nul
            echo %%PDF-1.7>%output%
            """);

            var service = CreateService(command, root);

            var result = await service.GenerateAsync(
                CreateInvoice(),
                new EInvoiceGenerationRequest(EInvoiceArtifactFormat.ZugferdFacturX),
                TestContext.Current.CancellationToken);

            result.Status.Should().Be(EInvoiceGenerationStatus.ValidationFailed);
            result.Message.Should().Contain("reported failed legal validation");
            result.Message.Should().Contain("selected-tool command reported signature-related validation errors");
            result.Message.Should().Contain("selected-tool signature validation failed");
            result.Artifact.Should().BeNull();
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
        long maxArtifactBytes = 1024 * 1024,
        string validationProfile = "test-profile")
        => new(Options.Create(new ExternalCommandEInvoiceOptions
        {
            Enabled = true,
            ExecutablePath = commandPath,
            TempDirectory = tempDirectory,
            TimeoutSeconds = 10,
            SupportsZugferdFacturX = true,
            SupportsXRechnung = supportsXRechnung,
            MaxArtifactBytes = maxArtifactBytes,
            ValidationProfile = validationProfile
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

    private static string GetFixturePath(string fileName)
    {
        var repositoryRoot = FindRepositoryRoot();
        return Path.Combine(repositoryRoot, "tests", "Darwin.Infrastructure.Tests", "Fixtures", fileName);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Darwin repository root from '{AppContext.BaseDirectory}'.");
    }
}
