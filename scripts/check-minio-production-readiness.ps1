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
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must be an absolute URL."
        exit 2
    }

    if ($parsed.Scheme -ne "https") {
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must use HTTPS."
        exit 2
    }

    if ($parsed.Host -in @("localhost", "127.0.0.1", "::1")) {
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must point to the production endpoint, not a loopback URL."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace($parsed.UserInfo) -or
        -not [string]::IsNullOrWhiteSpace($parsed.Query) -or
        -not [string]::IsNullOrWhiteSpace($parsed.Fragment) -or
        ($parsed.AbsolutePath -ne "/" -and -not [string]::IsNullOrWhiteSpace($parsed.AbsolutePath))) {
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must be the base HTTPS endpoint only. Put bucket names in DARWIN_MINIO_PRODUCTION_BUCKET and keep access keys, object keys, user info, query strings, and fragments out of readiness input."
        exit 2
    }
}

function Assert-S3BucketName {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    if ($Value.Length -lt 3 -or $Value.Length -gt 63) {
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must be between 3 and 63 characters."
        exit 2
    }

    if ($Value -cnotmatch '^[a-z0-9][a-z0-9-]*[a-z0-9]$' -or $Value.Contains("--")) {
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must use a valid MinIO/S3 bucket label: lowercase letters, numbers, single hyphens, and alphanumeric start/end."
        exit 2
    }
}

function Assert-SafeEvidenceReference {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    if ($Value -match '(?i)(password|secret|token|access[_ -]?key|connection[_ -]?string|private[_ -]?key|provider payload|raw payload)\s*[:=]' -or
        $Value -match '-----BEGIN .*PRIVATE KEY-----' -or
        $Value -match '[{}]') {
        Write-Host "MinIO production readiness is blocked."
        Write-Host "$Name must be a non-secret evidence reference, not a credential, provider payload, policy JSON, object key dump, or private artifact content."
        exit 2
    }
}

$endpoint = Get-EnvValue "DARWIN_MINIO_PRODUCTION_ENDPOINT"
$bucket = Get-EnvValue "DARWIN_MINIO_PRODUCTION_BUCKET"
$selectedProviderSmokeReference = Get-EnvValue "DARWIN_MINIO_SELECTED_PROVIDER_SMOKE_REFERENCE"
$requiredConfirmations = @(
    "DARWIN_MINIO_PRODUCTION_PROVIDER_SELECTED_CONFIRMED",
    "DARWIN_MINIO_TLS_CONFIRMED",
    "DARWIN_MINIO_DEDICATED_KEYS_CONFIRMED",
    "DARWIN_MINIO_LEAST_PRIVILEGE_POLICY_CONFIRMED",
    "DARWIN_MINIO_BUCKET_OBJECT_LOCK_CONFIRMED",
    "DARWIN_MINIO_BUCKET_VERSIONING_CONFIRMED",
    "DARWIN_MINIO_DEFAULT_RETENTION_CONFIRMED",
    "DARWIN_MINIO_RETENTION_MODE_CONFIRMED",
    "DARWIN_MINIO_LEGAL_HOLD_POLICY_CONFIRMED",
    "DARWIN_MINIO_BACKUP_CONFIGURED_CONFIRMED",
    "DARWIN_MINIO_RESTORE_TEST_CONFIRMED",
    "DARWIN_MINIO_MONITORING_CONFIRMED",
    "DARWIN_MINIO_ALERTING_CONFIRMED",
    "DARWIN_MINIO_DARWIN_PROFILE_CONFIGURED_CONFIRMED",
    "DARWIN_MINIO_INVOICE_ARCHIVE_PROFILE_CONFIRMED",
    "DARWIN_MINIO_SHIPMENT_LABELS_PROFILE_CONFIRMED",
    "DARWIN_MINIO_MEDIA_ASSETS_PROFILE_DECIDED_CONFIRMED",
    "DARWIN_MINIO_FINANCE_EXPORTS_PROFILE_CONFIRMED",
    "DARWIN_MINIO_FINANCE_EXPORT_OUTBOUND_PROFILE_CONFIRMED",
    "DARWIN_MINIO_PERSONNEL_DOCUMENTS_PROFILE_CONFIRMED",
    "DARWIN_MINIO_PAYROLL_PAYSLIPS_PROFILE_CONFIRMED",
    "DARWIN_MINIO_DISPOSABLE_SMOKE_PREFIX_CONFIRMED",
    "DARWIN_MINIO_RETENTION_DELETE_BEHAVIOR_CONFIRMED",
    "DARWIN_MINIO_SELECTED_PROVIDER_SMOKE_CONFIRMED",
    "DARWIN_MINIO_OPERATOR_RUNBOOK_CONFIRMED"
)

$missing = @()
if ([string]::IsNullOrWhiteSpace($endpoint)) {
    $missing += "DARWIN_MINIO_PRODUCTION_ENDPOINT"
}

if ([string]::IsNullOrWhiteSpace($bucket)) {
    $missing += "DARWIN_MINIO_PRODUCTION_BUCKET"
}

if ([string]::IsNullOrWhiteSpace($selectedProviderSmokeReference)) {
    $missing += "DARWIN_MINIO_SELECTED_PROVIDER_SMOKE_REFERENCE"
}

foreach ($name in $requiredConfirmations) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "MinIO production readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print MinIO access keys, secret keys, bucket policy JSON, object keys, or provider responses."
    Write-Host "It records operator confirmations only; production immutability still requires real provider validation."
    exit 2
}

Assert-AbsoluteHttpsEndpoint -Name "DARWIN_MINIO_PRODUCTION_ENDPOINT" -Value $endpoint
Assert-S3BucketName -Name "DARWIN_MINIO_PRODUCTION_BUCKET" -Value $bucket
Assert-SafeEvidenceReference -Name "DARWIN_MINIO_SELECTED_PROVIDER_SMOKE_REFERENCE" -Value $selectedProviderSmokeReference

Write-Host "MinIO production readiness prerequisites are present."
Write-Host "No MinIO access key, secret key, object payload, bucket policy, or provider response was accepted or printed."
Write-Host "Selected-provider smoke evidence reference is present and was not printed."
Write-Host "After this preflight passes, keep the selected-provider smoke and WebAdmin smoke evidence attached before claiming provider-level archive immutability."
