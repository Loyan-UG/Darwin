param(
    [string]$OutputPath = "artifacts\production-readiness\local-postgres-restore-readiness-report.md",
    [string]$BackupRoot = "",
    [string]$BackupDate = "",
    [string]$PostgresContainerName = "",
    [int]$DockerCommandTimeoutSeconds = 900,
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
    "scripts\check-local-postgres-restore-readiness.ps1"
)

if (-not [string]::IsNullOrWhiteSpace($BackupRoot)) {
    $command += @("-BackupRoot", $BackupRoot)
}

if (-not [string]::IsNullOrWhiteSpace($BackupDate)) {
    $command += @("-BackupDate", $BackupDate)
}

if (-not [string]::IsNullOrWhiteSpace($PostgresContainerName)) {
    $command += @("-PostgresContainerName", $PostgresContainerName)
}

$command += @("-DockerCommandTimeoutSeconds", $DockerCommandTimeoutSeconds.ToString())

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
    throw "Local PostgreSQL restore readiness output appears to contain a secret assignment, private key, private endpoint, provider payload, private artifact, approval document, customer data, bank identifier, payroll content, generated e-invoice payload, keystore value, or pasted raw payload. Refusing to write report."
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
$lines.Add("# Local PostgreSQL Restore Readiness Report")
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
$lines.Add("This report is a non-secret readiness summary generated from `scripts\check-local-postgres-restore-readiness.ps1`. It restores the local PostgreSQL backup dump into an isolated temporary database with `pg_restore --no-owner`, checks application schema/table/migration-history counts, and cleans up the temporary database without storing credentials, private operational files, provider payloads, dump contents, restored row data, customer data, bank identifiers, payroll contents, or private approval documents.")
$lines.Add("")
$lines.Add("## Full Non-Secret Output")
$lines.Add("")
$lines.Add('```text')
$lines.Add($outputText)
$lines.Add('```')
$lines.Add("")
$lines.Add("Use this report as local restore rehearsal evidence for the production-like staging backup and restore row. A `Ready` result proves the local dump can be restored into the current local PostgreSQL container; it does not replace production backup policy, offsite restore, monitoring owner, or deployment owner approval.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated local PostgreSQL restore readiness report appears to contain a secret assignment, private key, private endpoint, provider payload, private artifact, approval document, customer data, bank identifier, payroll content, generated e-invoice payload, keystore value, or pasted raw payload. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Local PostgreSQL restore readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
