param(
    [string]$ReportDirectory = "artifacts\production-readiness",
    [string]$OutputPath = "artifacts\production-readiness\production-readiness-env-template.ps1",
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
        "(?i)\b(keystore|service account|bucket policy|provider response)\s*[:=]\s*\S+",
        "(?i)\b(private artifact|approval document|generated e-invoice payload)\s*[:=]\s*\S+"
    )

    foreach ($pattern in $blockedPatterns) {
        if ($Value -match $pattern) {
            return $true
        }
    }

    return $false
}

function Get-MissingEvidenceKeys {
    param([Parameter(Mandatory = $true)][string]$Content)

    $keys = [System.Collections.Generic.List[string]]::new()
    $matches = [regex]::Matches($Content, '(?im)^\s*-\s*(DARWIN_[A-Z0-9_]+)\s*$')
    foreach ($match in $matches) {
        $key = $match.Groups[1].Value
        if (-not $keys.Contains($key)) {
            $keys.Add($key)
        }
    }

    return $keys
}

function Test-SensitiveKeyName {
    param([Parameter(Mandatory = $true)][string]$Name)

    return $Name -match '(?i)(API_KEY|API_SECRET|SECRET|TOKEN|PASSWORD|ACCESS_KEY|PRIVATE_KEY|CONNECTION_STRING|WEBHOOK_SECRET)'
}

function New-Placeholder {
    param([Parameter(Mandatory = $true)][string]$Name)

    if (Test-SensitiveKeyName -Name $Name) {
        return "<set from secure vault or current shell only>"
    }

    if ($Name.EndsWith("_CONFIRMED", [StringComparison]::OrdinalIgnoreCase)) {
        return "true"
    }

    if ($Name -match '(?i)(URL|ENDPOINT|BASE_URL)$') {
        return "<approved public-safe URL>"
    }

    if ($Name -match '(?i)(REFERENCE|LABEL|CHANNEL|PATH|BUCKET|CONTAINER|PREFIX|MODE|CODE|VERSION|ACCOUNT_NUMBER|EMAIL|PHONE|VAT_ID|NAME|STREET|POSTAL_CODE|CITY|COUNTRY)$') {
        return "<public-safe evidence value or reference>"
    }

    return "<public-safe evidence reference>"
}

$reports = @(
    @{
        Name = "Production-like staging"
        FileName = "production-like-staging-readiness-report.md"
    },
    @{
        Name = "Web and mobile"
        FileName = "web-mobile-readiness-report.md"
    },
    @{
        Name = "Go-live aggregate"
        FileName = "go-live-readiness-report.md"
    },
    @{
        Name = "MinIO production"
        FileName = "minio-production-readiness-report.md"
    },
    @{
        Name = "Azure Blob object storage"
        FileName = "azure-object-storage-readiness-report.md"
    },
    @{
        Name = "E-invoice production"
        FileName = "einvoice-production-readiness-report.md"
    },
    @{
        Name = "Android launch"
        FileName = "android-launch-readiness-report.md"
    },
    @{
        Name = "Provider readiness"
        FileName = "provider-readiness-report.md"
    }
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedReportDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReportDirectory)
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

if (-not (Test-Path $resolvedReportDirectory -PathType Container)) {
    throw "Readiness report directory was not found: $resolvedReportDirectory"
}

$itemsByReport = [System.Collections.Generic.List[object]]::new()
$seenKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($report in $reports) {
    $path = Join-Path $resolvedReportDirectory $report.FileName
    if (-not (Test-Path $path -PathType Leaf)) {
        continue
    }

    $content = Get-Content $path -Raw
    if (Test-ContainsSensitivePattern $content) {
        throw "Readiness report appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value: $($report.FileName)"
    }

    $keys = Get-MissingEvidenceKeys -Content $content
    if ($keys.Count -eq 0) {
        continue
    }

    $newKeys = [System.Collections.Generic.List[string]]::new()
    foreach ($key in ($keys | Sort-Object)) {
        if (-not $seenKeys.Contains($key)) {
            [void]$seenKeys.Add($key)
            $newKeys.Add($key)
        }
    }

    if ($newKeys.Count -eq 0) {
        continue
    }

    $itemsByReport.Add([pscustomobject]@{
        Name = $report.Name
        FileName = $report.FileName
        Keys = @($newKeys)
    })
}

$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# Generated Darwin production readiness evidence environment template.")
$lines.Add("# Prepared at UTC: $preparedAtUtc")
$lines.Add("# Branch: $branch")
$lines.Add("# Commit: $commit")
$lines.Add("#")
$lines.Add("# This file is generated under the ignored artifacts path by default.")
$lines.Add("# Keep filled copies outside git and outside public chat. Do not paste credentials, tokens, private keys, private endpoints, raw provider payloads, customer data, generated e-invoice payloads, bank identifiers, payroll contents, private artifacts, or approval documents into repository files.")
$lines.Add("# Confirmation variables use the literal value true only after the named owner has approved the underlying evidence.")
$lines.Add("# Secret-like variables are shown only so operators know which secure vault/session values are required for controlled smokes.")
$lines.Add("")

foreach ($item in $itemsByReport) {
    $lines.Add("# $($item.Name) - from $($item.FileName)")
    foreach ($key in $item.Keys) {
        if (Test-SensitiveKeyName -Name $key) {
            $lines.Add("# Secret-like setting required: $key")
            $lines.Add("# Set this from secure vault or the current process environment only. No template assignment is written.")
            continue
        }

        $placeholder = New-Placeholder -Name $key
        $escapedPlaceholder = $placeholder.Replace('"', '\"')
        $lines.Add("`$env:$key = `"$escapedPlaceholder`"")
    }

    $lines.Add("")
}

$template = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $template) {
    throw "Generated environment template appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value. Refusing to write template."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $template -Encoding UTF8

$totalKeys = 0
$sensitiveKeyCount = 0
foreach ($item in $itemsByReport) {
    foreach ($key in $item.Keys) {
        $totalKeys++
        if (Test-SensitiveKeyName -Name $key) {
            $sensitiveKeyCount++
        }
    }
}

Write-Host "Production readiness environment template created:"
Write-Host $resolvedOutputPath
Write-Host "Evidence areas: $($itemsByReport.Count)"
Write-Host "Variables: $totalKeys"
Write-Host "Secret-like variable names: $sensitiveKeyCount"
Write-Host "No secret values were accepted or written."
