param(
    [string]$OutputPath = "artifacts\production-readiness\local-execution-summary.md",
    [string]$ReportDirectory = "artifacts\production-readiness",
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
        "(?i)\b(keystore|service account|bucket policy|provider response)\s*[:=]\s*\S+"
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

function Get-MetadataValue {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $pattern = "(?im)^\s*#?\s*$([regex]::Escape($Name)):\s*(?<value>.+?)\s*$"
    $match = [regex]::Match($Content, $pattern)
    if ($match.Success) {
        return $match.Groups["value"].Value.Trim()
    }

    return ""
}

function Get-LocalReportRows {
    param([Parameter(Mandatory = $true)][string]$BundlePath)

    if (-not (Test-Path $BundlePath -PathType Leaf)) {
        throw "Missing readiness report bundle: $BundlePath"
    }

    $rows = @()
    $lines = Get-Content $BundlePath
    foreach ($line in $lines) {
        $match = [regex]::Match([string]$line, '^\|\s*(?<name>[^|]+?)\s*\|\s*(?<status>Ready|Blocked|Failed|Missing|Unparseable)\s*\|\s*(?<exit>-?\d+)\s*\|\s*(?<file>[^|]+?)\s*\|$')
        if (-not $match.Success) {
            continue
        }

        $rows += [pscustomobject]@{
            Name = $match.Groups["name"].Value.Trim()
            Status = $match.Groups["status"].Value.Trim()
            ExitCode = $match.Groups["exit"].Value.Trim()
            FileName = $match.Groups["file"].Value.Trim()
        }
    }

    return $rows
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedReportDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReportDirectory)
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

$bundlePath = Join-Path $resolvedReportDirectory "readiness-report-bundle.md"
$bundleContent = Get-Content $bundlePath -Raw
if (Test-ContainsSensitivePattern $bundleContent) {
    throw "Readiness report bundle appears to contain a sensitive assignment or payload. Refusing to write local execution summary."
}

$bundleBranch = Get-MetadataValue -Content $bundleContent -Name "Branch"
$bundleCommit = Get-MetadataValue -Content $bundleContent -Name "Commit"
$bundleOverall = Get-MetadataValue -Content $bundleContent -Name "Overall result"
$bundleExitCode = Get-MetadataValue -Content $bundleContent -Name "Exit code"
$currentBranch = (& git -C $repoRoot branch --show-current 2>$null)
$currentCommit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)

if (-not [string]::IsNullOrWhiteSpace($currentBranch) -and
    -not [string]::Equals($bundleBranch, $currentBranch, [StringComparison]::Ordinal)) {
    throw "Readiness report bundle branch '$bundleBranch' does not match current branch '$currentBranch'."
}

if (-not [string]::IsNullOrWhiteSpace($currentCommit) -and
    -not [string]::Equals($bundleCommit, $currentCommit, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Readiness report bundle commit '$bundleCommit' does not match current commit '$currentCommit'."
}

$rows = Get-LocalReportRows -BundlePath $bundlePath
$readyCount = @($rows | Where-Object { $_.Status -eq "Ready" }).Count
$blockedCount = @($rows | Where-Object { $_.Status -eq "Blocked" }).Count
$failedCount = @($rows | Where-Object { $_.Status -eq "Failed" }).Count
$missingCount = @($rows | Where-Object { $_.Status -eq "Missing" -or $_.Status -eq "Unparseable" }).Count

$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Production Readiness Local Execution Summary")
$lines.Add("")
$lines.Add("Prepared at UTC: $preparedAtUtc")
$lines.Add("")
$lines.Add("Branch: $bundleBranch")
$lines.Add("")
$lines.Add("Commit: $bundleCommit")
$lines.Add("")
$lines.Add("Overall result: $bundleOverall")
$lines.Add("")
$lines.Add("Exit code: $bundleExitCode")
$lines.Add("")
$lines.Add("Scope: local non-secret readiness report execution summary only. This file is ignored by git and does not contain provider credentials, private payloads, private artifacts, customer data, bank identifiers, payroll content, generated invoice payloads, mobile signing material, or approval documents.")
$lines.Add("")
$lines.Add("## Local Report Outcome")
$lines.Add("")
$lines.Add("| Result | Count |")
$lines.Add("| --- | ---: |")
$lines.Add("| Ready | $readyCount |")
$lines.Add("| Blocked | $blockedCount |")
$lines.Add("| Failed | $failedCount |")
$lines.Add("| Missing or unparseable | $missingCount |")
$lines.Add("")
$lines.Add("## Report Evidence")
$lines.Add("")
$lines.Add("| Evidence area | Result | Exit code | Report |")
$lines.Add("| --- | --- | ---: | --- |")
foreach ($row in $rows) {
    $lines.Add("| $(Escape-MarkdownCell $row.Name) | $(Escape-MarkdownCell $row.Status) | $($row.ExitCode) | $(Escape-MarkdownCell $row.FileName) |")
}
$lines.Add("")
$lines.Add("## Generated Follow-Up Artifacts")
$lines.Add("")
$lines.Add("| Artifact | Use |")
$lines.Add("| --- | --- |")
$lines.Add("| production-readiness-action-plan.md | Owner action rows and missing evidence keys derived from local reports. |")
$lines.Add("| production-readiness-owner-handoff.md | Owner-grouped follow-up for deployment evidence collection. |")
$lines.Add("| production-readiness-env-template.ps1 | Non-secret placeholder template for missing evidence variables. |")
$lines.Add("| evidence-package-local-draft.md | Ignored evidence-package draft that keeps deployment-specific evidence blocked. |")
$lines.Add("")
$lines.Add("## Remaining Evidence Boundary")
$lines.Add("")
$lines.Add("Ready local report rows are supporting evidence only. They do not replace production-like staging proof, provider evidence, storage evidence, e-invoice acceptance evidence, Android signing and smoke evidence, monitoring evidence, rollback proof, legal or accounting approval, or final owner sign-off.")

$summary = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $summary) {
    throw "Generated local execution summary appears to contain a sensitive assignment or payload. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $summary -Encoding UTF8

Write-Host "Production readiness local execution summary created:"
Write-Host $resolvedOutputPath
