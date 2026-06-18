param(
    [string]$OutputPath = "artifacts\production-readiness\go-live-readiness-report.md",
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
        "(?i)\bkeystore\s*[:=]\s*\S+"
    )

    foreach ($pattern in $blockedPatterns) {
        if ($Value -match $pattern) {
            return $true
        }
    }

    return $false
}

function Escape-MarkdownCell {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return $Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ").Trim()
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

Push-Location $repoRoot
try {
    $rawOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-go-live-readiness.ps1" 2>&1
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$outputText = ($rawOutput | ForEach-Object { $_.ToString() }) -join "`n"
if (Test-ContainsSensitivePattern $outputText) {
    throw "Readiness output appears to contain a secret assignment, private key, keystore value, or pasted raw payload. Refusing to write report."
}

$status = switch ($exitCode) {
    0 { "Ready" }
    2 { "Blocked" }
    default { "Failed" }
}

$summaryRows = New-Object System.Collections.Generic.List[object]
foreach ($line in ($outputText -split "`r?`n")) {
    $match = [regex]::Match($line, '^\s*-\s*(?<name>.+?):\s*(?<status>Ready|Blocked|Failed)\s*$')
    if ($match.Success) {
        $summaryRows.Add([pscustomobject]@{
            Name = $match.Groups["name"].Value.Trim()
            Status = $match.Groups["status"].Value.Trim()
        })
    }
}

$readyCount = @($summaryRows | Where-Object { $_.Status -eq "Ready" }).Count
$blockedCount = @($summaryRows | Where-Object { $_.Status -eq "Blocked" }).Count
$failedCount = @($summaryRows | Where-Object { $_.Status -eq "Failed" }).Count
$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Darwin Go-Live Readiness Dry-Run Report")
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
$lines.Add("This report is a non-secret readiness summary generated from `scripts\check-go-live-readiness.ps1`. It does not store credentials, private endpoints, raw provider payloads, private artifacts, customer data, bank identifiers, payroll contents, generated e-invoice payloads, or private approval documents.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| Result | Count |")
$lines.Add("| --- | ---: |")
$lines.Add("| Ready | $readyCount |")
$lines.Add("| Blocked | $blockedCount |")
$lines.Add("| Failed | $failedCount |")
$lines.Add("")
$lines.Add("## Check Results")
$lines.Add("")
$lines.Add("| Check | Result |")
$lines.Add("| --- | --- |")
foreach ($row in $summaryRows) {
    $lines.Add("| $(Escape-MarkdownCell $row.Name) | $(Escape-MarkdownCell $row.Status) |")
}
$lines.Add("")
$lines.Add("## Full Non-Secret Output")
$lines.Add("")
$lines.Add('```text')
$lines.Add($outputText)
$lines.Add('```')
$lines.Add("")
$lines.Add("Use this report as a non-secret attachment reference in the production readiness evidence package. A `Blocked` result is expected until deployment owners provide real provider, storage, e-invoice, mobile, monitoring, rollback, and approval evidence.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated report appears to contain a secret assignment, private key, keystore value, or pasted raw payload. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Go-live readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"
Write-Host "Ready: $readyCount; Blocked: $blockedCount; Failed: $failedCount"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
