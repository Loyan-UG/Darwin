param(
    [switch]$Execute,
    [switch]$CreateSmokeOrder,
    [switch]$CheckReturnRoute,
    [switch]$OpenCheckout,
    [switch]$WaitForWebhookFinalization,
    [string]$SmokeProductSlug = "iphone-15-pro-128",
    [int]$WebhookWaitSeconds = 180,
    [int]$WebhookPollSeconds = 5
)

$ErrorActionPreference = "Stop"

$required = @("DARWIN_WEBAPI_BASE_URL")
if (-not $CreateSmokeOrder) {
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

if (-not $Execute) {
    Write-Host "Stripe test-mode smoke configuration is present."
    Write-Host "Run with -Execute to call the local WebApi payment-intent endpoint. Add -CreateSmokeOrder to create a public checkout order first. Add -OpenCheckout to open Stripe hosted checkout without printing the session URL. Add -WaitForWebhookFinalization after paying test checkout to poll payment state. No secrets are printed."
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

function Invoke-DarwinJson {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$Body
    )

    return Invoke-RestMethod -Method $Method -Uri $Uri -Body $Body -ContentType "application/json"
}

function Get-DarwinJson {
    param([Parameter(Mandatory = $true)][string]$Uri)

    return Invoke-RestMethod -Method "Get" -Uri $Uri
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
