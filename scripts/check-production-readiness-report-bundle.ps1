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
    "web-mobile-readiness-report.md",
    "go-live-readiness-report.md",
    "minio-production-readiness-report.md",
    "azure-object-storage-readiness-report.md",
    "einvoice-production-readiness-report.md",
    "android-launch-readiness-report.md",
    "provider-readiness-report.md"
)

$statuses = @{}
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

    $status = Get-OverallResult -Content $content
    if ([string]::IsNullOrWhiteSpace($status)) {
        Add-Problem $problems "Readiness report is missing an Overall result line: $fileName"
        continue
    }

    if ($status -eq "Failed") {
        Add-Problem $problems "Readiness report result is Failed and must be rerun or investigated before attachment: $fileName"
    }

    $statuses[$fileName] = $status
}

$bundlePath = Join-Path $resolvedDirectory "readiness-report-bundle.md"
if (Test-Path $bundlePath -PathType Leaf) {
    $bundleContent = Get-Content $bundlePath -Raw
    foreach ($fileName in $expectedReports | Where-Object { $_ -ne "readiness-report-bundle.md" }) {
        if ($bundleContent.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Add-Problem $problems "Bundle index does not reference expected report: $fileName"
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
Write-Host "Ready: $readyCount; Blocked: $blockedCount; Failed: 0; Missing/Unknown: 0"
Write-Host "This validates local non-secret report shape only; deployment owners remain responsible for real evidence and approvals behind each reference."
