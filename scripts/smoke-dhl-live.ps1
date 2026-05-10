param(
    [switch]$Execute,
    [switch]$IncludeReturn,
    [switch]$RequireRuntimePipeline
)

$ErrorActionPreference = "Stop"

function Test-Truthy {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return @("1", "true", "yes", "y") -contains $Value.Trim().ToLowerInvariant()
}

function Assert-DhlRuntimePipelineReady {
    $blocked = New-Object System.Collections.Generic.List[string]

    if (-not (Test-Truthy ([Environment]::GetEnvironmentVariable("DARWIN_DHL_SHIPMENT_PROVIDER_OPERATION_WORKER_CONFIRMED")))) {
        $blocked.Add("DARWIN_DHL_SHIPMENT_PROVIDER_OPERATION_WORKER_CONFIRMED=true is required after ShipmentProviderOperationWorker is enabled.")
    }

    if (-not (Test-Truthy ([Environment]::GetEnvironmentVariable("DARWIN_DHL_PROVIDER_CALLBACK_WORKER_CONFIRMED")))) {
        $blocked.Add("DARWIN_DHL_PROVIDER_CALLBACK_WORKER_CONFIRMED=true is required after ProviderCallbackWorker is enabled.")
    }

    if (-not (Test-Truthy ([Environment]::GetEnvironmentVariable("DARWIN_DHL_SHIPMENT_LABELS_STORAGE_CONFIRMED")))) {
        $blocked.Add("DARWIN_DHL_SHIPMENT_LABELS_STORAGE_CONFIRMED=true is required after the ShipmentLabels storage profile or shared media fallback is validated.")
    }

    if ($blocked.Count -gt 0) {
        Write-Host "DHL runtime pipeline readiness is blocked. Confirm these non-secret deployment prerequisites first:"
        foreach ($message in $blocked) {
            Write-Host " - $message"
        }

        exit 2
    }
}

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

if ($RequireRuntimePipeline) {
    Assert-DhlRuntimePipelineReady
}

if (-not $Execute) {
    Write-Host "DHL live smoke configuration is present."
    if ($RequireRuntimePipeline) {
        Write-Host "DHL runtime pipeline readiness is confirmed."
    }

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

function New-DhlShipmentPayload {
    param(
        [Parameter(Mandatory = $true)][string]$ReferencePrefix,
        [Parameter(Mandatory = $true)][hashtable]$Shipper,
        [Parameter(Mandatory = $true)][hashtable]$Consignee
    )

    return @{
        profile = "STANDARD_GRUPPENPROFIL"
        shipments = @(
            @{
                product = $productCode
                billingNumber = Get-EnvValue "DARWIN_DHL_ACCOUNT_NUMBER"
                refNo = "$ReferencePrefix-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
                shipper = $Shipper
                consignee = $Consignee
                details = @{
                    dim = @{ uom = "mm"; height = 100; length = 300; width = 200 }
                    weight = @{ uom = "kg"; value = 0.1 }
                }
            }
        )
    } | ConvertTo-Json -Depth 10
}

$shipperStreet = Split-Street (Get-EnvValue "DARWIN_DHL_SHIPPER_STREET")
$receiverStreet = Split-Street (Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_STREET")
$receiverPhone = [Environment]::GetEnvironmentVariable("DARWIN_DHL_TEST_RECEIVER_PHONE_E164")

$shipper = @{
    name1 = Get-EnvValue "DARWIN_DHL_SHIPPER_NAME"
    addressStreet = $shipperStreet.Street
    addressHouse = $shipperStreet.House
    postalCode = Get-EnvValue "DARWIN_DHL_SHIPPER_POSTAL_CODE"
    city = Get-EnvValue "DARWIN_DHL_SHIPPER_CITY"
    country = (Get-EnvValue "DARWIN_DHL_SHIPPER_COUNTRY").ToUpperInvariant()
    email = Get-EnvValue "DARWIN_DHL_SHIPPER_EMAIL"
    phone = Get-EnvValue "DARWIN_DHL_SHIPPER_PHONE_E164"
}

$receiver = @{
    name1 = Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_NAME"
    addressStreet = $receiverStreet.Street
    addressHouse = $receiverStreet.House
    postalCode = Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_POSTAL_CODE"
    city = Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_CITY"
    country = (Get-EnvValue "DARWIN_DHL_TEST_RECEIVER_COUNTRY").ToUpperInvariant()
    phone = $receiverPhone
}

$payload = New-DhlShipmentPayload -ReferencePrefix "DARWIN-LIVE-SMOKE" -Shipper $shipper -Consignee $receiver
$returnPayload = New-DhlShipmentPayload -ReferencePrefix "DARWIN-RETURN-SMOKE" -Shipper $receiver -Consignee $shipper

$headers = @{
    "Accept" = "application/json"
    "dhl-api-key" = Get-EnvValue "DARWIN_DHL_API_KEY"
    "Authorization" = "Bearer $(Get-EnvValue 'DARWIN_DHL_API_SECRET')"
}

function Invoke-DhlValidationRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Body
    )

    Write-Host "Sending DHL $Label validation request. No secrets or response payloads will be printed."
    try {
        Invoke-RestMethod -Method Post -Uri "$baseUrl/orders?validate=true" -Headers $headers -Body $Body -ContentType "application/json" | Out-Null
        Write-Host "DHL $Label validation request succeeded."
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode) {
            Write-Error "DHL $Label validation request failed with HTTP $statusCode."
        }
        else {
            Write-Error "DHL $Label validation request failed."
        }
    }
}

Invoke-DhlValidationRequest -Label "outbound-shipment" -Body $payload

if ($IncludeReturn) {
    Invoke-DhlValidationRequest -Label "return-shipment" -Body $returnPayload
}
