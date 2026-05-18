param(
    [switch]$Execute,
    [string]$ExecutablePath = $env:DARWIN_EINVOICE_COMMAND_PATH,
    [string]$Format = $env:DARWIN_EINVOICE_FORMAT,
    [string]$ValidationProfile = $env:DARWIN_EINVOICE_VALIDATION_PROFILE,
    [string]$TempDirectory = $env:DARWIN_EINVOICE_TEMP_DIRECTORY,
    [int]$TimeoutSeconds = 60,
    [long]$MaxArtifactBytes = 20971520
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Format)) {
    $Format = "ZugferdFacturX"
}

if ([string]::IsNullOrWhiteSpace($ValidationProfile)) {
    $ValidationProfile = "external-command-smoke"
}

$normalizedFormat = $Format.Trim()
$supportedFormats = @("ZugferdFacturX", "XRechnung")
$missing = @()

if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $missing += "DARWIN_EINVOICE_COMMAND_PATH"
}

if ($supportedFormats -notcontains $normalizedFormat) {
    Write-Host "E-invoice external-command smoke is blocked. DARWIN_EINVOICE_FORMAT must be ZugferdFacturX or XRechnung."
    exit 2
}

if ($missing.Count -gt 0) {
    Write-Host "E-invoice external-command smoke is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "The command path must point to a deployment-approved generator/validator wrapper. This script does not print secrets, invoice payloads, generated artifacts, or provider output."
    exit 2
}

$trimmedExecutablePath = $ExecutablePath.Trim()
if (-not [System.IO.Path]::IsPathRooted($trimmedExecutablePath)) {
    Write-Host "E-invoice external-command smoke is blocked. DARWIN_EINVOICE_COMMAND_PATH must be an existing absolute file path."
    exit 2
}

$resolvedExecutable = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($trimmedExecutablePath)
if (-not (Test-Path $resolvedExecutable -PathType Leaf)) {
    Write-Host "E-invoice external-command smoke is blocked. DARWIN_EINVOICE_COMMAND_PATH must be an existing absolute file path."
    exit 2
}

if ($TimeoutSeconds -lt 5 -or $TimeoutSeconds -gt 300) {
    Write-Host "E-invoice external-command smoke is blocked. TimeoutSeconds must be between 5 and 300."
    exit 2
}

if ($MaxArtifactBytes -lt 1024 -or $MaxArtifactBytes -gt 104857600) {
    Write-Host "E-invoice external-command smoke is blocked. MaxArtifactBytes must be between 1024 and 104857600."
    exit 2
}

if (-not $Execute) {
    Write-Host "E-invoice external-command smoke configuration is present for format '$normalizedFormat'."
    Write-Host "Run with -Execute to call the configured command through Darwin's IEInvoiceGenerationService adapter. The smoke verifies process execution plus artifact shape only; it is not a substitute for ZUGFeRD/Factur-X, XRechnung, PDF/A-3, EN16931, or legal validation."
    exit 0
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$tempRoot = if ([string]::IsNullOrWhiteSpace($TempDirectory)) {
    Join-Path ([System.IO.Path]::GetTempPath()) ("darwin_einvoice_external_smoke_" + [Guid]::NewGuid().ToString("N"))
}
else {
    Join-Path $TempDirectory.Trim() ("darwin_einvoice_external_smoke_" + [Guid]::NewGuid().ToString("N"))
}

New-Item -ItemType Directory -Path $tempRoot | Out-Null

try {
    $projectPath = Join-Path $tempRoot "Darwin.EInvoiceExternalSmoke.csproj"
    $programPath = Join-Path $tempRoot "Program.cs"
    $infrastructureProject = Join-Path $repoRoot "src\Darwin.Infrastructure\Darwin.Infrastructure.csproj"

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$infrastructureProject" />
  </ItemGroup>
</Project>
"@ | Set-Content -Path $projectPath -Encoding UTF8

    @'
using System.Text.Json;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Domain.Entities.CRM;
using Darwin.Infrastructure.Compliance;
using Microsoft.Extensions.Options;

static string Env(string name) => Environment.GetEnvironmentVariable(name)?.Trim() ?? string.Empty;

var executablePath = Env("DARWIN_EINVOICE_COMMAND_PATH");
var formatName = Env("DARWIN_EINVOICE_FORMAT");
if (string.IsNullOrWhiteSpace(formatName))
{
    formatName = "ZugferdFacturX";
}

var format = Enum.Parse<EInvoiceArtifactFormat>(formatName, ignoreCase: false);
var invoiceId = Guid.NewGuid();
var issuedAtUtc = DateTime.UtcNow;
var source = new
{
    invoiceId,
    currency = "EUR",
    issuedAtUtc,
    totalGrossMinor = 11900,
    issuer = new
    {
        legalName = "Darwin Smoke Seller",
        taxId = "DE123456789",
        addressLine1 = "Seller Street 1",
        postalCode = "10115",
        city = "Berlin",
        country = "DE"
    },
    customer = new
    {
        legalName = "Darwin Smoke Buyer",
        addressLine1 = "Buyer Street 2",
        postalCode = "10115",
        city = "Berlin",
        country = "DE"
    },
    lines = new[]
    {
        new
        {
            description = "Smoke service",
            quantity = 1,
            unitPriceNetMinor = 10000,
            totalNetMinor = 10000,
            totalGrossMinor = 11900
        }
    }
};

var service = new ExternalCommandEInvoiceGenerationService(Options.Create(new ExternalCommandEInvoiceOptions
{
    Enabled = true,
    ExecutablePath = executablePath,
    TempDirectory = Env("DARWIN_EINVOICE_TEMP_DIRECTORY"),
    TimeoutSeconds = int.Parse(Env("DARWIN_EINVOICE_TIMEOUT_SECONDS")),
    MaxArtifactBytes = long.Parse(Env("DARWIN_EINVOICE_MAX_ARTIFACT_BYTES")),
    SupportsZugferdFacturX = true,
    SupportsXRechnung = true,
    ValidationProfile = Env("DARWIN_EINVOICE_VALIDATION_PROFILE")
}));

var result = await service.GenerateAsync(
    new Invoice
    {
        Id = invoiceId,
        IssuedSnapshotJson = JsonSerializer.Serialize(source),
        RowVersion = new byte[] { 1 }
    },
    new EInvoiceGenerationRequest(format));

if (!result.IsGenerated)
{
    throw new InvalidOperationException("E-invoice external-command smoke failed with status " + result.Status + ".");
}

Console.WriteLine("E-invoice external-command smoke generated an artifact through the configured adapter.");
Console.WriteLine("Format: " + result.Artifact!.Format);
Console.WriteLine("Content type: " + result.Artifact.ContentType);
Console.WriteLine("Validation profile: " + result.Artifact.ValidationProfile);
'@ | Set-Content -Path $programPath -Encoding UTF8

    Set-Item -Path Env:DARWIN_EINVOICE_COMMAND_PATH -Value $resolvedExecutable
    Set-Item -Path Env:DARWIN_EINVOICE_FORMAT -Value $normalizedFormat
    Set-Item -Path Env:DARWIN_EINVOICE_VALIDATION_PROFILE -Value $ValidationProfile.Trim()
    Set-Item -Path Env:DARWIN_EINVOICE_TEMP_DIRECTORY -Value $tempRoot
    Set-Item -Path Env:DARWIN_EINVOICE_TIMEOUT_SECONDS -Value $TimeoutSeconds.ToString()
    Set-Item -Path Env:DARWIN_EINVOICE_MAX_ARTIFACT_BYTES -Value $MaxArtifactBytes.ToString()

    dotnet run --project $projectPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item -Path $tempRoot -Recurse -Force
    }
}
