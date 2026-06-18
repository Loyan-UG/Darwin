param(
    [string]$OutputPath = "artifacts\production-readiness\einvoice-production-readiness-report.md",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Test-ContainsSensitivePattern {
    param([Parameter(Mandatory = $true)][string]$Value)

    $blockedPatterns = @(
        "(?i)-----BEGIN (RSA |EC |OPENSSH |DSA |)PRIVATE KEY-----",
        "(?i)\b(password|secret|token|access[_ -]?key|connection[_ -]?string|webhook[_ -]?secret)\s*[:=]\s*\S+",
        "(?i)\b(api[_ -]?key|client[_ -]?secret|refresh[_ -]?token|private[_ -]?key)\s*[:=]\s*\S+",
        "(?i)\b(customer invoice|invoice payload|raw xml|raw pdf|generated pdf|generated xml)\s*[:=]\s*\S+",
        "(?i)\b(raw provider payload|provider payload|raw payload)\s*[:=]\s*\{",
        "(?i)\b(validator credential|legal document|private approval)\s*[:=]\s*\S+",
        "(?i)\bkeystore\s*[:=]\s*\S+"
    )

    foreach ($pattern in $blockedPatterns) {
        if ($Value -match $pattern) {
            return $true
        }
    }

    return $false
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

Push-Location $repoRoot
try {
    $rawOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-einvoice-production-readiness.ps1" 2>&1
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$outputText = ($rawOutput | ForEach-Object { $_.ToString() }) -join "`n"
if (Test-ContainsSensitivePattern $outputText) {
    throw "E-invoice readiness output appears to contain a secret assignment, private key, generated artifact payload, validator credential, legal document, private approval, keystore value, or pasted raw payload. Refusing to write report."
}

$status = switch ($exitCode) {
    0 { "Ready" }
    2 { "Blocked" }
    default { "Failed" }
}

$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# E-Invoice Production Readiness Report")
$lines.Add("")
$lines.Add("Prepared at UTC: $preparedAtUtc")
$lines.Add("")
$lines.Add("Branch: $branch")
$lines.Add("")
$lines.Add("Commit: $commit")
$lines.Add("")
$lines.Add("Overall result: $status")
$lines.Add("")
$lines.Add("Exit code: $exitCode")
$lines.Add("")
$lines.Add("This report is a non-secret readiness summary generated from `scripts\check-einvoice-production-readiness.ps1`. It does not store customer invoices, generated PDF/XML artifacts, validator credentials, provider payloads, legal documents, private approval records, customer data, or archive payloads.")
$lines.Add("")
$lines.Add("## Full Non-Secret Output")
$lines.Add("")
$lines.Add('```text')
$lines.Add($outputText)
$lines.Add('```')
$lines.Add("")
$lines.Add("Use this report as a non-secret attachment reference for e-invoice readiness evidence. A `Blocked` result is expected until the accounting or tax owner provides non-secret references to real ZUGFeRD/Factur-X and XRechnung fixture, generated artifact, validation-report, storage/download-smoke, and sign-off evidence.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated e-invoice readiness report appears to contain a secret assignment, private key, generated artifact payload, validator credential, legal document, private approval, keystore value, or pasted raw payload. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "E-invoice production readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
