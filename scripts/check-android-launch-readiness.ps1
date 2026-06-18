param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repoRoot
try {
    $projectReadinessOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-android-project-readiness.ps1" 2>&1
    $projectReadinessExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

foreach ($line in $projectReadinessOutput) {
    Write-Host $line
}

if ($projectReadinessExitCode -ne 0) {
    exit $projectReadinessExitCode
}

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
        "service account",
        "google-services.json",
        "keystore",
        "raw payload",
        "provider payload"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host "Android launch readiness is blocked."
            Write-Host "$Name contains sensitive wording. Use a non-secret artifact label, build number, checksum reference, or external evidence reference."
            exit 2
        }
    }
}

$artifactReference = Get-EnvValue "DARWIN_ANDROID_RELEASE_ARTIFACT_REFERENCE"
$versionName = Get-EnvValue "DARWIN_ANDROID_VERSION_NAME"
$versionCode = Get-EnvValue "DARWIN_ANDROID_VERSION_CODE"
$googleSignInEnabled = Test-Truthy (Get-EnvValue "DARWIN_ANDROID_GOOGLE_SIGN_IN_ENABLED").ToLowerInvariant()

$requiredConfirmations = @(
    "DARWIN_ANDROID_RELEASE_CHANNEL_CONFIRMED",
    "DARWIN_ANDROID_SIGNING_PROFILE_CONFIRMED",
    "DARWIN_ANDROID_SIGNED_ARTIFACT_CONFIRMED",
    "DARWIN_ANDROID_MAPS_CONFIG_CONFIRMED",
    "DARWIN_ANDROID_MAPS_KEY_RESTRICTIONS_CONFIRMED",
    "DARWIN_ANDROID_FIREBASE_CONFIG_CONFIRMED",
    "DARWIN_ANDROID_PUSH_SMOKE_CONFIRMED",
    "DARWIN_ANDROID_CONSUMER_SMOKE_CONFIRMED",
    "DARWIN_ANDROID_BUSINESS_SMOKE_CONFIRMED",
    "DARWIN_ANDROID_CAMERA_QR_SMOKE_CONFIRMED",
    "DARWIN_ANDROID_CLEAR_TEXT_GUARD_CONFIRMED",
    "DARWIN_ANDROID_CERT_TRUST_GUARD_CONFIRMED",
    "DARWIN_ANDROID_ROUTE_COMPATIBILITY_CONFIRMED",
    "DARWIN_ANDROID_EVIDENCE_PACKAGE_CONFIRMED"
)

if ($googleSignInEnabled) {
    $requiredConfirmations += "DARWIN_ANDROID_GOOGLE_SIGN_IN_SMOKE_CONFIRMED"
}

$missing = @()
if ([string]::IsNullOrWhiteSpace($artifactReference)) {
    $missing += "DARWIN_ANDROID_RELEASE_ARTIFACT_REFERENCE"
}

if ([string]::IsNullOrWhiteSpace($versionName)) {
    $missing += "DARWIN_ANDROID_VERSION_NAME"
}

if ([string]::IsNullOrWhiteSpace($versionCode)) {
    $missing += "DARWIN_ANDROID_VERSION_CODE"
}

foreach ($name in $requiredConfirmations) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Android launch readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print signing keys, keystore paths, Firebase service-account credentials, API keys, OAuth secrets, private package artifacts, provider payloads, or device logs."
    Write-Host "It records operator confirmations only; production launch still requires real signed artifacts and device smoke evidence."
    exit 2
}

Assert-SafeText -Name "DARWIN_ANDROID_RELEASE_ARTIFACT_REFERENCE" -Value $artifactReference
Assert-SafeText -Name "DARWIN_ANDROID_VERSION_NAME" -Value $versionName
Assert-SafeText -Name "DARWIN_ANDROID_VERSION_CODE" -Value $versionCode

$parsedVersionCode = 0
if (-not [int]::TryParse($versionCode, [ref]$parsedVersionCode) -or $parsedVersionCode -le 0) {
    Write-Host "Android launch readiness is blocked."
    Write-Host "DARWIN_ANDROID_VERSION_CODE must be a positive integer."
    exit 2
}

Write-Host "Android launch readiness prerequisites are present."
Write-Host "No signing key, keystore path, Firebase credential, API key, OAuth secret, private artifact, provider response, or device log was accepted or printed."
Write-Host "After this preflight passes, record the signed artifact, physical device smoke, push/maps evidence, route compatibility, and owner approvals in the deployment evidence package."
