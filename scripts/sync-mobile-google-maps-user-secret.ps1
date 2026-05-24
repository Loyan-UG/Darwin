param(
    [string]$ProjectPath = "src\Darwin.Mobile.Consumer\Darwin.Mobile.Consumer.csproj"
)

$ErrorActionPreference = "Stop"

function Get-SecretValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Json,
        [Parameter(Mandatory = $true)]
        [string[]]$Names
    )

    $cleanJson = ($Json -split "`r?`n" | Where-Object {
        $_ -notmatch '^\s*//BEGIN\s*$' -and $_ -notmatch '^\s*//END\s*$'
    }) -join "`n"

    if ([string]::IsNullOrWhiteSpace($cleanJson)) {
        return $null
    }

    $values = $cleanJson | ConvertFrom-Json
    foreach ($name in $Names) {
        $property = $values.PSObject.Properties[$name]
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            return [string]$property.Value
        }
    }

    return $null
}

$raw = dotnet user-secrets list --project $ProjectPath --json
$key = Get-SecretValue -Json ($raw | Out-String) -Names @("GoogleMapsApiKey", "GOOGLE_MAPS_API_KEY", "ANDROID_GOOGLE_MAPS_API_KEY")

if ([string]::IsNullOrWhiteSpace($key) -or $key -notmatch '^A[Ii]za[0-9A-Za-z_-]{20,}$') {
    Write-Error "No valid raw Google Maps API key was found in .NET User Secrets for $ProjectPath."
}

[Environment]::SetEnvironmentVariable("GoogleMapsApiKey", $key, "User")
[Environment]::SetEnvironmentVariable("GOOGLE_MAPS_API_KEY", $key, "User")
[Environment]::SetEnvironmentVariable("ANDROID_GOOGLE_MAPS_API_KEY", $key, "User")

Write-Host "Mobile Google Maps build variables were synchronized from .NET User Secrets. Restart Visual Studio or any open terminal before building Android."
