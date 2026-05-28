param(
    [ValidateSet(
        "CoreSecurity",
        "RenderLists",
        "RenderEditors",
        "BusinessOnboarding",
        "BillingProvider",
        "ShippingProvider",
        "All")]
    [string]$Lane = "All",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$project = "tests\Darwin.WebAdmin.Tests\Darwin.WebAdmin.Tests.csproj"
$baseArgs = @("test", $project, "/p:UseSharedCompilation=false")
if ($NoRestore) {
    $baseArgs += "--no-restore"
}

$lanes = [ordered]@{
    CoreSecurity = "FullyQualifiedName~UnauthenticatedAdminRoot|FullyQualifiedName~ForwardedHttpsRequest|FullyQualifiedName~LoginPage|FullyQualifiedName~RegisterPage|FullyQualifiedName~Culture|FullyQualifiedName~AccountPostEndpoints|FullyQualifiedName~Logout|FullyQualifiedName~Permission|FullyQualifiedName~DataProtection|FullyQualifiedName~ProtectedAuthAntiBot"
    RenderLists = "FullyQualifiedName~AuthenticatedAdminListPages|FullyQualifiedName~AuthenticatedDashboard|FullyQualifiedName~AuthenticatedAlertsFragment"
    RenderEditors = "FullyQualifiedName~AuthenticatedAdminCreateEditors|FullyQualifiedName~AuthenticatedSeededEntityPages|FullyQualifiedName~AuthenticatedHtmxListAndDetailPartials|FullyQualifiedName~AuthenticatedHtmxEditorPartials"
    BusinessOnboarding = "FullyQualifiedName~Business|FullyQualifiedName~Onboarding|FullyQualifiedName~Invitation|FullyQualifiedName~BusinessCommunications"
    BillingProvider = "FullyQualifiedName~AuthenticatedSiteSettingsEdit|FullyQualifiedName~AuthenticatedBillingWebhookDelivery|FullyQualifiedName~AuthenticatedProviderCallback"
    ShippingProvider = "FullyQualifiedName~Shipment|FullyQualifiedName~Return|FullyQualifiedName~ProviderOperation|FullyQualifiedName~Dhl"
}

$selected = if ($Lane -eq "All") { $lanes.Keys } else { @($Lane) }

foreach ($name in $selected) {
    Write-Host "Running WebAdmin focused lane: $name"
    & dotnet @baseArgs --filter $lanes[$name]
    if ($LASTEXITCODE -ne 0) {
        throw "WebAdmin focused lane failed: $name"
    }
}
