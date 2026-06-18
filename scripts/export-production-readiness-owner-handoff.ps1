param(
    [string]$ReportDirectory = "artifacts\production-readiness",
    [string]$OutputPath = "artifacts\production-readiness\production-readiness-owner-handoff.md",
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

function Escape-MarkdownCell {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return $Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ").Trim()
}

function Get-OverallResult {
    param([Parameter(Mandatory = $true)][string]$Content)

    $match = [regex]::Match($Content, '(?im)^Overall result:\s*(?<status>Ready|Blocked|Failed)\s*$')
    if ($match.Success) {
        return $match.Groups["status"].Value
    }

    return "Unparseable"
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

function Format-MissingEvidenceKeys {
    param([System.Collections.Generic.List[string]]$Keys)

    if ($null -eq $Keys -or $Keys.Count -eq 0) {
        return "None listed by preflight"
    }

    $safeKeys = [System.Collections.Generic.List[string]]::new()
    $redactedCount = 0
    foreach ($key in $Keys) {
        if ($key -match '(?i)(API_KEY|API_SECRET|SECRET|TOKEN|PASSWORD|ACCESS_KEY|PRIVATE_KEY|CONNECTION_STRING|WEBHOOK_SECRET)') {
            $redactedCount++
            continue
        }

        $safeKeys.Add($key)
    }

    $parts = [System.Collections.Generic.List[string]]::new()
    foreach ($safeKey in $safeKeys) {
        $parts.Add($safeKey)
    }

    if ($redactedCount -gt 0) {
        $parts.Add("sensitive configuration key names redacted: $redactedCount")
    }

    return $parts -join ", "
}

$reports = @(
    @{
        Name = "Production-like staging"
        FileName = "production-like-staging-readiness-report.md"
        Owner = "Darwin technical owner with area owners"
        NextAction = "Run the production-like staging rehearsal and attach build/test, migration, rollback, backup/restore, storage, provider, e-invoice, Android, monitoring, and owner sign-off references."
    },
    @{
        Name = "Local backup package"
        FileName = "local-backup-readiness-report.md"
        Owner = "Darwin technical owner"
        NextAction = "Attach the ready local backup package report as supporting evidence; production backup policy and owner approval still remain separate evidence."
    },
    @{
        Name = "Local PostgreSQL restore"
        FileName = "local-postgres-restore-readiness-report.md"
        Owner = "Darwin technical owner"
        NextAction = "Attach the ready local restore report as supporting evidence; production restore rehearsal and monitoring owner approval still remain separate evidence."
    },
    @{
        Name = "Local release candidate"
        FileName = "local-release-candidate-readiness-report.md"
        Owner = "Darwin technical owner"
        NextAction = "Attach the ready release-candidate report; storefront build, signed mobile artifacts, and owner approvals still remain separate evidence."
    },
    @{
        Name = "Evidence package validator tooling"
        FileName = "evidence-package-validator-smoke.md"
        Owner = "Darwin technical owner"
        NextAction = "Attach the ready validator smoke report as tooling evidence; filled deployment evidence and owner approvals still remain separate evidence."
    },
    @{
        Name = "Web and mobile"
        FileName = "web-mobile-readiness-report.md"
        Owner = "Darwin technical owner with mobile/Web owner"
        NextAction = "Provide approved WebApi and Web URLs, storefront build evidence, runtime and route smokes, degraded API log review, staging sign-off, and mobile resource evidence."
    },
    @{
        Name = "Go-live aggregate"
        FileName = "go-live-readiness-report.md"
        Owner = "Darwin technical owner"
        NextAction = "Resolve every blocked dedicated readiness row, then rerun the aggregate go-live dry run."
    },
    @{
        Name = "MinIO production"
        FileName = "minio-production-readiness-report.md"
        Owner = "System administrator or DevOps owner"
        NextAction = "Configure the approved MinIO production target with TLS, least-privilege keys, retention/legal hold, backup/restore, monitoring, disposable-prefix smoke, and runbook owner evidence."
    },
    @{
        Name = "Azure Blob object storage"
        FileName = "azure-object-storage-readiness-report.md"
        Owner = "System administrator or DevOps owner"
        NextAction = "Prepare Azure Blob readiness as the next storage hardening lane without replacing the selected MinIO production evidence unless the deployment explicitly selects Azure."
    },
    @{
        Name = "E-invoice production"
        FileName = "einvoice-production-readiness-report.md"
        Owner = "Accounting/tax owner with Darwin technical owner"
        NextAction = "Provide dual ZUGFeRD/Factur-X and XRechnung fixture, artifact, validation-report, storage/download-smoke, and accounting/tax sign-off evidence."
    },
    @{
        Name = "Android launch"
        FileName = "android-launch-readiness-report.md"
        Owner = "Darwin technical owner with system administrator owner"
        NextAction = "Provide signed Android artifact, release channel, push/maps/native sign-in evidence when enabled, physical QR/device smoke, route compatibility, and launch sign-off."
    },
    @{
        Name = "Provider readiness"
        FileName = "provider-readiness-report.md"
        Owner = "Payment, shipping, communications, and VAT area owners"
        NextAction = "Run approved Stripe, DHL, Brevo, and VIES smokes for the selected deployment scope and attach non-secret callback, monitoring, and playbook references."
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

$items = [System.Collections.Generic.List[object]]::new()
foreach ($report in $reports) {
    $path = Join-Path $resolvedReportDirectory $report.FileName
    if (-not (Test-Path $path -PathType Leaf)) {
        $items.Add([pscustomobject]@{
            Name = $report.Name
            Status = "Missing"
            Owner = $report.Owner
            MissingEvidence = "Report file missing"
            NextAction = $report.NextAction
            FileName = $report.FileName
        }) | Out-Null
        continue
    }

    $content = Get-Content $path -Raw
    if (Test-ContainsSensitivePattern $content) {
        throw "Readiness report appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value: $($report.FileName)"
    }

    $missingEvidence = if ($report.Name -eq "Go-live aggregate") {
        "See the dedicated owner rows in this handoff"
    } else {
        Format-MissingEvidenceKeys -Keys (Get-MissingEvidenceKeys -Content $content)
    }

    $items.Add([pscustomobject]@{
        Name = $report.Name
        Status = Get-OverallResult -Content $content
        Owner = $report.Owner
        MissingEvidence = $missingEvidence
        NextAction = $report.NextAction
        FileName = $report.FileName
    }) | Out-Null
}

$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)
$owners = @($items | Select-Object -ExpandProperty Owner -Unique | Sort-Object)

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# Production Readiness Owner Handoff")
$lines.Add("")
$lines.Add("Prepared at UTC: $preparedAtUtc")
$lines.Add("")
$lines.Add("Branch: $branch")
$lines.Add("")
$lines.Add("Commit: $commit")
$lines.Add("")
$lines.Add("This handoff is generated from non-secret readiness reports. It does not store credentials, private endpoints, raw provider payloads, private artifacts, customer data, generated e-invoice payloads, mobile signing material, bank identifiers, payroll contents, private approval documents, or production evidence itself.")
$lines.Add("")
$lines.Add("## Owner Summary")
$lines.Add("")
$lines.Add("| Owner | Ready | Blocked | Failed or missing |")
$lines.Add("| --- | ---: | ---: | ---: |")
foreach ($owner in $owners) {
    $ownerItems = @($items | Where-Object { $_.Owner -eq $owner })
    $ready = @($ownerItems | Where-Object { $_.Status -eq "Ready" }).Count
    $blocked = @($ownerItems | Where-Object { $_.Status -eq "Blocked" }).Count
    $failed = @($ownerItems | Where-Object { $_.Status -eq "Failed" -or $_.Status -eq "Missing" -or $_.Status -eq "Unparseable" }).Count
    $lines.Add("| $(Escape-MarkdownCell $owner) | $ready | $blocked | $failed |")
}

foreach ($owner in $owners) {
    $ownerItems = @($items | Where-Object { $_.Owner -eq $owner })
    $lines.Add("")
    $lines.Add("## $owner")
    $lines.Add("")
    $lines.Add("| Evidence area | Result | Missing evidence keys | Next action | Source report |")
    $lines.Add("| --- | --- | --- | --- | --- |")
    foreach ($item in $ownerItems) {
        $lines.Add("| $(Escape-MarkdownCell $item.Name) | $(Escape-MarkdownCell $item.Status) | $(Escape-MarkdownCell $item.MissingEvidence) | $(Escape-MarkdownCell $item.NextAction) | $(Escape-MarkdownCell $item.FileName) |")
    }
}

$lines.Add("")
$lines.Add("Ready local rows are supporting evidence only. Blocked rows require the named owner to provide real deployment evidence and approval outside source control before go-live can be approved.")

$handoff = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $handoff) {
    throw "Generated production readiness owner handoff appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $handoff -Encoding UTF8

Write-Host "Production readiness owner handoff created:"
Write-Host $resolvedOutputPath
Write-Host "Owners: $($owners.Count)"
Write-Host "Rows: $($items.Count)"
