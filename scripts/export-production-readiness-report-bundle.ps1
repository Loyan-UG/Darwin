param(
    [string]$OutputDirectory = "artifacts\production-readiness",
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
        "(?i)\b(keystore|service account|bucket policy|provider response)\s*[:=]\s*\S+"
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

function Get-ReportStatus {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path $Path -PathType Leaf)) {
        return "Missing"
    }

    $content = Get-Content $Path -Raw
    if (Test-ContainsSensitivePattern $content) {
        throw "Report appears to contain a sensitive assignment or payload: $Path"
    }

    $match = [regex]::Match($content, '(?im)^Overall result:\s*(?<status>Ready|Blocked|Failed)\s*$')
    if ($match.Success) {
        return $match.Groups["status"].Value
    }

    return "Unparseable"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
if (-not (Test-Path $resolvedOutputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $resolvedOutputDirectory | Out-Null
}

$reports = @(
    @{
        Name = "Production-like staging"
        Script = "scripts\export-production-like-staging-readiness-report.ps1"
        FileName = "production-like-staging-readiness-report.md"
    },
    @{
        Name = "Local backup package"
        Script = "scripts\export-local-backup-readiness-report.ps1"
        FileName = "local-backup-readiness-report.md"
    },
    @{
        Name = "Local PostgreSQL restore"
        Script = "scripts\export-local-postgres-restore-readiness-report.ps1"
        FileName = "local-postgres-restore-readiness-report.md"
    },
    @{
        Name = "Local release candidate"
        Script = "scripts\export-local-release-candidate-readiness-report.ps1"
        FileName = "local-release-candidate-readiness-report.md"
    },
    @{
        Name = "Web and mobile"
        Script = "scripts\export-web-mobile-readiness-report.ps1"
        FileName = "web-mobile-readiness-report.md"
    },
    @{
        Name = "Go-live aggregate"
        Script = "scripts\export-go-live-readiness-report.ps1"
        FileName = "go-live-readiness-report.md"
    },
    @{
        Name = "MinIO production"
        Script = "scripts\export-minio-production-readiness-report.ps1"
        FileName = "minio-production-readiness-report.md"
    },
    @{
        Name = "Azure Blob object storage"
        Script = "scripts\export-azure-object-storage-readiness-report.ps1"
        FileName = "azure-object-storage-readiness-report.md"
    },
    @{
        Name = "E-invoice production"
        Script = "scripts\export-einvoice-production-readiness-report.ps1"
        FileName = "einvoice-production-readiness-report.md"
    },
    @{
        Name = "Android launch"
        Script = "scripts\export-android-launch-readiness-report.ps1"
        FileName = "android-launch-readiness-report.md"
    },
    @{
        Name = "Provider readiness"
        Script = "scripts\export-provider-readiness-report.ps1"
        FileName = "provider-readiness-report.md"
    }
)

$results = New-Object System.Collections.Generic.List[object]

Push-Location $repoRoot
try {
    foreach ($report in $reports) {
        $outputPath = Join-Path $resolvedOutputDirectory $report.FileName
        $command = @(
            "powershell",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            $report.Script,
            "-OutputPath",
            $outputPath
        )

        if ($Force) {
            $command += "-Force"
        }

        $rawOutput = & $command[0] $command[1..($command.Count - 1)] 2>&1
        $exitCode = $LASTEXITCODE
        $outputText = ($rawOutput | ForEach-Object { $_.ToString() }) -join "`n"
        if (Test-ContainsSensitivePattern $outputText) {
            throw "$($report.Name) exporter output appears to contain a sensitive assignment or payload. Refusing to write bundle index."
        }

        $status = Get-ReportStatus -Path $outputPath
        if ($exitCode -eq 1 -and $status -ne "Failed") {
            $status = "Failed"
        }

        $results.Add([pscustomobject]@{
            Name = $report.Name
            Status = $status
            ExitCode = $exitCode
            FileName = $report.FileName
        })
    }
}
finally {
    Pop-Location
}

$readyCount = @($results | Where-Object { $_.Status -eq "Ready" }).Count
$blockedCount = @($results | Where-Object { $_.Status -eq "Blocked" }).Count
$failedCount = @($results | Where-Object { $_.Status -eq "Failed" }).Count
$missingCount = @($results | Where-Object { $_.Status -eq "Missing" -or $_.Status -eq "Unparseable" }).Count
$overall = if ($failedCount -gt 0 -or $missingCount -gt 0) { "Failed" } elseif ($blockedCount -gt 0) { "Blocked" } else { "Ready" }
$exitCode = if ($overall -eq "Failed") { 1 } elseif ($overall -eq "Blocked") { 2 } else { 0 }

$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)
$bundlePath = Join-Path $resolvedOutputDirectory "readiness-report-bundle.md"

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Production Readiness Report Bundle")
$lines.Add("")
$lines.Add("Prepared at UTC: $preparedAtUtc")
$lines.Add("")
$lines.Add("Branch: $branch")
$lines.Add("")
$lines.Add("Commit: $commit")
$lines.Add("")
$lines.Add("Overall result: $overall")
$lines.Add("")
$lines.Add("Exit code: $exitCode")
$lines.Add("")
$lines.Add("This bundle index is a non-secret summary of generated local readiness reports. It does not store credentials, provider payloads, private artifacts, customer data, generated e-invoice payloads, mobile signing material, private approval records, or production evidence itself.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| Result | Count |")
$lines.Add("| --- | ---: |")
$lines.Add("| Ready | $readyCount |")
$lines.Add("| Blocked | $blockedCount |")
$lines.Add("| Failed | $failedCount |")
$lines.Add("| Missing or unparseable | $missingCount |")
$lines.Add("")
$lines.Add("## Reports")
$lines.Add("")
$lines.Add("| Report | Result | Exit code | File |")
$lines.Add("| --- | --- | ---: | --- |")
foreach ($result in $results) {
    $lines.Add("| $(Escape-MarkdownCell $result.Name) | $(Escape-MarkdownCell $result.Status) | $($result.ExitCode) | $(Escape-MarkdownCell $result.FileName) |")
}
$lines.Add("")
$lines.Add("## Generated Follow-Up Artifacts")
$lines.Add("")
$lines.Add("| Artifact | Purpose |")
$lines.Add("| --- | --- |")
$lines.Add("| production-readiness-action-plan.md | Owner action rows, missing evidence keys, and next actions derived from the readiness reports. |")
$lines.Add("| production-readiness-env-template.ps1 | De-duplicated local/session placeholder template for missing evidence variables. Filled copies must stay outside git. |")
$lines.Add("| evidence-package-local-draft.md | Ignored evidence-package draft with local report/helper references marked ready and deployment-specific rows left blocked. |")
$lines.Add("")
$lines.Add("Use the listed report files as non-secret attachment references in the deployment evidence package. A `Blocked` bundle is valid current-state evidence, but it is not go-live approval and does not replace real staging, storage, provider, e-invoice, mobile, monitoring, rollback, or owner approval records.")

$bundle = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $bundle) {
    throw "Generated bundle index appears to contain a sensitive assignment or payload. Refusing to write report."
}

Set-Content -Path $bundlePath -Value $bundle -Encoding UTF8

$actionPlanPath = Join-Path $resolvedOutputDirectory "production-readiness-action-plan.md"
$envTemplatePath = Join-Path $resolvedOutputDirectory "production-readiness-env-template.ps1"
$localDraftPath = Join-Path $resolvedOutputDirectory "evidence-package-local-draft.md"
Push-Location $repoRoot
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\export-production-readiness-action-plan.ps1" -ReportDirectory $resolvedOutputDirectory -OutputPath $actionPlanPath -Force | Out-Host
    if ($LASTEXITCODE -eq 1) {
        exit 1
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\export-production-readiness-env-template.ps1" -ReportDirectory $resolvedOutputDirectory -OutputPath $envTemplatePath -Force | Out-Host
    if ($LASTEXITCODE -eq 1) {
        exit 1
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\export-production-readiness-local-package-draft.ps1" -OutputPath $localDraftPath -Force | Out-Host
    if ($LASTEXITCODE -eq 1) {
        exit 1
    }
}
finally {
    Pop-Location
}

Write-Host "Production readiness report bundle created:"
Write-Host $bundlePath
Write-Host "Overall result: $overall"
Write-Host "Ready: $readyCount; Blocked: $blockedCount; Failed: $failedCount; Missing/Unparseable: $missingCount"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
