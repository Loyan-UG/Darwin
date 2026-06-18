param()

$ErrorActionPreference = "Stop"

function Get-EnvValue {
    param([Parameter(Mandatory = $true)][string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ($null -eq $value) {
        return ""
    }

    return $value.Trim()
}

function Test-Truthy {
    param([string]$Value)

    return $Value -in @("1", "true", "yes", "y")
}

function Assert-SafeText {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $blocked = @(
        "secret",
        "token",
        "credential",
        "password",
        "privatekey",
        "private key",
        "connectionstring",
        "connection string",
        "accesskey",
        "access key",
        "customer data",
        "invoice payload",
        "raw xml",
        "raw pdf",
        "provider payload"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host "E-invoice production readiness is blocked."
            Write-Host "$Name contains sensitive wording. Use a non-secret tooling label, checksum reference, evidence repository pointer, or approval record id."
            exit 2
        }
    }
}

$toolingReference = Get-EnvValue "DARWIN_EINVOICE_TOOLING_REFERENCE"
$evidenceReference = Get-EnvValue "DARWIN_EINVOICE_EVIDENCE_PACKAGE_REFERENCE"

$requiredConfirmations = @(
    "DARWIN_EINVOICE_TOOLING_PINNED_CONFIRMED",
    "DARWIN_EINVOICE_REQUIRE_VALIDATION_REPORT_CONFIRMED",
    "DARWIN_EINVOICE_ARCHIVE_RETENTION_CONFIRMED",
    "DARWIN_EINVOICE_ZUGFERD_FIXTURES_APPROVED_CONFIRMED",
    "DARWIN_EINVOICE_ZUGFERD_ARTIFACT_GENERATED_CONFIRMED",
    "DARWIN_EINVOICE_ZUGFERD_VALIDATION_REPORT_CONFIRMED",
    "DARWIN_EINVOICE_ZUGFERD_STORAGE_DOWNLOAD_SMOKE_CONFIRMED",
    "DARWIN_EINVOICE_ZUGFERD_ACCOUNTING_SIGNOFF_CONFIRMED",
    "DARWIN_EINVOICE_XRECHNUNG_FIXTURES_APPROVED_CONFIRMED",
    "DARWIN_EINVOICE_XRECHNUNG_ARTIFACT_GENERATED_CONFIRMED",
    "DARWIN_EINVOICE_XRECHNUNG_VALIDATION_REPORT_CONFIRMED",
    "DARWIN_EINVOICE_XRECHNUNG_STORAGE_DOWNLOAD_SMOKE_CONFIRMED",
    "DARWIN_EINVOICE_XRECHNUNG_ACCOUNTING_SIGNOFF_CONFIRMED"
)

$missing = @()
if ([string]::IsNullOrWhiteSpace($toolingReference)) {
    $missing += "DARWIN_EINVOICE_TOOLING_REFERENCE"
}

if ([string]::IsNullOrWhiteSpace($evidenceReference)) {
    $missing += "DARWIN_EINVOICE_EVIDENCE_PACKAGE_REFERENCE"
}

foreach ($name in $requiredConfirmations) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "E-invoice production readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print customer invoices, generated PDF/XML artifacts, private validator credentials, provider payloads, or legal approval documents."
    Write-Host "It records operator confirmations only; compliance still requires real fixture validation, storage/download evidence, and accounting/tax sign-off for both formats."
    exit 2
}

Assert-SafeText -Name "DARWIN_EINVOICE_TOOLING_REFERENCE" -Value $toolingReference
Assert-SafeText -Name "DARWIN_EINVOICE_EVIDENCE_PACKAGE_REFERENCE" -Value $evidenceReference

Write-Host "E-invoice production readiness prerequisites are present for ZUGFeRD/Factur-X and XRechnung."
Write-Host "No customer invoice, generated PDF/XML artifact, validator credential, provider response, legal document, or private approval record was accepted or printed."
Write-Host "After this preflight passes, keep the real artifacts, validation reports, and sign-offs in the approved evidence repository or object storage."
