param(
    [string]$OutputPath = "artifacts\production-readiness\web-mobile-readiness-report.md",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Test-ContainsSensitivePattern {
    param([Parameter(Mandatory = $true)][string]$Value)

    $blockedPatterns = @(
        "(?i)-----BEGIN (RSA |EC |OPENSSH |DSA |)PRIVATE KEY-----",
        "(?i)\b(password|secret|token|access[_ -]?key|connection[_ -]?string|webhook[_ -]?secret)\s*[:=]\s*\S+",
        "(?i)\b(api[_ -]?key|client[_ -]?secret|refresh[_ -]?token|private[_ -]?key)\s*[:=]\s*\S+",
        "(?i)\b(auth[_ -]?cookie|npm[_ -]?token|registry[_ -]?credential|environment[_ -]?file)\s*[:=]\s*\S+",
        "(?i)\b(raw provider payload|provider payload|raw payload)\s*[:=]\s*\{",
        "(?i)\b(private package artifact|device log|customer data)\s*[:=]\s*\S+",
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

function Invoke-ReadinessCheck {
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
        throw "$Name output appears to contain a secret assignment, environment file value, npm token, registry credential, provider payload, private package artifact, customer data, keystore value, or device log. Refusing to write report."
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
        Name = "Web toolchain readiness prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-web-toolchain-readiness.ps1")
    },
    @{
        Name = "Web storefront local build readiness"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-web-storefront-local-build.ps1")
    },
    @{
        Name = "Web storefront route smoke prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-web-storefront-routes.ps1")
    },
    @{
        Name = "Web storefront readiness prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-web-storefront-readiness.ps1")
    },
    @{
        Name = "Mobile resource naming readiness prerequisites"
        Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-mobile-resource-names.ps1")
    }
)

Push-Location $repoRoot
try {
    $results = New-Object System.Collections.Generic.List[object]
    foreach ($check in $checks) {
        $results.Add((Invoke-ReadinessCheck -Name $check.Name -Command $check.Command))
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
$lines.Add("# Web And Mobile Readiness Report")
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
$lines.Add("This report is a non-secret readiness summary for Web storefront and mobile resource prerequisites. It runs the local storefront production build only when dependencies are already present from the lockfile and checks whether route-smoke configuration is present. It does not run npm install, build mobile packages, execute external provider calls by default, store npm tokens, registry credentials, environment files, API keys, auth cookies, private package artifacts, customer data, provider payloads, or device logs.")
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
$lines.Add("Use this report as a non-secret attachment reference for production-like staging and Android-first launch evidence. A `Blocked` result is expected until the deployment owner provides approved storefront runtime, executed route smoke, degraded API log review, staging owner sign-off, and mobile resource-name evidence. A Ready local build row proves only that the checked-out storefront builds locally; a Ready route-smoke prerequisite row proves only that the smoke can be executed against the configured public routes.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated Web/Mobile readiness report appears to contain a secret assignment, environment file value, npm token, registry credential, provider payload, private package artifact, customer data, keystore value, or device log. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Web and mobile readiness report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"
Write-Host "Ready: $readyCount; Blocked: $blockedCount; Failed: $failedCount"

if ($exitCode -eq 1) {
    exit 1
}

exit 0
