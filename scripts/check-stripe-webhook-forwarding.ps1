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

$endpointPath = "/api/v1/public/billing/stripe/webhooks"
$publicUrl = Get-EnvValue "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL"
$forwardingConfirmed = Test-Truthy (Get-EnvValue "DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED").ToLowerInvariant()
$stripeCommand = Get-Command stripe -ErrorAction SilentlyContinue

if ([string]::IsNullOrWhiteSpace($publicUrl) -and $null -eq $stripeCommand) {
    Write-Host "Stripe webhook forwarding is blocked."
    Write-Host "Configure these environment variables first:"
    Write-Host " - DARWIN_STRIPE_WEBHOOK_PUBLIC_URL, or install Stripe CLI"
    Write-Host " - DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED"
    Write-Host "Configure either DARWIN_STRIPE_WEBHOOK_PUBLIC_URL with a public HTTPS endpoint, or install Stripe CLI for local forwarding."
    Write-Host "The endpoint path must be $endpointPath."
    Write-Host "No webhook signing secret is accepted or printed by this check."
    exit 2
}

if (-not [string]::IsNullOrWhiteSpace($publicUrl)) {
    if (-not [Uri]::TryCreate($publicUrl, [UriKind]::Absolute, [ref]$null)) {
        Write-Host "Stripe webhook forwarding is blocked."
        Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must be an absolute URL."
        exit 2
    }

    $uri = [Uri]$publicUrl
    if ($uri.Scheme -ne "https") {
        Write-Host "Stripe webhook forwarding is blocked."
        Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must use HTTPS."
        exit 2
    }

    if ($uri.Host -in @("localhost", "127.0.0.1", "::1")) {
        Write-Host "Stripe webhook forwarding is blocked."
        Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must be reachable by Stripe, not a loopback URL."
        exit 2
    }

    if (-not $uri.AbsolutePath.EndsWith($endpointPath, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Stripe webhook forwarding is blocked."
        Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must end with $endpointPath."
        exit 2
    }
}

if (-not $forwardingConfirmed) {
    Write-Host "Stripe webhook forwarding is blocked."
    Write-Host "Configure these environment variables first:"
    Write-Host " - DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED"
    Write-Host "Set DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED=true after the Stripe Dashboard endpoint or Stripe CLI forwarding is configured for $endpointPath."
    Write-Host "No webhook signing secret is accepted or printed by this check."
    exit 2
}

Write-Host "Stripe webhook forwarding prerequisites are present."
Write-Host "Use scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder -CheckReturnRoute -OpenCheckout -WaitForWebhookFinalization after starting the forwarder."
