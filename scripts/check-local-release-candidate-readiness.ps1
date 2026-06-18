param()

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

function Invoke-ReadinessCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Command
    )

    $rawOutput = & $Command[0] $Command[1..($Command.Count - 1)] 2>&1
    $exitCode = $LASTEXITCODE
    $outputLines = @($rawOutput | ForEach-Object { $_.ToString() })
    $outputText = $outputLines -join "`n"

    if (Test-ContainsSensitivePattern $outputText) {
        throw "$Name output appears to contain a sensitive assignment or payload."
    }

    $tail = @($outputLines | Select-Object -Last 12)
    [pscustomobject]@{
        Name = $Name
        ExitCode = $exitCode
        Tail = ($tail -join "`n").Trim()
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$checks = @(
    @{
        Name = "WebAdmin build"
        Command = @("dotnet", "build", "src\Darwin.WebAdmin\Darwin.WebAdmin.csproj", "--no-restore")
    },
    @{
        Name = "WebApi build"
        Command = @("dotnet", "build", "src\Darwin.WebApi\Darwin.WebApi.csproj", "--no-restore")
    },
    @{
        Name = "Worker build"
        Command = @("dotnet", "build", "src\Darwin.Worker\Darwin.Worker.csproj", "--no-restore")
    },
    @{
        Name = "Contracts compatibility tests"
        Command = @("dotnet", "test", "tests\Darwin.Contracts.Tests\Darwin.Contracts.Tests.csproj", "--filter", "Order|Invoice|Checkout|Commerce", "--no-restore")
    },
    @{
        Name = "Mobile shared compatibility tests"
        Command = @("dotnet", "test", "tests\Darwin.Mobile.Shared.Tests\Darwin.Mobile.Shared.Tests.csproj", "--filter", "ApiRoutes|MemberCommerceService", "--no-restore")
    }
)

Push-Location $repoRoot
try {
    $results = [System.Collections.Generic.List[object]]::new()
    foreach ($check in $checks) {
        $results.Add((Invoke-ReadinessCommand -Name $check.Name -Command $check.Command)) | Out-Null
    }
}
finally {
    Pop-Location
}

$failed = @($results | Where-Object { $_.ExitCode -ne 0 })
if ($failed.Count -gt 0) {
    Write-Host "Local release candidate readiness failed."
} else {
    Write-Host "Local release candidate readiness prerequisites are present."
}

foreach ($result in $results) {
    $status = if ($result.ExitCode -eq 0) { "Ready" } else { "Failed" }
    Write-Host "[$status] $($result.Name) (exit $($result.ExitCode))"
    if (-not [string]::IsNullOrWhiteSpace($result.Tail)) {
        Write-Host $result.Tail
    }
}

Write-Host "Web storefront npm/toolchain readiness is covered by scripts\check-web-toolchain-readiness.ps1 and the Web/Mobile readiness report."
Write-Host "No credentials, private endpoints, provider payloads, private artifacts, customer data, bank identifiers, payroll contents, generated e-invoice payloads, or private approval documents were accepted or printed."

if ($failed.Count -gt 0) {
    exit 1
}

exit 0
