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

function Assert-AbsoluteHttpsEndpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $parsed = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed)) {
        Write-Host "Azure Blob object-storage readiness is blocked."
        Write-Host "$Name must be an absolute URL."
        exit 2
    }

    if ($parsed.Scheme -ne "https") {
        Write-Host "Azure Blob object-storage readiness is blocked."
        Write-Host "$Name must use HTTPS."
        exit 2
    }

    if ($parsed.Host -in @("localhost", "127.0.0.1", "::1")) {
        Write-Host "Azure Blob object-storage readiness is blocked."
        Write-Host "$Name must point to the production or production-like Azure endpoint, not a loopback URL."
        exit 2
    }
}

function Assert-AzureContainerName {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    if ($Value.Length -lt 3 -or $Value.Length -gt 63) {
        Write-Host "Azure Blob object-storage readiness is blocked."
        Write-Host "$Name must be between 3 and 63 characters."
        exit 2
    }

    if ($Value -cnotmatch '^[a-z0-9][a-z0-9-]*[a-z0-9]$' -or $Value.Contains("--")) {
        Write-Host "Azure Blob object-storage readiness is blocked."
        Write-Host "$Name must use a valid Azure container label: lowercase letters, numbers, single hyphens, and alphanumeric start/end."
        exit 2
    }
}

$endpoint = Get-EnvValue "DARWIN_AZURE_BLOB_PRODUCTION_ENDPOINT"
$container = Get-EnvValue "DARWIN_AZURE_BLOB_PRODUCTION_CONTAINER"
$requiredConfirmations = @(
    "DARWIN_AZURE_BLOB_PROVIDER_SELECTED_CONFIRMED",
    "DARWIN_AZURE_BLOB_TLS_CONFIRMED",
    "DARWIN_AZURE_BLOB_DEDICATED_IDENTITY_CONFIRMED",
    "DARWIN_AZURE_BLOB_LEAST_PRIVILEGE_POLICY_CONFIRMED",
    "DARWIN_AZURE_BLOB_VERSIONING_CONFIRMED",
    "DARWIN_AZURE_BLOB_IMMUTABILITY_POLICY_CONFIRMED",
    "DARWIN_AZURE_BLOB_LEGAL_HOLD_POLICY_CONFIRMED",
    "DARWIN_AZURE_BLOB_BACKUP_CONFIGURED_CONFIRMED",
    "DARWIN_AZURE_BLOB_RESTORE_TEST_CONFIRMED",
    "DARWIN_AZURE_BLOB_MONITORING_CONFIRMED",
    "DARWIN_AZURE_BLOB_ALERTING_CONFIRMED",
    "DARWIN_AZURE_BLOB_DARWIN_PROFILE_CONFIGURED_CONFIRMED",
    "DARWIN_AZURE_BLOB_INVOICE_ARCHIVE_PROFILE_CONFIRMED",
    "DARWIN_AZURE_BLOB_SHIPMENT_LABELS_PROFILE_CONFIRMED",
    "DARWIN_AZURE_BLOB_MEDIA_ASSETS_PROFILE_DECIDED_CONFIRMED",
    "DARWIN_AZURE_BLOB_FINANCE_EXPORTS_PROFILE_CONFIRMED",
    "DARWIN_AZURE_BLOB_FINANCE_EXPORT_OUTBOUND_PROFILE_CONFIRMED",
    "DARWIN_AZURE_BLOB_PERSONNEL_DOCUMENTS_PROFILE_CONFIRMED",
    "DARWIN_AZURE_BLOB_PAYROLL_PAYSLIPS_PROFILE_CONFIRMED",
    "DARWIN_AZURE_BLOB_DISPOSABLE_SMOKE_PREFIX_CONFIRMED",
    "DARWIN_AZURE_BLOB_RETENTION_DELETE_BEHAVIOR_CONFIRMED",
    "DARWIN_AZURE_BLOB_OPERATOR_RUNBOOK_CONFIRMED"
)

$missing = @()
if ([string]::IsNullOrWhiteSpace($endpoint)) {
    $missing += "DARWIN_AZURE_BLOB_PRODUCTION_ENDPOINT"
}

if ([string]::IsNullOrWhiteSpace($container)) {
    $missing += "DARWIN_AZURE_BLOB_PRODUCTION_CONTAINER"
}

foreach ($name in $requiredConfirmations) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Azure Blob object-storage readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print connection strings, account keys, SAS tokens, client secrets, object keys, provider payloads, or private endpoint details."
    Write-Host "It records operator confirmations only; production immutability still requires real Azure policy validation and storage smoke evidence."
    exit 2
}

Assert-AbsoluteHttpsEndpoint -Name "DARWIN_AZURE_BLOB_PRODUCTION_ENDPOINT" -Value $endpoint
Assert-AzureContainerName -Name "DARWIN_AZURE_BLOB_PRODUCTION_CONTAINER" -Value $container

Write-Host "Azure Blob object-storage readiness prerequisites are present."
Write-Host "No connection string, account key, SAS token, client secret, object payload, policy JSON, or provider response was accepted or printed."
Write-Host "After this preflight passes, run the selected-provider smoke against the approved disposable Azure prefix before claiming provider-level archive immutability."
