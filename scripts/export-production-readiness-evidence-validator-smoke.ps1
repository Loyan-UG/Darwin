param(
    [string]$OutputPath = "artifacts\production-readiness\evidence-package-validator-smoke.md",
    [string]$WorkDirectory = "artifacts\production-readiness\evidence-validator-smoke",
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

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$resolvedWorkDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($WorkDirectory)

if ((Test-Path $resolvedOutputPath -PathType Leaf) -and -not $Force) {
    throw "Output file already exists. Pass -Force to replace it."
}

if (-not (Test-Path $resolvedWorkDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $resolvedWorkDirectory | Out-Null
}

$commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
$branch = (& git -C $repoRoot branch --show-current 2>$null)
$packagePath = Join-Path $resolvedWorkDirectory "evidence-package-validator-smoke-package.md"

Push-Location $repoRoot
try {
    $createOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\new-production-readiness-evidence-package.ps1" `
        -OutputPath $packagePath `
        -DeploymentLabel "validator-smoke" `
        -ReleaseReference "git-smoke-$commit" `
        -PreparedBy "Darwin technical owner" `
        -Force 2>&1
    $createExitCode = $LASTEXITCODE

    $validationOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\check-production-readiness-evidence-package.ps1" -Path $packagePath 2>&1
    $validationExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$createText = ($createOutput | ForEach-Object { $_.ToString() }) -join "`n"
$validationText = ($validationOutput | ForEach-Object { $_.ToString() }) -join "`n"
$packageContent = if (Test-Path $packagePath -PathType Leaf) { Get-Content $packagePath -Raw } else { "" }

$problems = [System.Collections.Generic.List[string]]::new()
if ($createExitCode -ne 0) {
    $problems.Add("Evidence package generation failed.") | Out-Null
}

if ($validationExitCode -ne 2) {
    $problems.Add("Generated package validator smoke expected exit code 2 for an incomplete draft package, but received $validationExitCode.") | Out-Null
}

if ($packageContent.IndexOf("| Owner handoff |", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $problems.Add("Generated package is missing the owner handoff row.") | Out-Null
}

if ($validationText.IndexOf("Missing required section", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    $problems.Add("Validator reported a missing required section for the generated template package.") | Out-Null
}

if ($validationText.IndexOf("Missing required evidence marker", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    $problems.Add("Validator reported a missing required evidence marker for the generated template package.") | Out-Null
}

if ($validationText.IndexOf("appears to contain", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    $problems.Add("Validator reported sensitive content in the generated template package.") | Out-Null
}

if ((Test-ContainsSensitivePattern $createText) -or (Test-ContainsSensitivePattern $validationText)) {
    throw "Evidence validator smoke output appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value. Refusing to write report."
}

$status = if ($problems.Count -gt 0) { "Failed" } else { "Ready" }
$exitCode = if ($status -eq "Ready") { 0 } else { 1 }
$preparedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# Evidence Package Validator Smoke Report")
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
$lines.Add("This report is a non-secret smoke for the production readiness evidence package template and validator. It generates a temporary package from the template, verifies that required rows such as owner handoff are present, and confirms the validator blocks the draft only because deployment evidence is still incomplete.")
$lines.Add("")
$lines.Add("## Smoke Expectations")
$lines.Add("")
$lines.Add("| Check | Expected |")
$lines.Add("| --- | --- |")
$lines.Add("| Package generation | Exit code 0 |")
$lines.Add("| Package validation | Exit code 2 for incomplete draft evidence |")
$lines.Add("| Required sections and markers | No missing-section or missing-marker validator output |")
$lines.Add("| Owner handoff row | Present in generated package |")
$lines.Add("")
if ($problems.Count -gt 0) {
    $lines.Add("## Problems")
    $lines.Add("")
    foreach ($problem in $problems) {
        $lines.Add("- $problem")
    }
    $lines.Add("")
}
$lines.Add("## Full Non-Secret Output")
$lines.Add("")
$lines.Add("### Package generation")
$lines.Add("")
$lines.Add('```text')
$lines.Add($createText)
$lines.Add('```')
$lines.Add("")
$lines.Add("### Package validation")
$lines.Add("")
$lines.Add('```text')
$lines.Add($validationText)
$lines.Add('```')
$lines.Add("")
$lines.Add("A Ready result proves the template and validator contract is internally consistent. It does not prove deployment readiness or replace owner approvals.")

$report = $lines -join "`r`n"
if (Test-ContainsSensitivePattern $report) {
    throw "Generated evidence validator smoke report appears to contain a sensitive assignment, private key, raw payload, private endpoint, provider response, or private evidence value. Refusing to write report."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Set-Content -Path $resolvedOutputPath -Value $report -Encoding UTF8

Write-Host "Evidence package validator smoke report created:"
Write-Host $resolvedOutputPath
Write-Host "Overall result: $status"

if ($exitCode -ne 0) {
    exit 1
}
