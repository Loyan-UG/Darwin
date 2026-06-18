param(
    [string]$Directory = "artifacts\production-readiness"
)

$ErrorActionPreference = "Stop"

function Add-Problem {
    param(
        [System.Collections.Generic.List[string]]$Problems,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $Problems.Add($Message)
}

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

function Get-OverallResult {
    param([Parameter(Mandatory = $true)][string]$Content)

    $match = [regex]::Match($Content, '(?im)^Overall result:\s*(?<status>Ready|Blocked|Failed)\s*$')
    if ($match.Success) {
        return $match.Groups["status"].Value
    }

    return ""
}

function Get-ReportExitCode {
    param([Parameter(Mandatory = $true)][string]$Content)

    $match = [regex]::Match($Content, '(?im)^Exit code:\s*(?<exit>-?\d+)\s*$')
    if ($match.Success) {
        return [int]$match.Groups["exit"].Value
    }

    return $null
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

function Get-BundleReportRows {
    param([Parameter(Mandatory = $true)][string]$Content)

    $rows = @{}
    foreach ($line in ($Content -split "`r?`n")) {
        $match = [regex]::Match([string]$line, '^\|\s*(?<name>[^|]+?)\s*\|\s*(?<status>Ready|Blocked|Failed|Missing|Unparseable)\s*\|\s*(?<exit>-?\d+)\s*\|\s*(?<file>[^|]+?)\s*\|$')
        if (-not $match.Success) {
            continue
        }

        $fileName = $match.Groups["file"].Value.Trim()
        $rows[$fileName] = [pscustomobject]@{
            Name = $match.Groups["name"].Value.Trim()
            Status = $match.Groups["status"].Value.Trim()
            ExitCode = [int]$match.Groups["exit"].Value
            FileName = $fileName
        }
    }

    return $rows
}

function Assert-CurrentBranchAndCommit {
    param(
        [AllowEmptyCollection()][System.Collections.Generic.List[string]]$Problems,
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string]$Kind
    )

    $branch = Get-MetadataValue -Content $Content -Name "Branch"
    if ([string]::IsNullOrWhiteSpace($branch)) {
        Add-Problem $Problems "$Kind is missing a Branch line and may be stale: $FileName"
    }
    elseif (-not [string]::IsNullOrWhiteSpace($expectedBranch) -and
        -not [string]::Equals($branch, $expectedBranch, [StringComparison]::Ordinal)) {
        Add-Problem $Problems "$Kind branch '$branch' does not match current branch '$expectedBranch': $FileName"
    }

    $commit = Get-MetadataValue -Content $Content -Name "Commit"
    if ([string]::IsNullOrWhiteSpace($commit)) {
        Add-Problem $Problems "$Kind is missing a Commit line and may be stale: $FileName"
    }
    elseif (-not [string]::IsNullOrWhiteSpace($expectedCommit) -and
        -not [string]::Equals($commit, $expectedCommit, [StringComparison]::OrdinalIgnoreCase)) {
        Add-Problem $Problems "$Kind commit '$commit' does not match current commit '$expectedCommit': $FileName"
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$expectedBranch = (& git -C $repoRoot branch --show-current 2>$null)
$expectedCommit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)

$resolvedDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Directory)
$problems = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path $resolvedDirectory -PathType Container)) {
    Write-Host "Production readiness report bundle validation is blocked."
    Write-Host "Report directory was not found: $resolvedDirectory"
    exit 2
}

$expectedReports = @(
    "readiness-report-bundle.md",
    "production-like-staging-readiness-report.md",
    "local-backup-readiness-report.md",
    "local-postgres-restore-readiness-report.md",
    "local-release-candidate-readiness-report.md",
    "evidence-package-validator-smoke.md",
    "web-mobile-readiness-report.md",
    "go-live-readiness-report.md",
    "minio-production-readiness-report.md",
    "azure-object-storage-readiness-report.md",
    "einvoice-production-readiness-report.md",
    "android-launch-readiness-report.md",
    "provider-readiness-report.md"
)

$expectedHelpers = @(
    "production-readiness-action-plan.md",
    "production-readiness-owner-handoff.md",
    "production-readiness-env-template.ps1",
    "local-execution-summary.md",
    "evidence-package-local-draft.md"
)

$statuses = @{}
$reportExitCodes = @{}
foreach ($fileName in $expectedReports) {
    $path = Join-Path $resolvedDirectory $fileName
    if (-not (Test-Path $path -PathType Leaf)) {
        Add-Problem $problems "Missing readiness report: $fileName"
        continue
    }

    $content = Get-Content $path -Raw
    if (Test-ContainsSensitivePattern $content) {
        Add-Problem $problems "Readiness report appears to contain a secret assignment, private key, raw payload, private endpoint, provider response, or private evidence value: $fileName"
        continue
    }

    Assert-CurrentBranchAndCommit -Problems $problems -Content $content -FileName $fileName -Kind "Readiness report"

    $status = Get-OverallResult -Content $content
    if ([string]::IsNullOrWhiteSpace($status)) {
        Add-Problem $problems "Readiness report is missing an Overall result line: $fileName"
        continue
    }

    if ($status -eq "Failed") {
        Add-Problem $problems "Readiness report result is Failed and must be rerun or investigated before attachment: $fileName"
    }

    $reportExitCode = Get-ReportExitCode -Content $content
    if ($null -eq $reportExitCode) {
        Add-Problem $problems "Readiness report is missing an Exit code line: $fileName"
    }
    elseif ($status -eq "Ready" -and $reportExitCode -ne 0) {
        Add-Problem $problems "Ready readiness report should have exit code 0 but has $reportExitCode`: $fileName"
    }
    elseif ($status -eq "Blocked" -and $reportExitCode -ne 2) {
        Add-Problem $problems "Blocked readiness report should have exit code 2 but has $reportExitCode`: $fileName"
    }
    elseif ($status -eq "Failed" -and $reportExitCode -eq 0) {
        Add-Problem $problems "Failed readiness report should not have exit code 0: $fileName"
    }

    $statuses[$fileName] = $status
    $reportExitCodes[$fileName] = $reportExitCode
}

foreach ($fileName in $expectedHelpers) {
    $path = Join-Path $resolvedDirectory $fileName
    if (-not (Test-Path $path -PathType Leaf)) {
        Add-Problem $problems "Missing readiness helper: $fileName"
        continue
    }

    $content = Get-Content $path -Raw
    if (Test-ContainsSensitivePattern $content) {
        Add-Problem $problems "Readiness helper appears to contain a secret assignment, private key, raw payload, private endpoint, provider response, or private evidence value: $fileName"
        continue
    }

    if ($fileName -in @("production-readiness-action-plan.md", "production-readiness-owner-handoff.md", "production-readiness-env-template.ps1", "local-execution-summary.md")) {
        Assert-CurrentBranchAndCommit -Problems $problems -Content $content -FileName $fileName -Kind "Readiness helper"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and
        -not [string]::IsNullOrWhiteSpace($expectedCommit) -and
        $content.IndexOf("Release reference: git-$expectedCommit", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft release reference does not match current commit '$expectedCommit': $fileName"
    }

    if ($fileName -eq "production-readiness-action-plan.md" -and $content.IndexOf("Owner Action Rows", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Readiness action plan does not contain owner action rows: $fileName"
    }

    if ($fileName -eq "production-readiness-action-plan.md" -and $content.IndexOf("| Evidence area | Result | Exit code | Owner |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Readiness action plan does not include report exit codes in owner action rows: $fileName"
    }

    if ($fileName -eq "production-readiness-env-template.ps1" -and $content.IndexOf("No template assignment is written", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Readiness helper does not document secret-like variable handling: $fileName"
    }

    if ($fileName -eq "production-readiness-env-template.ps1" -and $content.IndexOf("Go-live aggregate", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Add-Problem $problems "Readiness environment template should use dedicated evidence sections instead of the aggregate go-live report: $fileName"
    }

    if ($fileName -eq "local-execution-summary.md" -and $content.IndexOf("Production Readiness Local Execution Summary", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local execution summary does not contain the expected title: $fileName"
    }

    if ($fileName -eq "local-execution-summary.md" -and $content.IndexOf("Remaining Evidence Boundary", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local execution summary does not preserve the deployment evidence boundary: $fileName"
    }

    if ($fileName -eq "production-readiness-action-plan.md" -and $content.IndexOf("| Go-live aggregate | Blocked | 2 | Darwin technical owner | See the dedicated readiness rows |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Readiness action plan should keep the aggregate go-live row summarized: $fileName"
    }

    if ($fileName -eq "production-readiness-owner-handoff.md" -and $content.IndexOf("Production Readiness Owner Handoff", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Owner handoff helper does not contain the expected title: $fileName"
    }

    if ($fileName -eq "production-readiness-owner-handoff.md" -and $content.IndexOf("System administrator or DevOps owner", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Owner handoff helper does not contain system administration ownership rows: $fileName"
    }

    if ($fileName -eq "production-readiness-owner-handoff.md" -and $content.IndexOf("| Evidence area | Result | Exit code | Missing evidence keys |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Owner handoff helper does not include report exit codes in owner rows: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("Local Draft Notice", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local draft notice: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("| Readiness report bundle |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain readiness report bundle reference row: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("| Owner handoff |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain owner handoff reference row: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("Local Supporting Evidence Snapshot", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local supporting evidence snapshot: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("local-execution-summary.md", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local execution summary reference: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("local-backup-readiness-report.md", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local backup report reference: $fileName"
    }
}

$bundlePath = Join-Path $resolvedDirectory "readiness-report-bundle.md"
if (Test-Path $bundlePath -PathType Leaf) {
    $bundleContent = Get-Content $bundlePath -Raw
    $bundleRows = Get-BundleReportRows -Content $bundleContent
    foreach ($fileName in $expectedReports | Where-Object { $_ -ne "readiness-report-bundle.md" }) {
        if ($bundleContent.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Add-Problem $problems "Bundle index does not reference expected report: $fileName"
            continue
        }

        if (-not $bundleRows.ContainsKey($fileName)) {
            Add-Problem $problems "Bundle index does not contain a parseable report row for expected report: $fileName"
            continue
        }

        $bundleRow = $bundleRows[$fileName]
        if ($statuses.ContainsKey($fileName) -and
            -not [string]::Equals($bundleRow.Status, $statuses[$fileName], [StringComparison]::Ordinal)) {
            Add-Problem $problems "Bundle index status '$($bundleRow.Status)' does not match report status '$($statuses[$fileName])': $fileName"
        }

        if ($reportExitCodes.ContainsKey($fileName) -and $bundleRow.ExitCode -ne $reportExitCodes[$fileName]) {
            Add-Problem $problems "Bundle index exit code '$($bundleRow.ExitCode)' does not match report exit code '$($reportExitCodes[$fileName])': $fileName"
        }
    }

    foreach ($fileName in $expectedHelpers) {
        if ($bundleContent.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Add-Problem $problems "Bundle index does not reference expected helper: $fileName"
        }
    }
}

if ($problems.Count -gt 0) {
    Write-Host "Production readiness report bundle validation is blocked."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }

    exit 2
}

$readyCount = @($statuses.Values | Where-Object { $_ -eq "Ready" }).Count
$blockedCount = @($statuses.Values | Where-Object { $_ -eq "Blocked" }).Count

Write-Host "Production readiness report bundle validation passed."
Write-Host "Reports checked: $($expectedReports.Count)"
Write-Host "Helpers checked: $($expectedHelpers.Count)"
Write-Host "Ready: $readyCount; Blocked: $blockedCount; Failed: 0; Missing/Unparseable: 0"
Write-Host "This validates local non-secret report shape only; deployment owners remain responsible for real evidence and approvals behind each reference."
