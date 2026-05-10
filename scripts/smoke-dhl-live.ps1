param(
    [switch]$Execute
)

$ErrorActionPreference = "Stop"

$required = @(
    "DARWIN_DHL_API_BASE_URL",
    "DARWIN_DHL_API_KEY",
    "DARWIN_DHL_API_SECRET",
    "DARWIN_DHL_ACCOUNT_NUMBER",
    "DARWIN_DHL_SHIPPER_NAME",
    "DARWIN_DHL_SHIPPER_STREET",
    "DARWIN_DHL_SHIPPER_POSTAL_CODE",
    "DARWIN_DHL_SHIPPER_CITY",
    "DARWIN_DHL_SHIPPER_COUNTRY",
    "DARWIN_DHL_SHIPPER_EMAIL",
    "DARWIN_DHL_SHIPPER_PHONE_E164",
    "DARWIN_DHL_TEST_RECEIVER_NAME",
    "DARWIN_DHL_TEST_RECEIVER_STREET",
    "DARWIN_DHL_TEST_RECEIVER_POSTAL_CODE",
    "DARWIN_DHL_TEST_RECEIVER_CITY",
    "DARWIN_DHL_TEST_RECEIVER_COUNTRY"
)

$missing = @()
foreach ($name in $required) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        $missing += $name
    }
}

$invalid = @()
$configuredBaseUrl = [Environment]::GetEnvironmentVariable("DARWIN_DHL_API_BASE_URL")
if (-not [string]::IsNullOrWhiteSpace($configuredBaseUrl)) {
    $parsedBaseUrl = $null
    if (-not [Uri]::TryCreate($configuredBaseUrl.Trim(), [UriKind]::Absolute, [ref]$parsedBaseUrl)) {
        $invalid += "DARWIN_DHL_API_BASE_URL must be an absolute URL."
    }
    elseif ($parsedBaseUrl.Scheme -ne "https" -and
        $parsedBaseUrl.Host -ne "localhost" -and
        $parsedBaseUrl.Host -ne "127.0.0.1" -and
        $parsedBaseUrl.Host -ne "::1") {
        $invalid += "DARWIN_DHL_API_BASE_URL must use HTTPS for non-local endpoints."
    }
}

if ($missing.Count -gt 0) {
    Write-Host "DHL live smoke is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    exit 2
}

if ($invalid.Count -gt 0) {
    Write-Host "DHL live smoke is blocked. Fix these configuration values first:"
    foreach ($message in $invalid) {
        Write-Host " - $message"
    }

    exit 2
}

if (-not $Execute) {
    Write-Host "DHL live smoke configuration is present."
    Write-Host "Run with -Execute to send a real DHL validation request. No secrets are printed."
    exit 0
}

function Get-EnvValue {
    param([Parameter(Mandatory = $true)][string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ($null -eq $value) {
        return ""
    }

    return $value.Trim()
}

function Split-Street {
    param([Parameter(Mandatory = $true)][string]$StreetLine)

    $value = $StreetLine.Trim()
    $lastSpace = $value.LastIndexOf(" ")
    if ($lastSpace -le 0 -or $lastSpace -eq ($value.Length - 1)) {
        return @{ Street = $value; House = $null }
    }

    $suffix = $value.Substring($lastSpace + 1).Trim()
    if ($suffix -notmatch "\d") {
        return @{ Street = $value; House = $null }
    }

    return @{ Street = $value.Substring(0, $lastSpace).Trim(); House = $suffix }
}

$baseUrl = (Get-EnvValue "DARWIN_DHL_API_BASE_URL").TrimEnd("/")
$productCode = Get-EnvValue "DARWIN_DHL_PRODUCT_CODE"
if ([string]::IsNullOrWhiteSpace($productCode)) {
    $productCode = "V01PAK"
}

$receiverStreet = Split-Street (Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_STREET")
$receiverPhone = [Environment]::GetEnvironmentVariable("DARWIN_DHL_TEST_RECEIVER_PHONE_E164")

$payload = @{
    profile = "STANDARD_GRUPPENPROFIL"
    shipments = @(
        @{
            product = $productCode
            billingNumber = Get-EnvValue "DARWIN_DHL_ACCOUNT_NUMBER"
            refNo = "DARWIN-LIVE-SMOKE-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
            shipper = @{
                name1 = Get-EnvValue "DARWIN_DHL_SHIPPER_NAME"
                addressStreet = Get-EnvValue "DARWIN_DHL_SHIPPER_STREET"
                postalCode = Get-EnvValue "DARWIN_DHL_SHIPPER_POSTAL_CODE"
                city = Get-EnvValue "DARWIN_DHL_SHIPPER_CITY"
                country = (Get-EnvValue "DARWIN_DHL_SHIPPER_COUNTRY").ToUpperInvariant()
                email = Get-EnvValue "DARWIN_DHL_SHIPPER_EMAIL"
                phone = Get-EnvValue "DARWIN_DHL_SHIPPER_PHONE_E164"
            }
            consignee = @{
                name1 = Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_NAME"
                addressStreet = $receiverStreet.Street
                addressHouse = $receiverStreet.House
                postalCode = Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_POSTAL_CODE"
                city = Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_CITY"
                country = (Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_COUNTRY").ToUpperInvariant()
                phone = $receiverPhone
            }
            details = @{
                dim = @{ uom = "mm"; height = 100; length = 300; width = 200 }
                weight = @{ uom = "kg"; value = 0.1 }
            }
        }
    )
} | ConvertTo-Json -Depth 10

$headers = @{
    "Accept" = "application/json"
    "dhl-api-key" = Get-EnvValue "DARWIN_DHL_API_KEY"
    "Authorization" = "Bearer $(Get-EnvValue 'DARWIN_DHL_API_SECRET')"
}

Write-Host "Sending DHL live validation request to the configured base URL. No secrets or response payloads will be printed."

try {
    Invoke-RestMethod -Method Post -Uri "$baseUrl/orders?validate=true" -Headers $headers -Body $payload -ContentType "application/json" | Out-Null
    Write-Host "DHL live validation request succeeded."
}
catch {
    $statusCode = $null
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if ($statusCode) {
        Write-Error "DHL live validation request failed with HTTP $statusCode."
    }
    else {
        Write-Error "DHL live validation request failed."
    }
}
