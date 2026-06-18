param()

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
        Write-Host "Web storefront readiness is blocked."
        Write-Host "$Name must be an absolute URL."
        exit 2
    }

    if (-not ($uri.Scheme -in @("http", "https"))) {
        Write-Host "Web storefront readiness is blocked."
        Write-Host "$Name must use http or https."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace($uri.UserInfo)) {
        Write-Host "Web storefront readiness is blocked."
        Write-Host "$Name must not contain embedded credentials."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace($uri.Query) -or
        -not [string]::IsNullOrWhiteSpace($uri.Fragment)) {
        Write-Host "Web storefront readiness is blocked."
        Write-Host "$Name must be a base deployment URL without query strings or fragments. Keep API keys, auth tokens, route state, and provider payloads out of readiness input."
        exit 2
    }

    $isLocal = $uri.Host -in @("localhost", "127.0.0.1", "::1")
    if (-not $isLocal -and $uri.Scheme -ne "https") {
        Write-Host "Web storefront readiness is blocked."
        Write-Host "$Name must use HTTPS for non-local endpoints."
        exit 2
    }

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
        "raw payload",
        "provider payload"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host "Web storefront readiness is blocked."
            Write-Host "$Name contains sensitive wording. Use a non-secret deployment URL or evidence reference."
            exit 2
        }
    }
}

function Assert-SafeEvidenceReference {
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
        "environment file",
        "npm token",
        "registry credential",
        "raw payload",
        "provider payload",
        "customer data",
        "private package artifact",
        "build artifact",
        "private approval"
    )

    foreach ($term in $blocked) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host "Web storefront readiness is blocked."
            Write-Host "$Name contains sensitive wording. Use a non-secret build report id, smoke report id, log-review ticket, sign-off reference, or evidence-package row id."
            exit 2
        }
    }
}

$webApiBaseUrl = Get-EnvValue "DARWIN_WEBAPI_BASE_URL"
$webSiteUrl = Get-EnvValue "DARWIN_WEB_SITE_URL"
$defaultProductionApiConfirmed = Test-Truthy (Get-EnvValue "DARWIN_WEB_DEFAULT_PRODUCTION_API_CONFIRMED").ToLowerInvariant()

$requiredReferences = @(
    "DARWIN_WEB_STOREFRONT_BUILD_REFERENCE",
    "DARWIN_WEB_RUNTIME_CONFIG_SMOKE_REFERENCE",
    "DARWIN_WEB_PUBLIC_DISCOVERY_SMOKE_REFERENCE",
    "DARWIN_WEB_MEMBER_PORTAL_ROUTE_SMOKE_REFERENCE",
    "DARWIN_WEB_CHECKOUT_ROUTE_SMOKE_REFERENCE",
    "DARWIN_WEB_DEGRADED_API_LOG_REVIEW_REFERENCE",
    "DARWIN_WEB_STAGING_OWNER_SIGNOFF_REFERENCE"
)

$required = @(
    "DARWIN_WEB_STOREFRONT_BUILD_CONFIRMED",
    "DARWIN_WEB_RUNTIME_CONFIG_SMOKE_CONFIRMED",
    "DARWIN_WEB_PUBLIC_DISCOVERY_SMOKE_CONFIRMED",
    "DARWIN_WEB_MEMBER_PORTAL_ROUTE_SMOKE_CONFIRMED",
    "DARWIN_WEB_CHECKOUT_ROUTE_SMOKE_CONFIRMED",
    "DARWIN_WEB_DEGRADED_API_LOG_REVIEWED_CONFIRMED",
    "DARWIN_WEB_STAGING_OWNER_SIGNOFF_CONFIRMED"
)

$missing = @()
if ([string]::IsNullOrWhiteSpace($webApiBaseUrl)) {
    $missing += "DARWIN_WEBAPI_BASE_URL"
}

if ([string]::IsNullOrWhiteSpace($webSiteUrl)) {
    $missing += "DARWIN_WEB_SITE_URL"
}

foreach ($name in $required) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

foreach ($name in $requiredReferences) {
    if ([string]::IsNullOrWhiteSpace((Get-EnvValue $name))) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Web storefront readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept or print API keys, auth cookies, private endpoints with credentials, environment files, customer data, provider payloads, or build artifacts."
    exit 2
}

Assert-SafeUrl -Name "DARWIN_WEBAPI_BASE_URL" -Value $webApiBaseUrl
Assert-SafeUrl -Name "DARWIN_WEB_SITE_URL" -Value $webSiteUrl
foreach ($name in $requiredReferences) {
    Assert-SafeEvidenceReference -Name $name -Value (Get-EnvValue $name)
}

if ($webApiBaseUrl.TrimEnd("/") -ieq "https://api.loyan.de" -and -not $defaultProductionApiConfirmed) {
    Write-Host "Web storefront readiness is blocked."
    Write-Host "DARWIN_WEBAPI_BASE_URL points at the default production API. Confirm this explicitly with DARWIN_WEB_DEFAULT_PRODUCTION_API_CONFIRMED=true, or point staging builds at a production-like staging WebApi."
    exit 2
}

Write-Host "Web storefront readiness prerequisites are present."
Write-Host "No API key, auth cookie, embedded credential, environment file, customer data, provider payload, or build artifact was accepted or printed."
