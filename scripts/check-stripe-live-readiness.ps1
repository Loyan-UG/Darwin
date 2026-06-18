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

function Assert-AbsoluteHttpsEndpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$RequiredPath
    )

    $parsed = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed)) {
        Write-Host "Stripe live readiness is blocked."
        Write-Host "$Name must be an absolute URL."
        exit 2
    }

    if ($parsed.Scheme -ne "https") {
        Write-Host "Stripe live readiness is blocked."
        Write-Host "$Name must use HTTPS."
        exit 2
    }

    if ($parsed.Host -in @("localhost", "127.0.0.1", "::1")) {
        Write-Host "Stripe live readiness is blocked."
        Write-Host "$Name must be reachable by Stripe, not a loopback URL."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace($parsed.UserInfo) -or
        -not [string]::IsNullOrWhiteSpace($parsed.Query) -or
        -not [string]::IsNullOrWhiteSpace($parsed.Fragment)) {
        Write-Host "Stripe live readiness is blocked."
        Write-Host "$Name must be the public HTTPS webhook URL without embedded credentials, query strings, or fragments. Keep webhook signing secrets, tokens, and provider payloads out of readiness input."
        exit 2
    }

    if (-not $parsed.AbsolutePath.EndsWith($RequiredPath, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Stripe live readiness is blocked."
        Write-Host "$Name must end with $RequiredPath."
        exit 2
    }
}

$endpointPath = "/api/v1/public/billing/stripe/webhooks"
$webhookUrl = Get-EnvValue "DARWIN_STRIPE_LIVE_WEBHOOK_PUBLIC_URL"
$required = @(
    "DARWIN_STRIPE_LIVE_KEYS_CONFIGURED_CONFIRMED",
    "DARWIN_STRIPE_LIVE_WEBHOOK_ENDPOINT_CONFIRMED",
    "DARWIN_STRIPE_LIVE_WEBHOOK_EVENTS_CONFIRMED",
    "DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED",
    "DARWIN_STRIPE_WEBADMIN_VISIBILITY_CONFIRMED",
    "DARWIN_STRIPE_MONITORING_CONFIRMED",
    "DARWIN_STRIPE_ALERTING_CONFIRMED",
    "DARWIN_STRIPE_REFUND_DISPUTE_PLAYBOOK_CONFIRMED"
)

$missing = @()
if ([string]::IsNullOrWhiteSpace($webhookUrl)) {
    $missing += "DARWIN_STRIPE_LIVE_WEBHOOK_PUBLIC_URL"
}

foreach ($name in $required) {
    if (-not (Test-Truthy (Get-EnvValue $name).ToLowerInvariant())) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Stripe live readiness is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "This check does not accept Stripe secret keys, webhook signing secrets, or live payment details."
    Write-Host "It only records operator confirmations that live keys, webhook events, monitoring, alerting, worker processing, WebAdmin visibility, and refund/dispute playbooks are ready."
    exit 2
}

Assert-AbsoluteHttpsEndpoint -Name "DARWIN_STRIPE_LIVE_WEBHOOK_PUBLIC_URL" -Value $webhookUrl -RequiredPath $endpointPath

Write-Host "Stripe live readiness prerequisites are present."
Write-Host "No live Stripe API call, Checkout Session, charge, refund, dispute, or webhook secret validation was executed."
Write-Host "After explicit operator approval, repeat the documented Stripe smoke with live keys and verify webhook-finalized storefront payment, subscription, refund, dispute visibility, monitoring, and alerting."
