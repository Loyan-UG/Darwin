param(
    [string]$OutputPath = "artifacts\production-readiness\evidence-package-local-draft.md",
    [string]$ReportDirectory = "artifacts\production-readiness",
    [string]$DeploymentLabel = "local-production-like-readiness-draft",
    [string]$ReleaseReference = "",
    [string]$PreparedBy = "Darwin technical owner",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Assert-SafeText {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }

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
        "approval document"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "$Name contains sensitive wording. Use a public-safe deployment label or evidence reference."
        }
    }
}

function Set-ResultRow {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$RowName,
        [Parameter(Mandatory = $true)][string]$Reference,
        [Parameter(Mandatory = $true)][string]$Result
    )

    $lines = $Content -split "`r?`n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if (-not $lines[$i].StartsWith("| $RowName |", [StringComparison]::Ordinal)) {
            continue
        }

        $cells = $lines[$i].Split("|")
        if ($cells.Count -lt 5) {
            continue
        }

        $cells[2] = " $Reference "
        $cells[$cells.Count - 2] = " $Result "
        $lines[$i] = $cells -join "|"
    }

    return $lines -join "`r`n"
}

function Get-LocalReportRows {
    param([Parameter(Mandatory = $true)][string]$BundlePath)

    if (-not (Test-Path $BundlePath -PathType Leaf)) {
        return @()
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
            FileName = $match.Groups["file"].Value.Trim()
        }
    }

    return $rows
}

function Add-LocalSupportingEvidenceSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$BundlePath
    )

    $rows = Get-LocalReportRows -BundlePath $BundlePath
    if ($rows.Count -eq 0) {
        return $Content
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add($Content.TrimEnd())
    $lines.Add("")
    $lines.Add("## Local Supporting Evidence Snapshot")
    $lines.Add("")
    $lines.Add("These rows are copied from the local readiness report bundle for owner handoff. Ready local rows are supporting evidence only and do not replace production-like staging proof, provider evidence, deployment approvals, or the final filled evidence-package validation.")
    $lines.Add("")
    $lines.Add("| Evidence area | Local report | Local result | Package use |")
    $lines.Add("| --- | --- | --- | --- |")
    foreach ($row in $rows) {
        $packageUse = if ($row.Status -eq "Ready") {
            "Attach as local supporting evidence; deployment owner approval can still be required."
        } else {
            "Use the action plan and environment template to collect the missing deployment evidence."
        }

        $lines.Add("| $($row.Name) | $($row.FileName) | $($row.Status) | $packageUse |")
    }

    return $lines -join "`r`n"
}

if ([string]::IsNullOrWhiteSpace($ReleaseReference)) {
    $ReleaseReference = "git-$((& git rev-parse --short=12 HEAD 2>$null).Trim())"
}

Assert-SafeText -Name "DeploymentLabel" -Value $DeploymentLabel
Assert-SafeText -Name "ReleaseReference" -Value $ReleaseReference
Assert-SafeText -Name "PreparedBy" -Value $PreparedBy

$repoRoot = Split-Path -Parent $PSScriptRoot
$templatePath = Join-Path $repoRoot "docs\templates\production-readiness-evidence-package-template.md"
if (-not (Test-Path $templatePath -PathType Leaf)) {
    throw "Missing template: $templatePath"
}

$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

$bundlePath = "artifacts\production-readiness\readiness-report-bundle.md"
$actionPlanPath = "artifacts\production-readiness\production-readiness-action-plan.md"
$ownerHandoffPath = "artifacts\production-readiness\production-readiness-owner-handoff.md"
$envTemplatePath = "artifacts\production-readiness\production-readiness-env-template.ps1"
$localExecutionSummaryPath = "artifacts\production-readiness\local-execution-summary.md"
$resolvedReportDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReportDirectory)
$resolvedBundlePath = Join-Path $resolvedReportDirectory "readiness-report-bundle.md"

$content = Get-Content $templatePath -Raw
$content = $content.Replace("{{DEPLOYMENT_LABEL}}", $DeploymentLabel.Trim())
$content = $content.Replace("{{RELEASE_REFERENCE}}", $ReleaseReference.Trim())
$content = $content.Replace("{{PREPARED_BY}}", $PreparedBy.Trim())
$content = $content.Replace("{{PREPARED_AT_UTC}}", [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))

$localReference = "See $bundlePath, $actionPlanPath, $envTemplatePath, $localExecutionSummaryPath, or deployment evidence repository"
$content = $content.Replace("Pending operator entry", $localReference)
$content = $content.Replace("| Pending |", "| Blocked |")
$content = $content.Replace(" Pending |", " Blocked |")

$content = Set-ResultRow -Content $content -RowName "Readiness report bundle" -Reference $bundlePath -Result "Ready"
$content = Set-ResultRow -Content $content -RowName "Owner action plan" -Reference $actionPlanPath -Result "Ready"
$content = Set-ResultRow -Content $content -RowName "Owner handoff" -Reference $ownerHandoffPath -Result "Ready"
$content = Set-ResultRow -Content $content -RowName "Evidence environment template" -Reference $envTemplatePath -Result "Ready"
$content = Add-LocalSupportingEvidenceSnapshot -Content $content -BundlePath $resolvedBundlePath

$content += @"

## Local Draft Notice

This draft is generated for operator follow-up only. It intentionally leaves deployment-specific staging, provider, storage, e-invoice, Android, monitoring, rollback, and final approval rows blocked until real owner evidence exists. Do not use this draft as go-live approval.
"@

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $content -Encoding UTF8

Write-Host "Production readiness local evidence package draft created:"
Write-Host $resolvedOutputPath
Write-Host "This draft is expected to remain blocked until real deployment evidence and owner approvals exist."
