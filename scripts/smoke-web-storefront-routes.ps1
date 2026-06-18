param(
    [switch]$Execute,
    [switch]$AllowProductionEndpoint,
    [string]$WebSiteUrl = $env:DARWIN_WEB_SITE_URL,
    [string]$Routes = $env:DARWIN_WEB_ROUTE_SMOKE_PATHS
)

$ErrorActionPreference = "Stop"

function Get-EnvValue {
    param([Parameter(Mandatory = $true)][string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ($null -eq $value) {
        return ""
    }

    return $value.Trim()
}

function Test-Truthy {
    param([string]$Value)

    return $Value -in @("1", "true", "yes", "y")
}

function Assert-SafeUrl {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $uri = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri)) {
        Write-Host "Web storefront route smoke is blocked."
        Write-Host "$Name must be an absolute URL."
        exit 2
    }

    if (-not ($uri.Scheme -in @("http", "https"))) {
        Write-Host "Web storefront route smoke is blocked."
        Write-Host "$Name must use http or https."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace($uri.UserInfo)) {
        Write-Host "Web storefront route smoke is blocked."
        Write-Host "$Name must not contain embedded credentials."
        exit 2
    }

    $isLocal = $uri.Host -in @("localhost", "127.0.0.1", "::1")
    if (-not $isLocal -and $uri.Scheme -ne "https") {
        Write-Host "Web storefront route smoke is blocked."
        Write-Host "$Name must use HTTPS for non-local endpoints."
        exit 2
    }
}

function Assert-SafeText {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $blocked = @(
        "secret",
        "token",
        "credential",
        "password",
        "privatekey",
        "private key",
        "connectionstring",
        "connection string",
        "accesskey",
        "access key",
        "auth cookie",
        "raw payload",
        "provider payload"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host "Web storefront route smoke is blocked."
            Write-Host "$Name contains sensitive wording. Use public route paths only."
            exit 2
        }
    }
}

function Test-LocalEndpoint {
    param([Parameter(Mandatory = $true)][Uri]$Uri)

    return $Uri.Host -in @("localhost", "127.0.0.1", "::1")
}

function Assert-RouteSmokeConfirmed {
    param([Parameter(Mandatory = $true)][Uri]$Uri)

    if ($AllowProductionEndpoint -or (Test-LocalEndpoint -Uri $Uri)) {
        return
    }

    if (Test-Truthy (Get-EnvValue "DARWIN_WEB_ROUTE_SMOKE_CONFIRMED").ToLowerInvariant()) {
        return
    }

    Write-Host "Web storefront route smoke is blocked."
    Write-Host "The selected Web storefront URL is not local. Set DARWIN_WEB_ROUTE_SMOKE_CONFIRMED=true or pass -AllowProductionEndpoint only after the owner approves public GET smoke traffic."
    Write-Host "This guard prevents accidental traffic against a production-like storefront during local validation."
    exit 2
}

if ([string]::IsNullOrWhiteSpace($WebSiteUrl)) {
    Write-Host "Web storefront route smoke is blocked. Configure these environment variables first:"
    Write-Host " - DARWIN_WEB_SITE_URL"
    Write-Host "This check does not accept or print API keys, auth cookies, private endpoints with credentials, environment files, customer data, provider payloads, or response bodies."
    exit 2
}

if ([string]::IsNullOrWhiteSpace($Routes)) {
    $Routes = "/,/catalog,/help,/cart"
}

Assert-SafeUrl -Name "DARWIN_WEB_SITE_URL" -Value $WebSiteUrl
Assert-SafeText -Name "DARWIN_WEB_ROUTE_SMOKE_PATHS" -Value $Routes

$baseUri = [Uri]$WebSiteUrl.TrimEnd("/")
$routeList = @($Routes.Split(",", [StringSplitOptions]::RemoveEmptyEntries) |
    ForEach-Object { $_.Trim() } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

if ($routeList.Count -eq 0) {
    Write-Host "Web storefront route smoke is blocked."
    Write-Host "DARWIN_WEB_ROUTE_SMOKE_PATHS must include at least one public route path."
    exit 2
}

foreach ($route in $routeList) {
    if (-not $route.StartsWith("/", [StringComparison]::Ordinal)) {
        Write-Host "Web storefront route smoke is blocked."
        Write-Host "Route paths must start with '/'."
        exit 2
    }

    if ($route.Contains("?") -or $route.Contains("#")) {
        Write-Host "Web storefront route smoke is blocked."
        Write-Host "Route paths must not include query strings or fragments."
        exit 2
    }
}

if (-not $Execute) {
    Write-Host "Web storefront route smoke configuration is present."
    Write-Host "Routes configured: $($routeList.Count)"
    Write-Host "Run with -Execute to send public GET requests to the configured storefront routes. Non-local endpoints require DARWIN_WEB_ROUTE_SMOKE_CONFIRMED=true or -AllowProductionEndpoint. No response bodies, cookies, API keys, customer data, or provider payloads are printed."
    exit 0
}

Assert-RouteSmokeConfirmed -Uri $baseUri

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($route in $routeList) {
    $target = [Uri]::new($baseUri, $route)
    try {
        $response = Invoke-WebRequest -Uri $target -Method Get -MaximumRedirection 5 -TimeoutSec 20 -UseBasicParsing
        $statusCode = [int]$response.StatusCode
        if ($statusCode -lt 200 -or $statusCode -ge 400) {
            $failures.Add("$route returned HTTP $statusCode.") | Out-Null
        } else {
            Write-Host "$route : HTTP $statusCode"
        }
    }
    catch {
        $statusCode = 0
        if ($null -ne $_.Exception.Response) {
            try {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
            catch {
                $statusCode = 0
            }
        }

        if ($statusCode -gt 0) {
            $failures.Add("$route returned HTTP $statusCode.") | Out-Null
        } else {
            $failures.Add("$route could not be reached.") | Out-Null
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Web storefront route smoke failed."
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }

    Write-Host "No response bodies, cookies, API keys, customer data, private endpoints with credentials, provider payloads, or device logs were printed."
    exit 1
}

Write-Host "Web storefront route smoke completed successfully."
Write-Host "Routes checked: $($routeList.Count)"
Write-Host "No response bodies, cookies, API keys, customer data, private endpoints with credentials, provider payloads, or device logs were printed."
exit 0
