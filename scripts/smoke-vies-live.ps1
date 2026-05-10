param(
    [switch]$Execute,
    [switch]$CheckProviderFailure
)

$ErrorActionPreference = "Stop"

$endpoint = [Environment]::GetEnvironmentVariable("DARWIN_VIES_ENDPOINT_URL")
if ([string]::IsNullOrWhiteSpace($endpoint)) {
    $endpoint = "https://ec.europa.eu/taxation_customs/vies/services/checkVatService"
}

$timeoutSecondsValue = [Environment]::GetEnvironmentVariable("DARWIN_VIES_TIMEOUT_SECONDS")
$timeoutSeconds = 15
$invalid = @()
if (-not [string]::IsNullOrWhiteSpace($timeoutSecondsValue) -and -not [int]::TryParse($timeoutSecondsValue, [ref]$timeoutSeconds)) {
    $invalid += "DARWIN_VIES_TIMEOUT_SECONDS must be an integer."
}

if ($timeoutSeconds -lt 1 -or $timeoutSeconds -gt 120) {
    $invalid += "DARWIN_VIES_TIMEOUT_SECONDS must be between 1 and 120."
}

$required = @(
    "DARWIN_VIES_VALID_VAT_ID",
    "DARWIN_VIES_INVALID_VAT_ID"
)

$missing = @()
foreach ($name in $required) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        $missing += $name
    }
}

$parsedEndpoint = $null
if (-not [Uri]::TryCreate($endpoint.Trim(), [UriKind]::Absolute, [ref]$parsedEndpoint)) {
    $invalid += "DARWIN_VIES_ENDPOINT_URL must be an absolute URL."
}
elseif ($parsedEndpoint.Scheme -ne "https" -and
    $parsedEndpoint.Host -ne "localhost" -and
    $parsedEndpoint.Host -ne "127.0.0.1" -and
    $parsedEndpoint.Host -ne "::1") {
    $invalid += "DARWIN_VIES_ENDPOINT_URL must use HTTPS for non-local endpoints."
}

if ($missing.Count -gt 0) {
    Write-Host "VIES live smoke is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "Optional: DARWIN_VIES_ENDPOINT_URL and DARWIN_VIES_TIMEOUT_SECONDS."
    exit 2
}

if ($invalid.Count -gt 0) {
    Write-Host "VIES live smoke is blocked. Fix these configuration values first:"
    foreach ($message in $invalid) {
        Write-Host " - $message"
    }

    exit 2
}

if (-not $Execute) {
    Write-Host "VIES live smoke configuration is present."
    Write-Host "Run with -Execute to call VIES for the configured valid and invalid VAT IDs."
    Write-Host "Run with -Execute -CheckProviderFailure to also verify provider-failure handling expectation."
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

function Split-VatId {
    param([Parameter(Mandatory = $true)][string]$VatId)

    $normalized = ($VatId -replace "[\s.\-_/]", "").ToUpperInvariant()
    if ($normalized.Length -lt 4) {
        throw "VAT ID must include a two-letter country code and national number."
    }

    return @{
        CountryCode = $normalized.Substring(0, 2)
        Number = $normalized.Substring(2)
    }
}

function Invoke-ViesCheck {
    param(
        [Parameter(Mandatory = $true)][string]$VatId,
        [Parameter(Mandatory = $true)][string]$EndpointUrl
    )

    $parsed = Split-VatId $VatId
    $body = @"
<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:urn="urn:ec.europa.eu:taxud:vies:services:checkVat:types">
  <soapenv:Header/>
  <soapenv:Body>
    <urn:checkVat>
      <urn:countryCode>$($parsed.CountryCode)</urn:countryCode>
      <urn:vatNumber>$($parsed.Number)</urn:vatNumber>
    </urn:checkVat>
  </soapenv:Body>
</soapenv:Envelope>
"@

    try {
        $response = Invoke-WebRequest -Method Post -Uri $EndpointUrl -Body $body -ContentType "text/xml" -Headers @{ SOAPAction = "" } -TimeoutSec $timeoutSeconds
        if ($response.StatusCode -lt 200 -or $response.StatusCode -gt 299) {
            return @{ Status = "Unknown"; Source = "vies.unavailable"; Message = "VIES returned HTTP $($response.StatusCode)." }
        }

        [xml]$xml = $response.Content
        $validNode = $xml.GetElementsByTagName("valid") | Select-Object -First 1
        if ($null -eq $validNode) {
            return @{ Status = "Unknown"; Source = "vies.unavailable"; Message = "VIES response did not include a valid result." }
        }

        if ($validNode.InnerText -eq "true") {
            return @{ Status = "Valid"; Source = "vies"; Message = "VIES confirmed the VAT ID." }
        }

        if ($validNode.InnerText -eq "false") {
            return @{ Status = "Invalid"; Source = "vies"; Message = "VIES reported the VAT ID as invalid." }
        }

        return @{ Status = "Unknown"; Source = "vies.unavailable"; Message = "VIES response contained an unrecognized valid value." }
    }
    catch {
        return @{ Status = "Unknown"; Source = "vies.unavailable"; Message = "VIES VAT validation request failed." }
    }
}

$validResult = Invoke-ViesCheck -VatId (Get-EnvValue "DARWIN_VIES_VALID_VAT_ID") -EndpointUrl $endpoint
if ($validResult.Status -ne "Valid") {
    Write-Error "Expected configured valid VAT ID to return Valid, but got $($validResult.Status) from $($validResult.Source)."
}

$invalidResult = Invoke-ViesCheck -VatId (Get-EnvValue "DARWIN_VIES_INVALID_VAT_ID") -EndpointUrl $endpoint
if ($invalidResult.Status -ne "Invalid") {
    Write-Error "Expected configured invalid VAT ID to return Invalid, but got $($invalidResult.Status) from $($invalidResult.Source)."
}

Write-Host "VIES valid VAT ID smoke returned Valid."
Write-Host "VIES invalid VAT ID smoke returned Invalid."

if ($CheckProviderFailure) {
    $failureResult = Invoke-ViesCheck -VatId (Get-EnvValue "DARWIN_VIES_VALID_VAT_ID") -EndpointUrl "http://127.0.0.1:1/unavailable-vies"
    if ($failureResult.Status -ne "Unknown") {
        Write-Error "Provider failure must map to Unknown/manual review, but got $($failureResult.Status)."
    }

    Write-Host "Provider-failure expectation checked: failed VIES calls must remain Unknown/manual review."
}
