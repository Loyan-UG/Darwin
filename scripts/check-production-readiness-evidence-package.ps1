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

if ($problems.Count -gt 0) {
    Write-Host "Production readiness evidence package validation is blocked."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }

    exit 2
}

Write-Host "Production readiness evidence package validation passed."
Write-Host "No placeholders or sensitive value patterns were detected. This check validates package shape only; deployment owners remain responsible for the real evidence behind each non-secret reference."
