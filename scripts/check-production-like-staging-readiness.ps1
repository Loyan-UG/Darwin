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
        "webhook secret",
        "raw payload",
        "provider payload",
        "private endpoint",
        "customer data",
        "bank identifier",
        "payroll content",
        "invoice payload",
        "keystore"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host "Production-like staging readiness is blocked."
            Write-Host "$Name contains sensitive wording. Use a non-secret evidence reference, run id, build id, ticket id, or approval record id."
            exit 2
        }
    }
}

$stagingLabel = Get-EnvValue "DARWIN_STAGING_REHEARSAL_LABEL"
$releaseReference = Get-EnvValue "DARWIN_STAGING_RELEASE_REFERENCE"
$evidenceReference = Get-EnvValue "DARWIN_STAGING_EVIDENCE_REFERENCE"

$requiredConfirmations = @(
    "DARWIN_STAGING_BUILD_TESTS_CONFIRMED",
    "DARWIN_STAGING_MIGRATION_REHEARSAL_CONFIRMED",
    "DARWIN_STAGING_ROLLBACK_REHEARSAL_CONFIRMED",
    "DARWIN_STAGING_DATABASE_BACKUP_RESTORE_CONFIRMED",
    "DARWIN_STAGING_OBJECT_STORAGE_PREFLIGHTS_CONFIRMED",
    "DARWIN_STAGING_PROVIDER_PREFLIGHTS_CONFIRMED",
    "DARWIN_STAGING_EINVOICE_EVIDENCE_CONFIRMED",
    "DARWIN_STAGING_ANDROID_EVIDENCE_CONFIRMED",
    "DARWIN_STAGING_MONITORING_ALERTING_CONFIRMED",
    "DARWIN_STAGING_OWNER_SIGNOFF_CONFIRMED"
)

$requiredReferences = @(
    "DARWIN_STAGING_BUILD_TESTS_REFERENCE",
    "DARWIN_STAGING_MIGRATION_REHEARSAL_REFERENCE",
    "DARWIN_STAGING_ROLLBACK_REHEARSAL_REFERENCE",
    "DARWIN_STAGING_DATABASE_BACKUP_RESTORE_REFERENCE",
    "DARWIN_STAGING_OBJECT_STORAGE_PREFLIGHTS_REFERENCE",
    "DARWIN_STAGING_PROVIDER_PREFLIGHTS_REFERENCE",
    "DARWIN_STAGING_EINVOICE_EVIDENCE_REFERENCE",
    "DARWIN_STAGING_ANDROID_EVIDENCE_REFERENCE",
    "DARWIN_STAGING_MONITORING_ALERTING_REFERENCE",
    "DARWIN_STAGING_OWNER_SIGNOFF_REFERENCE"
)

$missing = @()
if ([string]::IsNullOrWhiteSpace($stagingLabel)) {
    $missing += "DARWIN_STAGING_REHEARSAL_LABEL"
}

if ([string]::IsNullOrWhiteSpace($releaseReference)) {
    $missing += "DARWIN_STAGING_RELEASE_REFERENCE"
}

if ([string]::IsNullOrWhiteSpace($evidenceReference)) {
    $missing += "DARWIN_STAGING_EVIDENCE_REFERENCE"
}

foreach ($name in $requiredReferences) {
    if ([string]::IsNullOrWhiteSpace((Get-EnvValue $name))) {
        $missing += $name
    }
}

foreach ($name in $requiredConfirmations) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Production-like staging readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print credentials, private endpoints, provider payloads, private artifacts, customer data, bank identifiers, payroll contents, generated e-invoice payloads, or private approval documents."
    Write-Host "It records operator confirmations only; production execution still requires real deployment evidence and final owner approvals."
    exit 2
}

Assert-SafeText -Name "DARWIN_STAGING_REHEARSAL_LABEL" -Value $stagingLabel
Assert-SafeText -Name "DARWIN_STAGING_RELEASE_REFERENCE" -Value $releaseReference
Assert-SafeText -Name "DARWIN_STAGING_EVIDENCE_REFERENCE" -Value $evidenceReference
foreach ($name in $requiredReferences) {
    Assert-SafeText -Name $name -Value (Get-EnvValue $name)
}

Write-Host "Production-like staging readiness prerequisites are present."
Write-Host "No credentials, private endpoints, raw provider payloads, private artifacts, customer data, bank identifiers, payroll contents, generated e-invoice payloads, or private approval documents were accepted or printed."
Write-Host "After this preflight passes, keep the real rehearsal outputs and owner sign-offs in the approved deployment evidence repository."
