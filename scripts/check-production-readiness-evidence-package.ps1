param(
    [string]$Path = $env:DARWIN_PRODUCTION_READINESS_EVIDENCE_PACKAGE_PATH
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
        "(?i)\bkeystore\s*[:=]\s*\S+"
    )

    foreach ($pattern in $blockedPatterns) {
        if ($Value -match $pattern) {
            return $true
        }
    }

    return $false
}

function Get-MetadataValue {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $escaped = [regex]::Escape($Name)
    $match = [regex]::Match($Content, "(?im)^\s*#?\s*$escaped\s*:\s*(?<value>.+?)\s*$")
    if ($match.Success) {
        return $match.Groups["value"].Value.Trim()
    }

    return ""
}

function Resolve-LocalEvidencePath {
    param(
        [Parameter(Mandatory = $true)][string]$Reference,
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot
    )

    $trimmed = $Reference.Trim()
    if ([System.IO.Path]::IsPathRooted($trimmed)) {
        return $trimmed
    }

    $repoRelative = Join-Path $RepositoryRoot $trimmed
    if (Test-Path $repoRelative -PathType Leaf) {
        return $repoRelative
    }

    return Join-Path $PackageDirectory $trimmed
}

function Get-ReadyLocalEvidenceReferences {
    param([Parameter(Mandatory = $true)][string]$Content)

    $artifactPattern = '(?<path>(?:[A-Za-z]:[\\/]|\.{1,2}[\\/]|artifacts[\\/])?[^\s|,;]+(?:readiness-report-bundle\.md|production-readiness-action-plan\.md|production-readiness-owner-handoff\.md|production-readiness-env-template\.ps1|local-execution-summary\.md))'
    $references = [System.Collections.Generic.List[string]]::new()

    foreach ($line in ($Content -split "`r?`n")) {
        if (-not $line.TrimStart().StartsWith("|", [StringComparison]::Ordinal)) {
            continue
        }

        $cells = @($line.Split("|") | ForEach-Object { $_.Trim() })
        if ($cells.Count -lt 4) {
            continue
        }

        $result = $cells[$cells.Count - 2]
        if (-not [string]::Equals($result, "Ready", [StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        foreach ($cell in $cells) {
            foreach ($match in [regex]::Matches($cell, $artifactPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
                $reference = $match.Groups["path"].Value.Trim()
                if (-not $references.Contains($reference)) {
                    $references.Add($reference)
                }
            }
        }
    }

    return $references
}

function Assert-LocalEvidenceReferences {
    param(
        [System.Collections.Generic.List[string]]$Problems,
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot
    )

    $currentBranch = (& git -C $RepositoryRoot branch --show-current 2>$null).Trim()
    $currentCommit = (& git -C $RepositoryRoot rev-parse --short=12 HEAD 2>$null).Trim()

    foreach ($reference in (Get-ReadyLocalEvidenceReferences -Content $Content)) {
        $path = Resolve-LocalEvidencePath -Reference $reference -PackageDirectory $PackageDirectory -RepositoryRoot $RepositoryRoot
        $fileName = Split-Path -Leaf $path

        if (-not (Test-Path $path -PathType Leaf)) {
            Add-Problem $Problems "Ready local evidence reference was not found: $reference"
            continue
        }

        $artifactContent = Get-Content $path -Raw
        if (Test-ContainsSensitivePattern $artifactContent) {
            Add-Problem $Problems "Ready local evidence reference appears to contain a sensitive assignment, private key, raw payload, provider response, or private evidence value: $reference"
        }

        $branch = Get-MetadataValue -Content $artifactContent -Name "Branch"
        if ([string]::IsNullOrWhiteSpace($branch)) {
            Add-Problem $Problems "Ready local evidence reference is missing Branch metadata: $reference"
        }
        elseif (-not [string]::Equals($branch, $currentBranch, [StringComparison]::Ordinal)) {
            Add-Problem $Problems "Ready local evidence reference branch '$branch' does not match current branch '$currentBranch': $reference"
        }

        $commit = Get-MetadataValue -Content $artifactContent -Name "Commit"
        if ([string]::IsNullOrWhiteSpace($commit)) {
            Add-Problem $Problems "Ready local evidence reference is missing Commit metadata: $reference"
        }
        elseif (-not [string]::Equals($commit, $currentCommit, [StringComparison]::Ordinal)) {
            Add-Problem $Problems "Ready local evidence reference commit '$commit' does not match current commit '$currentCommit': $reference"
        }

        if ($fileName -in @("readiness-report-bundle.md", "production-readiness-action-plan.md", "local-execution-summary.md")) {
            $overall = Get-MetadataValue -Content $artifactContent -Name "Overall result"
            if (-not [string]::Equals($overall, "Ready", [StringComparison]::Ordinal)) {
                Add-Problem $Problems "Ready local evidence reference must have Overall result Ready: $reference"
            }

            $exitCode = Get-MetadataValue -Content $artifactContent -Name "Exit code"
            if (-not [string]::Equals($exitCode, "0", [StringComparison]::Ordinal)) {
                Add-Problem $Problems "Ready local evidence reference must have Exit code 0: $reference"
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($Path)) {
    Write-Host "Production readiness evidence package validation is blocked."
    Write-Host "Set DARWIN_PRODUCTION_READINESS_EVIDENCE_PACKAGE_PATH or pass -Path with the filled non-secret evidence package path."
    exit 2
}

$resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
if (-not (Test-Path $resolvedPath -PathType Leaf)) {
    Write-Host "Production readiness evidence package validation is blocked."
    Write-Host "Evidence package file was not found: $resolvedPath"
    exit 2
}

$content = Get-Content $resolvedPath -Raw
$problems = [System.Collections.Generic.List[string]]::new()
$repoRoot = Split-Path -Parent $PSScriptRoot
$packageDirectory = Split-Path -Parent $resolvedPath

$requiredSections = @(
    "## 1. Deployment Identity",
    "## 2. Build, Migration, And Rollback Evidence",
    "## 3. Database Readiness",
    "## 4. Object Storage And Retention",
    "## 5. E-Invoice Acceptance",
    "## 6. Provider Smokes",
    "## 7. Mobile Release Evidence",
    "## 8. WebAdmin Operational Readiness",
    "## 9. Final Sign-Off",
    "## 10. Blockers And Owner Assignments"
)

foreach ($section in $requiredSections) {
    if ($content.IndexOf($section, [StringComparison]::Ordinal) -lt 0) {
        Add-Problem $problems "Missing required section: $section"
    }
}

$requiredMarkers = @(
    "InvoiceArchive",
    "Production-like staging rehearsal",
    "ShipmentLabels",
    "MediaAssets",
    "FinanceExports",
    "FinanceExportOutbound",
    "PersonnelDocuments",
    "PayrollPayslips",
    "Readiness report bundle",
    "Owner action plan",
    "Owner handoff",
    "Evidence environment template",
    "Local execution summary",
    "MinIO production readiness preflight",
    "Azure Blob readiness preflight",
    "ZUGFeRD/Factur-X",
    "XRechnung",
    "Stripe",
    "DHL",
    "Brevo",
    "VIES",
    "Android signed release artifact",
    "Android readiness preflight",
    "Business scope approval",
    "Accounting/tax approval",
    "Operations approval",
    "System administration approval",
    "Darwin technical approval"
)

foreach ($marker in $requiredMarkers) {
    if ($content.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Add-Problem $problems "Missing required evidence marker: $marker"
    }
}

$blockedPlaceholders = @(
    "{{DEPLOYMENT_LABEL}}",
    "{{RELEASE_REFERENCE}}",
    "{{PREPARED_AT_UTC}}",
    "{{PREPARED_BY}}",
    "Pending operator entry",
    "| Pending |",
    "Pending |",
    "| Pending"
)

foreach ($placeholder in $blockedPlaceholders) {
    if ($content.IndexOf($placeholder, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Add-Problem $problems "Evidence package still contains incomplete placeholder text: $placeholder"
    }
}

$blockedResultMarkers = @(
    "| Open |",
    "| Blocked |",
    "| Failed |",
    " Open |",
    " Blocked |",
    " Failed |",
    "| Open",
    "| Blocked",
    "| Failed"
)

foreach ($marker in $blockedResultMarkers) {
    if ($content.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Add-Problem $problems "Evidence package still contains an unresolved go-live result marker: $marker"
    }
}

if (Test-ContainsSensitivePattern $content) {
    Add-Problem $problems "Evidence package appears to contain a secret assignment, private key, keystore value, or pasted raw payload. Store secrets, raw provider payloads, private documents, bank identifiers, payroll internals, and private customer data outside this package."
}

if ($content -notmatch "Result\s*\|") {
    Add-Problem $problems "Evidence package does not contain result columns for operator evidence."
}

if ($content -notmatch "Owner\s*\|") {
    Add-Problem $problems "Evidence package does not contain owner columns for accountability."
}

if ($content -notmatch "Non-secret reference") {
    Add-Problem $problems "Evidence package must use non-secret references instead of embedding private artifacts."
}

Assert-LocalEvidenceReferences -Problems $problems -Content $content -PackageDirectory $packageDirectory -RepositoryRoot $repoRoot

if ($problems.Count -gt 0) {
    Write-Host "Production readiness evidence package validation is blocked."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }

    exit 2
}

Write-Host "Production readiness evidence package validation passed."
Write-Host "No placeholders or sensitive value patterns were detected. This check validates package shape only; deployment owners remain responsible for the real evidence behind each non-secret reference."
