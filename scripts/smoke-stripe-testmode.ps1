param(
    [switch]$Execute,
    [switch]$CreateSmokeOrder,
    [switch]$CheckReturnRoute,
    [switch]$CheckBusinessSubscriptionCheckout,
    [switch]$OpenCheckout,
    [switch]$WaitForWebhookFinalization,
    [switch]$RequireRuntimePipeline,
    [string]$SmokeProductSlug = "iphone-15-pro-128",
    [int]$WebhookWaitSeconds = 180,
    [int]$WebhookPollSeconds = 5
)

$ErrorActionPreference = "Stop"

$required = @("DARWIN_WEBAPI_BASE_URL")
if ($CheckBusinessSubscriptionCheckout) {
    $required += @(
        "DARWIN_BUSINESS_API_BEARER_TOKEN",
        "DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID"
    )
}

if (-not $CreateSmokeOrder -and -not $CheckBusinessSubscriptionCheckout) {
    $required += @(
        "DARWIN_STRIPE_SMOKE_ORDER_ID",
        "DARWIN_STRIPE_SMOKE_ORDER_NUMBER"
    )
}

$missing = @()
foreach ($name in $required) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Stripe test-mode smoke is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "Stripe test keys and webhook signing secret must be entered through Settings or secure configuration, not this script."
    exit 2
}

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

function Assert-StripeRuntimePipelineReady {
    if (-not (Test-Truthy (Get-EnvValue "DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED").ToLowerInvariant())) {
        Write-Host "Stripe runtime pipeline readiness is blocked."
        Write-Host "Set DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED=true after ProviderCallbackWorker is enabled for the target environment."
        Write-Host "No Stripe secrets or webhook signing secrets are accepted or printed by this check."
        exit 2
    }
}

function Assert-StripeWebhookForwardingReady {
    $endpointPath = "/api/v1/public/billing/stripe/webhooks"
    $publicUrl = Get-EnvValue "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL"
    $forwardingConfirmed = Test-Truthy (Get-EnvValue "DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED").ToLowerInvariant()
    $stripeCommand = Get-Command stripe -ErrorAction SilentlyContinue

    if ([string]::IsNullOrWhiteSpace($publicUrl) -and $null -eq $stripeCommand) {
        Write-Host "Stripe webhook finalization wait is blocked."
        Write-Host "Configure DARWIN_STRIPE_WEBHOOK_PUBLIC_URL or install Stripe CLI for local forwarding."
        Write-Host "No webhook signing secret is accepted or printed by this check."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace($publicUrl)) {
        $parsed = $null
        if (-not [Uri]::TryCreate($publicUrl, [UriKind]::Absolute, [ref]$parsed)) {
            Write-Host "Stripe webhook finalization wait is blocked."
            Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must be an absolute HTTPS URL."
            exit 2
        }

        if ($parsed.Scheme -ne "https" -or $parsed.Host -in @("localhost", "127.0.0.1", "::1")) {
            Write-Host "Stripe webhook finalization wait is blocked."
            Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must be a public HTTPS endpoint reachable by Stripe."
            exit 2
        }

        if (-not $parsed.AbsolutePath.EndsWith($endpointPath, [StringComparison]::OrdinalIgnoreCase)) {
            Write-Host "Stripe webhook finalization wait is blocked."
            Write-Host "DARWIN_STRIPE_WEBHOOK_PUBLIC_URL must end with $endpointPath."
            exit 2
        }
    }

    if (-not $forwardingConfirmed) {
        Write-Host "Stripe webhook finalization wait is blocked."
        Write-Host "Set DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED=true after Stripe Dashboard delivery or Stripe CLI forwarding is active."
        Write-Host "No webhook signing secret is accepted or printed by this check."
        exit 2
    }
}

function Invoke-DarwinJson {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$Body,
        [hashtable]$Headers = @{}
    )

    try {
        if ($Headers.Count -gt 0) {
            return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $Headers -Body $Body -ContentType "application/json"
        }

        return Invoke-RestMethod -Method $Method -Uri $Uri -Body $Body -ContentType "application/json"
    }
    catch {
        Write-SafeWebError -ErrorRecord $_
        throw
    }
}

function Get-DarwinJson {
    param([Parameter(Mandatory = $true)][string]$Uri)

    try {
        return Invoke-RestMethod -Method "Get" -Uri $Uri
    }
    catch {
        Write-SafeWebError -ErrorRecord $_
        throw
    }
}

function Write-SafeWebError {
    param([Parameter(Mandatory = $true)]$ErrorRecord)

    $response = $ErrorRecord.Exception.Response
    if ($null -eq $response) {
        return
    }

    $statusCode = [int]$response.StatusCode
    Write-Host "Darwin WebApi request failed with HTTP $statusCode."

    try {
        $stream = $response.GetResponseStream()
        if ($null -eq $stream) {
            return
        }

        $reader = [System.IO.StreamReader]::new($stream)
        try {
            $body = $reader.ReadToEnd()
            if ([string]::IsNullOrWhiteSpace($body)) {
                return
            }

            $safeBody = Hide-SensitiveText -Value $body
            if ($safeBody.Length -gt 1200) {
                $safeBody = $safeBody.Substring(0, 1200) + "...(truncated)"
            }

            Write-Host "Darwin WebApi error body: $safeBody"
        }
        finally {
            $reader.Dispose()
        }
    }
    catch {
        Write-Host "Darwin WebApi error body could not be read safely."
    }
}

function Hide-SensitiveText {
    param([string]$Value)

    if ([string]::IsNullOrEmpty($Value)) {
        return $Value
    }

    $patterns = @(
        '\b(sk|rk|pk)_(live|test)_[A-Za-z0-9]{12,}\b',
        ('\bwh' + 'sec_[A-Za-z0-9]{12,}\b'),
        '\bpi_[A-Za-z0-9]{12,}\b',
        '\bcs_(live|test)_[A-Za-z0-9]{12,}\b',
        '\bch_[A-Za-z0-9]{12,}\b',
        'https://checkout\.stripe\.com/[^\s"''<>]+'
    )

    $safe = $Value
    foreach ($pattern in $patterns) {
        $safe = [regex]::Replace($safe, $pattern, "[redacted]", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    }

    return $safe
}

function Get-SmokePaymentStatus {
    param(
        [Parameter(Mandatory = $true)]$Confirmation,
        [Parameter(Mandatory = $true)][string]$PaymentId
    )

    if ($null -eq $Confirmation.payments) {
        return $null
    }

    foreach ($payment in $Confirmation.payments) {
        if ([string]::Equals([string]$payment.id, $PaymentId, [StringComparison]::OrdinalIgnoreCase)) {
            return [string]$payment.status
        }
    }

    return $null
}

function Invoke-BusinessSubscriptionCheckoutSmoke {
    param([Parameter(Mandatory = $true)][string]$BaseUrl)

    $planId = Get-EnvValue "DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID"
    try {
        [Guid]::Parse($planId) | Out-Null
    }
    catch {
        Write-Error "DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID must be a valid billing plan GUID."
    }

    $token = Get-EnvValue "DARWIN_BUSINESS_API_BEARER_TOKEN"
    $body = @{
        planId = $planId
    } | ConvertTo-Json -Depth 5
    $headers = @{
        Authorization = "Bearer $token"
    }

    Write-Host "Creating Stripe test-mode business subscription Checkout Session through Darwin WebApi. No secrets or provider references will be printed."
    $response = Invoke-DarwinJson -Method "Post" -Uri "$BaseUrl/api/v1/business/billing/subscription/checkout-intent" -Body $body -Headers $headers

    if ([string]::IsNullOrWhiteSpace($response.checkoutUrl)) {
        Write-Error "Business subscription checkout did not return a checkout URL."
    }

    $checkoutUri = [Uri]$response.checkoutUrl
    if (-not $checkoutUri.Host.EndsWith("stripe.com", [StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "Business subscription checkout URL is not a Stripe-hosted URL."
    }

    if (-not [string]::Equals([string]$response.provider, "Stripe", [StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "Business subscription checkout did not return Stripe as the provider."
    }

    if ([string]::IsNullOrWhiteSpace($response.providerCheckoutSessionReference)) {
        Write-Error "Business subscription checkout did not persist a provider checkout session reference."
    }

    Write-Host "Business subscription Checkout Session handoff was created. Checkout host: $($checkoutUri.Host). Provider: Stripe."

    if ($OpenCheckout) {
        Start-Process -FilePath $checkoutUri.AbsoluteUri | Out-Null
        Write-Host "Stripe hosted subscription checkout was opened in the default browser. The checkout URL and provider references were not printed."
    }
}

function New-StripeSmokeOrder {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][string]$ProductSlug
    )

    $product = Get-DarwinJson -Uri "$BaseUrl/api/v1/public/catalog/products/$([Uri]::EscapeDataString($ProductSlug))"
    if ($null -eq $product.variants -or $product.variants.Count -eq 0) {
        Write-Error "Smoke product does not expose a purchasable variant."
    }

    $variant = $product.variants[0]
    $unitPriceNetMinor = [long]$variant.basePriceNetMinor
    if ($unitPriceNetMinor -le 0) {
        $unitPriceNetMinor = [long]$product.priceMinor
    }

    if ($unitPriceNetMinor -le 0) {
        Write-Error "Smoke product price is not usable."
    }

    $anonymousId = "stripe-smoke-" + [Guid]::NewGuid().ToString("N")
    $cartBody = @{
        anonymousId = $anonymousId
        variantId = $variant.id
        quantity = 1
        unitPriceNetMinor = $unitPriceNetMinor
        vatRate = 19
        currency = $product.currency
        selectedAddOnValueIds = @()
    } | ConvertTo-Json -Depth 10

    $cart = Invoke-DarwinJson -Method "Post" -Uri "$BaseUrl/api/v1/public/cart/items" -Body $cartBody
    $address = @{
        fullName = "Stripe Smoke"
        street1 = "Smoke Strasse 1"
        postalCode = "10115"
        city = "Berlin"
        countryCode = "DE"
        phoneE164 = "+4915112345678"
    }

    $intentBody = @{
        cartId = $cart.cartId
        shippingAddress = $address
    } | ConvertTo-Json -Depth 10
    $checkoutIntent = Invoke-DarwinJson -Method "Post" -Uri "$BaseUrl/api/v1/public/checkout/intent" -Body $intentBody

    $selectedShippingMethodId = $null
    if ($null -ne $checkoutIntent.shippingOptions -and $checkoutIntent.shippingOptions.Count -gt 0) {
        $selectedShippingMethodId = $checkoutIntent.shippingOptions[0].methodId
    }

    $orderBody = @{
        cartId = $cart.cartId
        billingAddress = $address
        shippingAddress = $address
        selectedShippingMethodId = $selectedShippingMethodId
        shippingTotalMinor = $checkoutIntent.selectedShippingTotalMinor
        culture = "de-DE"
    } | ConvertTo-Json -Depth 10

    $order = Invoke-DarwinJson -Method "Post" -Uri "$BaseUrl/api/v1/public/checkout/orders" -Body $orderBody
    if ([string]::IsNullOrWhiteSpace($order.orderId) -or [string]::IsNullOrWhiteSpace($order.orderNumber)) {
        Write-Error "Smoke order creation did not return an order identifier and order number."
    }

    return [pscustomobject]@{
        OrderId = [string]$order.orderId
        OrderNumber = [string]$order.orderNumber
    }
}

$baseUrl = (Get-EnvValue "DARWIN_WEBAPI_BASE_URL").TrimEnd("/")
if ($WaitForWebhookFinalization) {
    Assert-StripeWebhookForwardingReady
    Assert-StripeRuntimePipelineReady
}
elseif ($RequireRuntimePipeline) {
    Assert-StripeRuntimePipelineReady
}

if (-not $Execute) {
    Write-Host "Stripe test-mode smoke configuration is present."
    if ($RequireRuntimePipeline) {
        Write-Host "Stripe runtime pipeline readiness is confirmed."
    }

    Write-Host "Run with -Execute to call the local WebApi payment-intent endpoint. Add -CreateSmokeOrder to create a public checkout order first. Add -CheckBusinessSubscriptionCheckout to create a business subscription Checkout Session. Add -OpenCheckout to open Stripe hosted checkout without printing the session URL. Add -WaitForWebhookFinalization after paying test checkout to poll payment state. No secrets are printed."
    exit 0
}

if ($CheckBusinessSubscriptionCheckout) {
    Invoke-BusinessSubscriptionCheckoutSmoke -BaseUrl $baseUrl
    if (-not $CreateSmokeOrder) {
        Write-Host "Stripe test-mode subscription checkout smoke reached the provider handoff boundary. Complete provider validation by paying the Stripe test checkout and verifying subscription webhook events."
        exit 0
    }
}

if ($CreateSmokeOrder) {
    Write-Host "Creating a public storefront smoke order before Stripe handoff. No secrets or provider references will be printed."
    $smokeOrder = New-StripeSmokeOrder -BaseUrl $baseUrl -ProductSlug $SmokeProductSlug
    $orderId = $smokeOrder.OrderId
    $orderNumber = $smokeOrder.OrderNumber
    Write-Host "Smoke order created for Stripe handoff."
}
else {
    $orderId = Get-EnvValue "DARWIN_STRIPE_SMOKE_ORDER_ID"
    $orderNumber = Get-EnvValue "DARWIN_STRIPE_SMOKE_ORDER_NUMBER"
}

$intentBody = @{
    orderNumber = $orderNumber
    provider = "Stripe"
} | ConvertTo-Json -Depth 5

$intentUri = "$baseUrl/api/v1/public/checkout/orders/$orderId/payment-intent"
$confirmationUri = "$baseUrl/api/v1/public/checkout/orders/$orderId/confirmation?orderNumber=$([Uri]::EscapeDataString($orderNumber))"

Write-Host "Creating Stripe test-mode checkout session through Darwin WebApi. No secrets or provider references will be printed."
$intent = Invoke-DarwinJson -Method "Post" -Uri $intentUri -Body $intentBody

if ([string]::IsNullOrWhiteSpace($intent.checkoutUrl)) {
    Write-Error "Payment intent did not return a checkout URL."
}

$checkoutUri = [Uri]$intent.checkoutUrl
if (-not $checkoutUri.Host.EndsWith("stripe.com", [StringComparison]::OrdinalIgnoreCase)) {
    Write-Error "Payment intent checkout URL is not a Stripe-hosted URL."
}

if ($intent.status -ne "Pending") {
    Write-Error "Payment intent status should remain Pending before verified Stripe webhooks finalize it."
}

Write-Host "Checkout Session handoff was created. Checkout host: $($checkoutUri.Host). Payment status: $($intent.status)."

if ($OpenCheckout) {
    Start-Process -FilePath $checkoutUri.AbsoluteUri | Out-Null
    Write-Host "Stripe hosted checkout was opened in the default browser. The checkout URL and provider references were not printed."
}

if ($CheckReturnRoute) {
    $completeBody = @{
        orderNumber = $orderNumber
        providerReference = $intent.providerReference
        providerPaymentIntentReference = $intent.providerPaymentIntentReference
        providerCheckoutSessionReference = $intent.providerCheckoutSessionReference
        outcome = "Succeeded"
    } | ConvertTo-Json -Depth 5

    $completeUri = "$baseUrl/api/v1/public/checkout/orders/$orderId/payments/$($intent.paymentId)/complete"
    $completed = Invoke-DarwinJson -Method "Post" -Uri $completeUri -Body $completeBody

    if ($completed.paymentStatus -eq "Captured" -or $completed.paymentStatus -eq "Completed") {
        Write-Error "Storefront return route finalized a Stripe payment. Stripe finalization must remain webhook-only."
    }

    Write-Host "Return route checked. Payment status after return route: $($completed.paymentStatus)."
}

if ($WaitForWebhookFinalization) {
    if ($WebhookWaitSeconds -lt 5) {
        Write-Error "WebhookWaitSeconds must be at least 5 seconds."
    }

    if ($WebhookPollSeconds -lt 1) {
        Write-Error "WebhookPollSeconds must be at least 1 second."
    }

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($WebhookWaitSeconds)
    Write-Host "Polling Darwin WebApi for verified Stripe webhook finalization. Provider references will not be printed."

    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        $confirmation = Get-DarwinJson -Uri $confirmationUri
        $status = Get-SmokePaymentStatus -Confirmation $confirmation -PaymentId ([string]$intent.paymentId)

        if ($status -eq "Captured" -or $status -eq "Completed") {
            Write-Host "Verified Stripe webhook finalization reached payment status: $status."
            break
        }

        if ($status -eq "Failed" -or $status -eq "Voided") {
            Write-Error "Stripe webhook finalized the payment with non-success status: $status."
        }

        Start-Sleep -Seconds $WebhookPollSeconds
    }

    if ([DateTimeOffset]::UtcNow -ge $deadline) {
        Write-Error "Timed out waiting for verified Stripe webhook finalization."
    }
}

Write-Host "Stripe test-mode smoke reached the local handoff boundary. Complete provider validation by paying the Stripe test checkout and verifying webhook events."
