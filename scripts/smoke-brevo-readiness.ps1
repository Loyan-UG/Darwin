param(
    [switch]$Execute,
    [switch]$Sandbox
)

$ErrorActionPreference = "Stop"

$required = @(
    "DARWIN_BREVO_API_KEY",
    "DARWIN_BREVO_SENDER_EMAIL",
    "DARWIN_BREVO_TEST_RECIPIENT_EMAIL"
)

$missing = @()
foreach ($name in $required) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        $missing += $name
    }
}

$invalid = @()
$configuredBaseUrl = [Environment]::GetEnvironmentVariable("DARWIN_BREVO_BASE_URL")
if (-not [string]::IsNullOrWhiteSpace($configuredBaseUrl)) {
    $parsedBaseUrl = $null
    if (-not [Uri]::TryCreate($configuredBaseUrl.Trim(), [UriKind]::Absolute, [ref]$parsedBaseUrl)) {
        $invalid += "DARWIN_BREVO_BASE_URL must be an absolute URL."
    }
    elseif ($parsedBaseUrl.Scheme -ne "https" -and
        $parsedBaseUrl.Host -ne "localhost" -and
        $parsedBaseUrl.Host -ne "127.0.0.1" -and
        $parsedBaseUrl.Host -ne "::1") {
        $invalid += "DARWIN_BREVO_BASE_URL must use HTTPS for non-local endpoints."
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Brevo readiness smoke is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "Optional: DARWIN_BREVO_BASE_URL, DARWIN_BREVO_SENDER_NAME, DARWIN_BREVO_REPLY_TO_EMAIL."
    exit 2
}

if ($invalid.Count -gt 0) {
    Write-Host "Brevo readiness smoke is blocked. Fix these configuration values first:"
    foreach ($message in $invalid) {
        Write-Host " - $message"
    }

    exit 2
}

if (-not $Execute) {
    Write-Host "Brevo readiness smoke configuration is present."
    Write-Host "Run with -Execute -Sandbox to call Brevo in sandbox/drop mode."
    Write-Host "Run with -Execute only after a controlled inbox send is approved. No secrets are printed."
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

function Get-OptionalEnvValue {
    param([Parameter(Mandatory = $true)][string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $null
    }

    return $value.Trim()
}

$baseUrl = Get-OptionalEnvValue "DARWIN_BREVO_BASE_URL"
if ($null -eq $baseUrl) {
    $baseUrl = "https://api.brevo.com/v3/"
}

$baseUrl = $baseUrl.TrimEnd("/") + "/"
$senderName = Get-OptionalEnvValue "DARWIN_BREVO_SENDER_NAME"
if ($null -eq $senderName) {
    $senderName = "Darwin"
}

$headers = @{
    "Accept" = "application/json"
    "api-key" = Get-EnvValue "DARWIN_BREVO_API_KEY"
}

$messageHeaders = @{
    "X-Correlation-Key" = "brevo-readiness-smoke-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
}

if ($Sandbox) {
    $messageHeaders["X-Sib-Sandbox"] = "drop"
}

$payload = @{
    sender = @{
        email = Get-EnvValue "DARWIN_BREVO_SENDER_EMAIL"
        name = $senderName
    }
    to = @(
        @{
            email = Get-EnvValue "DARWIN_BREVO_TEST_RECIPIENT_EMAIL"
        }
    )
    subject = "Darwin Brevo readiness smoke"
    htmlContent = "<p>Darwin Brevo readiness smoke.</p>"
    textContent = "Darwin Brevo readiness smoke."
    tags = @("darwin", "readiness-smoke")
    headers = $messageHeaders
}

$replyTo = Get-OptionalEnvValue "DARWIN_BREVO_REPLY_TO_EMAIL"
if ($null -ne $replyTo) {
    $payload.replyTo = @{ email = $replyTo }
}

$body = $payload | ConvertTo-Json -Depth 10

if ($Sandbox) {
    Write-Host "Sending Brevo sandbox/drop readiness request. No secrets or response payloads will be printed."
}
else {
    Write-Host "Sending Brevo controlled-inbox readiness email. No secrets or response payloads will be printed."
}

try {
    Invoke-RestMethod -Method Post -Uri "$baseUrl/smtp/email" -Headers $headers -Body $body -ContentType "application/json" | Out-Null
    if ($Sandbox) {
        Write-Host "Brevo sandbox/drop readiness request succeeded."
    }
    else {
        Write-Host "Brevo controlled-inbox readiness request succeeded. Confirm delivery and webhook processing in WebAdmin."
    }
}
catch {
    $statusCode = $null
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if ($statusCode) {
        Write-Error "Brevo readiness request failed with HTTP $statusCode."
    }
    else {
        Write-Error "Brevo readiness request failed."
    }
}
