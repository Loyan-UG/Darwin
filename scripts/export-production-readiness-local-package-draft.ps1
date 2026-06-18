param(
    [string]$OutputPath = "artifacts\production-readiness\evidence-package-local-draft.md",
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
$envTemplatePath = "artifacts\production-readiness\production-readiness-env-template.ps1"
$localExecutionSummaryPath = "artifacts\production-readiness\local-execution-summary.md"

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
$content = Set-ResultRow -Content $content -RowName "Evidence environment template" -Reference $envTemplatePath -Result "Ready"

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
