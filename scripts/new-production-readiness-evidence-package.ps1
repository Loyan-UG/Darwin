param(
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [Parameter(Mandatory = $true)][string]$DeploymentLabel,
    [Parameter(Mandatory = $true)][string]$ReleaseReference,
    [Parameter(Mandatory = $true)][string]$PreparedBy,
    [switch]$Force,
    [switch]$DryRun
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
        "provider payload"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "$Name contains sensitive wording. Use a non-secret deployment label or external evidence reference."
        }
    }
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

$content = Get-Content $templatePath -Raw
$content = $content.Replace("{{DEPLOYMENT_LABEL}}", $DeploymentLabel.Trim())
$content = $content.Replace("{{RELEASE_REFERENCE}}", $ReleaseReference.Trim())
$content = $content.Replace("{{PREPARED_BY}}", $PreparedBy.Trim())
$content = $content.Replace("{{PREPARED_AT_UTC}}", [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))

if ($DryRun) {
    Write-Host "Production readiness evidence package dry run succeeded."
    Write-Host "Template: $templatePath"
    Write-Host "Output: $resolvedOutputPath"
    Write-Host "No file was written."
    exit 0
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $content -Encoding UTF8

Write-Host "Production readiness evidence package created:"
Write-Host $resolvedOutputPath
Write-Host "Review and store deployment-specific approvals, provider reports, private artifacts, and legal evidence outside source control."
