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
    "local-backup-readiness-report.md",
    "local-postgres-restore-readiness-report.md",
    "local-release-candidate-readiness-report.md",
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
    "evidence-package-local-draft.md"
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

    if ($fileName -eq "production-readiness-action-plan.md" -and $content.IndexOf("Owner Action Rows", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Readiness action plan does not contain owner action rows: $fileName"
    }

    if ($fileName -eq "production-readiness-env-template.ps1" -and $content.IndexOf("No template assignment is written", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Readiness helper does not document secret-like variable handling: $fileName"
    }

    if ($fileName -eq "production-readiness-owner-handoff.md" -and $content.IndexOf("Production Readiness Owner Handoff", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Owner handoff helper does not contain the expected title: $fileName"
    }

    if ($fileName -eq "production-readiness-owner-handoff.md" -and $content.IndexOf("System administrator or DevOps owner", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Owner handoff helper does not contain system administration ownership rows: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("Local Draft Notice", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local draft notice: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("| Readiness report bundle |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain readiness report bundle reference row: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("Local Supporting Evidence Snapshot", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local supporting evidence snapshot: $fileName"
    }

    if ($fileName -eq "evidence-package-local-draft.md" -and $content.IndexOf("local-backup-readiness-report.md", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Local evidence package draft does not contain the local backup report reference: $fileName"
    }
}

$bundlePath = Join-Path $resolvedDirectory "readiness-report-bundle.md"
if (Test-Path $bundlePath -PathType Leaf) {
    $bundleContent = Get-Content $bundlePath -Raw
    foreach ($fileName in $expectedReports | Where-Object { $_ -ne "readiness-report-bundle.md" }) {
        if ($bundleContent.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Add-Problem $problems "Bundle index does not reference expected report: $fileName"
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
