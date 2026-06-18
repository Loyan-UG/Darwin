param(
    [string]$OutputPath = "artifacts\production-readiness\local-backup-readiness-report.md",
    [string]$BackupRoot = "",
    [string]$BackupDate = "",
    [switch]$Force
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
        "(?i)\b(private artifact|approval document|generated e-invoice payload)\s*[:=]\s*\S+",
        "(?i)\b(keystore|service account|bucket policy|provider response)\s*[:=]\s*\S+"
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

$command = @(
    "powershell",
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    "scripts\check-local-backup-readiness.ps1"
)

if (-not [string]::IsNullOrWhiteSpace($BackupRoot)) {
    $command += @("-BackupRoot", $BackupRoot)
}

if (-not [string]::IsNullOrWhiteSpace($BackupDate)) {
    $command += @("-BackupDate", $BackupDate)
}

Push-Location $repoRoot
try {
    $rawOutput = & $command[0] $command[1..($command.Count - 1)] 2>&1
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$outputText = ($rawOutput | ForEach-Object { $_.ToString() }) -join "`n"
if (Test-ContainsSensitivePattern $outputText) {
    throw "Local backup readiness output appears to contain a secret assignment, private key, private endpoint, provider payload, private artifact, approval document, customer data, bank identifier, payroll content, generated e-invoice payload, keystore value, or pasted raw payload. Refusing to write report."
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
$lines.Add("# Local Backup Readiness Report")
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
$lines.Add("This report is a non-secret readiness summary generated from `scripts\check-local-backup-readiness.ps1`. It verifies the local backup manifest, PostgreSQL dump integrity, MinIO mirror presence, private local configuration group presence, and Docker container inventory without storing credentials, private operational files, provider payloads, backup file contents, customer data, bank identifiers, payroll contents, or private approval documents.")
$lines.Add("")
$lines.Add("## Full Non-Secret Output")
$lines.Add("")
$lines.Add('```text')
$lines.Add($outputText)
$lines.Add('```')
$lines.Add("")
$lines.Add("Use this report as a local attachment reference for the production-like staging backup and restore row. A `Ready` result proves only the local backup package is structurally present and internally consistent; it does not replace a real restore rehearsal, production backup policy, monitoring owner, or deployment owner approval.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated local backup readiness report appears to contain a secret assignment, private key, private endpoint, provider payload, private artifact, approval document, customer data, bank identifier, payroll content, generated e-invoice payload, keystore value, or pasted raw payload. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Local backup readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
