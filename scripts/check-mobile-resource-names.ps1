param()

$ErrorActionPreference = "Stop"

$resourceRoots = @(
    "src\Darwin.Mobile.Business\Resources",
    "src\Darwin.Mobile.Consumer\Resources"
)

$invalid = New-Object System.Collections.Generic.List[string]
$pattern = '^[a-z][a-z0-9_]*[a-z0-9]\.[a-z0-9]+$'

foreach ($root in $resourceRoots) {
    if (-not (Test-Path $root -PathType Container)) {
        continue
    }

    Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $_.FullName -match "\\(Images|Splash|AppIcon)\\" -and
            $_.Name -notmatch $pattern
        } |
        ForEach-Object {
            $relative = Resolve-Path -LiteralPath $_.FullName -Relative
            $invalid.Add($relative.TrimStart(".", "\"))
        }
}

if ($invalid.Count -gt 0) {
    Write-Host "Mobile resource naming readiness is blocked. Rename these MAUI image resources to lowercase alphanumeric or underscore names:"
    foreach ($path in $invalid) {
        Write-Host " - $path"
    }

    Write-Host "This check does not read or print signing keys, Firebase credentials, API keys, private artifacts, device logs, or provider payloads."
    exit 2
}

Write-Host "Mobile resource naming readiness prerequisites are present."
Write-Host "No signing key, Firebase credential, API key, private artifact, device log, or provider payload was accepted or printed."
