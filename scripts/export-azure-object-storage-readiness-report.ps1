param(
    [string]$OutputPath = "artifacts\production-readiness\azure-object-storage-readiness-report.md",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Test-ContainsSensitivePattern {
    param([Parameter(Mandatory = $true)][string]$Value)

    $blockedPatterns = @(
        "(?i)-----BEGIN (RSA |EC |OPENSSH |DSA |)PRIVATE KEY-----",
        "(?i)\b(password|secret|token|access[_ -]?key|connection[_ -]?string|webhook[_ -]?secret)\s*[:=]\s*\S+",
        "(?i)\b(api[_ -]?key|client[_ -]?secret|refresh[_ -]?token|private[_ -]?key)\s*[:=]\s*\S+",
        "(?i)\b(SAS[_ -]?token|account[_ -]?key)\s*[:=]\s*\S+",
        "(?i)\b(raw provider payload|provider payload|raw payload)\s*[:=]\s*\{",
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
    $rawOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-azure-object-storage-readiness.ps1" 2>&1
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$outputText = ($rawOutput | ForEach-Object { $_.ToString() }) -join "`n"
if (Test-ContainsSensitivePattern $outputText) {
    throw "Azure readiness output appears to contain a secret assignment, private key, keystore value, or pasted raw payload. Refusing to write report."
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
$lines.Add("# Azure Blob Object Storage Readiness Report")
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
$lines.Add("This report is a non-secret readiness summary generated from `scripts\check-azure-object-storage-readiness.ps1`. It does not store connection strings, account keys, SAS tokens, client secrets, private endpoint details, object keys, provider payloads, policy JSON, customer data, or private approval documents.")
$lines.Add("")
$lines.Add("## Full Non-Secret Output")
$lines.Add("")
$lines.Add('```text')
$lines.Add($outputText)
$lines.Add('```')
$lines.Add("")
$lines.Add("Use this report as a non-secret attachment reference only when Azure Blob is selected for a deployment or readiness lane. A `Blocked` result is expected until the deployment owner provides real Azure policy, identity, monitoring, profile, smoke-prefix, and runbook evidence.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated Azure readiness report appears to contain a secret assignment, private key, keystore value, or pasted raw payload. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Azure Blob readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
