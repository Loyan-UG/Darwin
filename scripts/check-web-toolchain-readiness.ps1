param()

$ErrorActionPreference = "Stop"

function Find-CommandPath {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        return ""
    }

    return $command.Source
}

function Test-SemVer {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][int]$MinimumMajor
    )

    $match = [regex]::Match($Value.Trim(), '^v?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)')
    if (-not $match.Success) {
        return $false
    }

    return [int]$match.Groups["major"].Value -ge $MinimumMajor
}

$missing = New-Object System.Collections.Generic.List[string]

$webRoot = "src\Darwin.Web"
if (-not (Test-Path (Join-Path $webRoot "package.json") -PathType Leaf)) {
    $missing.Add("src\Darwin.Web\package.json")
}

$nodePath = Find-CommandPath "node"
if ([string]::IsNullOrWhiteSpace($nodePath)) {
    $missing.Add("node on PATH")
}

$npmPath = Find-CommandPath "npm"
if ([string]::IsNullOrWhiteSpace($npmPath)) {
    $missing.Add("npm on PATH")
}

if ($missing.Count -gt 0) {
    Write-Host "Web toolchain readiness is blocked. Configure these prerequisites first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print npm tokens, registry credentials, environment files, private package artifacts, customer data, or provider payloads."
    exit 2
}

$nodeVersion = (& node --version 2>&1 | Select-Object -First 1).ToString().Trim()
$npmVersion = (& npm --version 2>&1 | Select-Object -First 1).ToString().Trim()

if (-not (Test-SemVer -Value $nodeVersion -MinimumMajor 22)) {
    Write-Host "Web toolchain readiness is blocked."
    Write-Host "Node.js major version must be 22 or newer for the current Next.js storefront toolchain."
    exit 2
}

if (-not (Test-SemVer -Value $npmVersion -MinimumMajor 10)) {
    Write-Host "Web toolchain readiness is blocked."
    Write-Host "npm major version must be 10 or newer for repeatable storefront install/build commands."
    exit 2
}

Write-Host "Web toolchain readiness prerequisites are present."
Write-Host "No npm token, registry credential, environment file, private package artifact, customer data, or provider payload was accepted or printed."
