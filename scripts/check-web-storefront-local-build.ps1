param()

$ErrorActionPreference = "Stop"

function Find-CommandPath {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $candidatePaths = @()
    if ($Name -in @("node", "node.exe")) {
        $candidatePaths += Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Links\node.exe"
        $candidatePaths += Get-ChildItem (Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages") -Recurse -Filter "node.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -ExpandProperty FullName
    }
    elseif ($Name -in @("npm", "npm.cmd")) {
        $candidatePaths += Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Links\npm.cmd"
        $candidatePaths += Get-ChildItem (Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages") -Recurse -Filter "npm.cmd" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -ExpandProperty FullName
    }

    foreach ($candidatePath in $candidatePaths) {
        if (-not [string]::IsNullOrWhiteSpace($candidatePath) -and (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
            return $candidatePath
        }
    }

    return ""
}

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

$repoRoot = Split-Path -Parent $PSScriptRoot
$webRoot = Join-Path $repoRoot "src\Darwin.Web"
$missing = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath (Join-Path $webRoot "package.json") -PathType Leaf)) {
    $missing.Add("src\Darwin.Web\package.json") | Out-Null
}

if (-not (Test-Path -LiteralPath (Join-Path $webRoot "package-lock.json") -PathType Leaf)) {
    $missing.Add("src\Darwin.Web\package-lock.json") | Out-Null
}

if (-not (Test-Path -LiteralPath (Join-Path $webRoot "node_modules") -PathType Container)) {
    $missing.Add("src\Darwin.Web\node_modules prepared from package-lock.json") | Out-Null
}

$nodePath = Find-CommandPath "node"
if ([string]::IsNullOrWhiteSpace($nodePath)) {
    $missing.Add("node on PATH or WinGet package cache") | Out-Null
}

$npmPath = Find-CommandPath "npm"
if ([string]::IsNullOrWhiteSpace($npmPath)) {
    $missing.Add("npm on PATH or WinGet package cache") | Out-Null
}

if ($missing.Count -gt 0) {
    Write-Host "Web storefront local build readiness is blocked. Configure these prerequisites first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not run npm install, accept npm tokens, read environment files, print registry credentials, or store private package artifacts."
    exit 2
}

$toolDirectories = @(
    [System.IO.Path]::GetDirectoryName($nodePath),
    [System.IO.Path]::GetDirectoryName($npmPath)
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

$toolDirectories = @($toolDirectories)
[array]::Reverse($toolDirectories)
foreach ($directory in $toolDirectories) {
    if ($env:PATH.Split([System.IO.Path]::PathSeparator) -notcontains $directory) {
        $env:PATH = "$directory$([System.IO.Path]::PathSeparator)$env:PATH"
    }
}

$stdoutPath = ""
$stderrPath = ""
Push-Location $webRoot
try {
    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ("darwin-web-build-stdout-" + [Guid]::NewGuid().ToString("N") + ".txt")
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ("darwin-web-build-stderr-" + [Guid]::NewGuid().ToString("N") + ".txt")
    $process = Start-Process -FilePath "cmd.exe" `
        -ArgumentList @("/d", "/c", "`"$npmPath`" run build") `
        -NoNewWindow `
        -Wait `
        -PassThru `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath
    $exitCode = $process.ExitCode
    $rawOutput = @()
    if (Test-Path -LiteralPath $stdoutPath -PathType Leaf) {
        $rawOutput += Get-Content -LiteralPath $stdoutPath
    }

    if (Test-Path -LiteralPath $stderrPath -PathType Leaf) {
        $rawOutput += Get-Content -LiteralPath $stderrPath
    }
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($stdoutPath) -and (Test-Path -LiteralPath $stdoutPath -PathType Leaf)) {
        Remove-Item -LiteralPath $stdoutPath -Force
    }

    if (-not [string]::IsNullOrWhiteSpace($stderrPath) -and (Test-Path -LiteralPath $stderrPath -PathType Leaf)) {
        Remove-Item -LiteralPath $stderrPath -Force
    }

    Pop-Location
}

$outputLines = @($rawOutput | ForEach-Object { $_.ToString() })
$outputText = $outputLines -join "`n"
if (Test-ContainsSensitivePattern $outputText) {
    Write-Host "Web storefront local build readiness failed."
    Write-Host " - npm build output appears to contain sensitive data. Refusing to print build output."
    exit 1
}

if ($exitCode -ne 0) {
    Write-Host "Web storefront local build readiness failed."
    Write-Host "npm run build exited with code $exitCode."
    Write-Host "Build output was captured and checked for sensitive patterns, but is not printed by this readiness script."

    exit 1
}

Write-Host "Web storefront local build readiness prerequisites are present."
Write-Host "npm run build completed successfully for src\Darwin.Web."
Write-Host "Build output was captured and checked for sensitive patterns, but is not printed by this readiness script."
Write-Host "No npm token, registry credential, environment file, API key, auth cookie, private package artifact, customer data, provider payload, or device log was accepted or printed."
exit 0
