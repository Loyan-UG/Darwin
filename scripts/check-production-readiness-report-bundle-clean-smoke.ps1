param(
    [string]$WorkDirectory = "",
    [switch]$KeepWorkDirectory
)

$ErrorActionPreference = "Stop"

function Test-ContainsSensitivePattern {
    param([Parameter(Mandatory = $true)][string]$Value)

    $blockedPatterns = @(
        "(?i)-----BEGIN (RSA |EC |OPENSSH |DSA |)PRIVATE KEY-----",
        "(?i)\b(password|secret|token|access[_ -]?key|connection[_ -]?string|webhook[_ -]?secret)\s*[:=]\s*\S+",
        "(?i)\b(api[_ -]?key|client[_ -]?secret|refresh[_ -]?token|private[_ -]?key)\s*[:=]\s*\S+",
        "(?i)\b(raw provider payload|provider payload|raw payload)\s*[:=]\s*\{",
        "(?i)\b(private endpoint|customer data|bank identifier|payroll content|invoice payload)\s*[:=]\s*\S+",
        "(?i)\b(keystore|service account|bucket policy|provider response)\s*[:=]\s*\S+",
        "(?i)\b(private artifact|approval document|generated e-invoice payload)\s*[:=]\s*\S+"
    )

    foreach ($pattern in $blockedPatterns) {
        if ($Value -match $pattern) {
            return $true
        }
    }

    return $false
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$createdWorkDirectory = $false
if ([string]::IsNullOrWhiteSpace($WorkDirectory)) {
    $WorkDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "darwin-clean-readiness-$([guid]::NewGuid().ToString("N"))"
    $createdWorkDirectory = $true
}

$resolvedWorkDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($WorkDirectory)
if (Test-Path $resolvedWorkDirectory) {
    Remove-Item -LiteralPath $resolvedWorkDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $resolvedWorkDirectory | Out-Null

$problems = [System.Collections.Generic.List[string]]::new()

Push-Location $repoRoot
try {
    $exportOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\export-production-readiness-report-bundle.ps1" -OutputDirectory $resolvedWorkDirectory -Force 2>&1
    $exportExitCode = $LASTEXITCODE

    $validationOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-production-readiness-report-bundle.ps1" -Directory $resolvedWorkDirectory 2>&1
    $validationExitCode = $LASTEXITCODE

    $goLiveOutput = & {
        $env:DARWIN_PRODUCTION_READINESS_REPORT_BUNDLE_DIRECTORY = $resolvedWorkDirectory
        powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-go-live-readiness.ps1" 2>&1
    }
    $goLiveExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$exportText = ($exportOutput | ForEach-Object { $_.ToString() }) -join "`n"
$validationText = ($validationOutput | ForEach-Object { $_.ToString() }) -join "`n"
$goLiveText = ($goLiveOutput | ForEach-Object { $_.ToString() }) -join "`n"
$goLiveReportPath = Join-Path $resolvedWorkDirectory "go-live-readiness-report.md"
$goLiveReportContent = if (Test-Path $goLiveReportPath -PathType Leaf) { Get-Content $goLiveReportPath -Raw } else { "" }

if ($exportExitCode -ne 0) {
    $problems.Add("Clean report-bundle export failed with exit code $exportExitCode.") | Out-Null
}

if ($validationExitCode -ne 0) {
    $problems.Add("Clean report-bundle validation failed with exit code $validationExitCode.") | Out-Null
}

if ($goLiveExitCode -notin @(0, 2)) {
    $problems.Add("Go-live dry-run against the clean bundle returned unexpected exit code $goLiveExitCode.") | Out-Null
}

if ($validationText.IndexOf("Production readiness report bundle validation passed.", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $problems.Add("Clean bundle validator did not report a passed validation.") | Out-Null
}

if ($goLiveText.IndexOf("Production readiness report bundle: Ready", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $problems.Add("Go-live dry-run did not mark the clean report bundle as Ready.") | Out-Null
}

if ($goLiveReportContent.IndexOf("Production readiness report bundle", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    $problems.Add("Embedded go-live report inside the bundle should skip the report-bundle self-check.") | Out-Null
}

if ((Test-ContainsSensitivePattern $exportText) -or
    (Test-ContainsSensitivePattern $validationText) -or
    (Test-ContainsSensitivePattern $goLiveText) -or
    (Test-ContainsSensitivePattern $goLiveReportContent)) {
    $problems.Add("Clean report-bundle smoke output appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value.") | Out-Null
}

if ($problems.Count -gt 0) {
    Write-Host "Production readiness report bundle clean smoke failed."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }

    if ($KeepWorkDirectory -or -not $createdWorkDirectory) {
        Write-Host "Work directory:"
        Write-Host $resolvedWorkDirectory
    }

    exit 1
}

Write-Host "Production readiness report bundle clean smoke passed."
Write-Host "Clean bundle directory:"
Write-Host $resolvedWorkDirectory
Write-Host "Export exit code: $exportExitCode"
Write-Host "Validation exit code: $validationExitCode"
Write-Host "Go-live exit code: $goLiveExitCode"
Write-Host "The embedded bundle report skipped its self-check, and the top-level go-live dry-run validated the finished clean bundle."

if ($createdWorkDirectory -and -not $KeepWorkDirectory) {
    Remove-Item -LiteralPath $resolvedWorkDirectory -Recurse -Force
}
