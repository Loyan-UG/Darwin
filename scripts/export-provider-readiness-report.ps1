param(
    [string]$OutputPath = "artifacts\production-readiness\provider-readiness-report.md",
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
        "(?i)\b(stripe[_ -]?secret|dhl[_ -]?secret|brevo[_ -]?api[_ -]?key|vat[_ -]?id)\s*[:=]\s*\S+",
        "(?i)\b(label payload|tracking payload|provider response)\s*[:=]\s*\{",
        "(?i)\bkeystore\s*[:=]\s*\S+"
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

function Invoke-ProviderCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Command,
        [int]$ExpectedBlockedExitCode = 2
    )

    $output = ""
    $exitCode = 1
    try {
        $rawOutput = & $Command[0] $Command[1..($Command.Count - 1)] 2>&1
        $exitCode = $LASTEXITCODE
        $output = ($rawOutput | ForEach-Object { $_.ToString() }) -join "`n"
    }
    catch {
        $exitCode = 1
        $output = $_.Exception.Message
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        $output = "(no output)"
    }

    if (Test-ContainsSensitivePattern $output) {
        throw "$Name output appears to contain a secret assignment, provider payload, label payload, tracking payload, keystore value, VAT identifier assignment, or provider response. Refusing to write report."
    }

    $status = "Failed"
    if ($exitCode -eq 0) {
        $status = "Ready"
    }
    elseif ($exitCode -eq $ExpectedBlockedExitCode) {
        $status = "Blocked"
    }

    return [pscustomobject]@{
        Name = $Name
        Status = $status
        ExitCode = $exitCode
        Output = $output.Trim()
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

$checks = @(
    @{
        Name = "Stripe test-mode handoff prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-stripe-testmode.ps1", "-CreateSmokeOrder", "-RequireRuntimePipeline")
    },
    @{
        Name = "Stripe webhook forwarding prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-stripe-webhook-forwarding.ps1")
    },
    @{
        Name = "Stripe live readiness prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-stripe-live-readiness.ps1")
    },
    @{
        Name = "DHL live smoke prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-dhl-live.ps1", "-RequireRuntimePipeline")
    },
    @{
        Name = "Brevo readiness prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-brevo-readiness.ps1", "-UseSiteSettings", "-RequireDeliveryPipeline")
    },
    @{
        Name = "VIES live smoke prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-vies-live.ps1")
    }
)

Push-Location $repoRoot
try {
    $results = New-Object System.Collections.Generic.List[object]
    foreach ($check in $checks) {
        $results.Add((Invoke-ProviderCheck -Name $check.Name -Command $check.Command))
    }
}
finally {
    Pop-Location
}

$readyCount = @($results | Where-Object { $_.Status -eq "Ready" }).Count
$blockedCount = @($results | Where-Object { $_.Status -eq "Blocked" }).Count
$failedCount = @($results | Where-Object { $_.Status -eq "Failed" }).Count
$status = if ($failedCount -gt 0) { "Failed" } elseif ($blockedCount -gt 0) { "Blocked" } else { "Ready" }
$exitCode = if ($failedCount -gt 0) { 1 } elseif ($blockedCount -gt 0) { 2 } else { 0 }

$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Provider Readiness Report")
$lines.Add("")
$lines.Add("Prepared at UTC: $preparedAtUtc")
$lines.Add("")
$lines.Add("Branch: $branch")
$lines.Add("")
$lines.Add("Commit: $commit")
$lines.Add("")
$lines.Add("Overall result: $status")
$lines.Add("")
$lines.Add("Exit code: $exitCode")
$lines.Add("")
$lines.Add("This report is a non-secret readiness summary for Stripe, DHL, Brevo, and VIES provider evidence. It does not execute live charges, DHL validation requests, Brevo sends, VIES checks, or provider mutations. It does not store provider secrets, webhook secrets, API keys, labels, tracking payloads, raw provider responses, VAT identifiers, customer data, or private approval records.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| Result | Count |")
$lines.Add("| --- | ---: |")
$lines.Add("| Ready | $readyCount |")
$lines.Add("| Blocked | $blockedCount |")
$lines.Add("| Failed | $failedCount |")
$lines.Add("")
$lines.Add("## Check Results")
$lines.Add("")
$lines.Add("| Check | Result | Exit code |")
$lines.Add("| --- | --- | ---: |")
foreach ($result in $results) {
    $lines.Add("| $(Escape-MarkdownCell $result.Name) | $(Escape-MarkdownCell $result.Status) | $($result.ExitCode) |")
}

$lines.Add("")
$lines.Add("## Full Non-Secret Output")
foreach ($result in $results) {
    $lines.Add("")
    $lines.Add("### $($result.Name)")
    $lines.Add("")
    $lines.Add("Result: $($result.Status)")
    $lines.Add("")
    $lines.Add('```text')
    $lines.Add($result.Output)
    $lines.Add('```')
}

$lines.Add("")
$lines.Add("Use this report as a non-secret attachment reference for provider-smoke evidence. A `Blocked` result is expected until the responsible owners provide approved Stripe, DHL, Brevo, and VIES configuration, smoke execution, monitoring, alerting, callback, and operational playbook evidence.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated provider readiness report appears to contain a secret assignment, provider payload, label payload, tracking payload, keystore value, VAT identifier assignment, or provider response. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Provider readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"
Write-Host "Ready: $readyCount; Blocked: $blockedCount; Failed: $failedCount"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
