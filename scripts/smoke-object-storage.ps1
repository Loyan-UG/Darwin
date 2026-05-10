param(
    [switch]$Execute,
    [string]$Provider = $env:DARWIN_OBJECT_STORAGE_PROVIDER,
    [string]$ProfileName = $env:DARWIN_OBJECT_STORAGE_PROFILE,
    [string]$ContainerName = $env:DARWIN_OBJECT_STORAGE_CONTAINER,
    [string]$Prefix = $env:DARWIN_OBJECT_STORAGE_PREFIX,
    [string]$FileRoot = $env:DARWIN_OBJECT_STORAGE_FILE_ROOT,
    [switch]$SmokeRetention
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Provider)) {
    $Provider = "S3Compatible"
}

if ([string]::IsNullOrWhiteSpace($ProfileName)) {
    $ProfileName = "Smoke"
}

$providerName = $Provider.Trim()
Set-Item -Path Env:DARWIN_OBJECT_STORAGE_PROVIDER -Value $providerName
Set-Item -Path Env:DARWIN_OBJECT_STORAGE_PROFILE -Value $ProfileName.Trim()
if (-not [string]::IsNullOrWhiteSpace($ContainerName)) {
    Set-Item -Path Env:DARWIN_OBJECT_STORAGE_CONTAINER -Value $ContainerName.Trim()
}

if (-not [string]::IsNullOrWhiteSpace($Prefix)) {
    Set-Item -Path Env:DARWIN_OBJECT_STORAGE_PREFIX -Value $Prefix.Trim()
}

if (-not [string]::IsNullOrWhiteSpace($FileRoot)) {
    Set-Item -Path Env:DARWIN_OBJECT_STORAGE_FILE_ROOT -Value $FileRoot.Trim()
}

if ($SmokeRetention) {
    Set-Item -Path Env:DARWIN_OBJECT_STORAGE_SMOKE_RETENTION -Value "true"
}

$required = @("DARWIN_OBJECT_STORAGE_PROVIDER", "DARWIN_OBJECT_STORAGE_CONTAINER")

switch ($providerName.ToLowerInvariant()) {
    "s3compatible" {
        $required += @(
            "DARWIN_OBJECT_STORAGE_S3_BUCKET",
            "DARWIN_OBJECT_STORAGE_S3_ACCESS_KEY",
            "DARWIN_OBJECT_STORAGE_S3_SECRET_KEY"
        )

        if ([string]::IsNullOrWhiteSpace($env:DARWIN_OBJECT_STORAGE_S3_ENDPOINT) -and
            [string]::IsNullOrWhiteSpace($env:DARWIN_OBJECT_STORAGE_S3_REGION)) {
            $required += "DARWIN_OBJECT_STORAGE_S3_ENDPOINT_OR_REGION"
        }
    }
    "azureblob" {
        $required += @("DARWIN_OBJECT_STORAGE_AZURE_CONTAINER")
        if ([string]::IsNullOrWhiteSpace($env:DARWIN_OBJECT_STORAGE_AZURE_CONNECTION_STRING) -and
            ([string]::IsNullOrWhiteSpace($env:DARWIN_OBJECT_STORAGE_AZURE_ACCOUNT_NAME) -or
                -not [string]::Equals($env:DARWIN_OBJECT_STORAGE_AZURE_USE_MANAGED_IDENTITY, "true", [StringComparison]::OrdinalIgnoreCase))) {
            $required += "DARWIN_OBJECT_STORAGE_AZURE_CONNECTION_STRING_OR_MANAGED_IDENTITY"
        }
    }
    "filesystem" {
        $required += @("DARWIN_OBJECT_STORAGE_FILE_ROOT")
    }
    default {
        Write-Host "Object storage smoke is blocked. Provider must be S3Compatible, AzureBlob, or FileSystem."
        exit 2
    }
}

$missing = @()
foreach ($name in $required) {
    if ($name -eq "DARWIN_OBJECT_STORAGE_S3_ENDPOINT_OR_REGION") {
        $missing += $name
        continue
    }

    if ($name -eq "DARWIN_OBJECT_STORAGE_AZURE_CONNECTION_STRING_OR_MANAGED_IDENTITY") {
        $missing += $name
        continue
    }

    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Object storage smoke is blocked. Configure these environment variables first:"
    foreach ($name in $missing) {
        Write-Host " - $name"
    }

    Write-Host "Provider credentials must be supplied through environment or secure configuration. This script does not print secrets."
    exit 2
}

if (-not $Execute) {
    Write-Host "Object storage smoke configuration is present for provider '$providerName'."
    Write-Host "Run with -Execute to create, read, inspect, optionally generate a temporary URL for, and delete a disposable smoke object through the selected profile. Add -SmokeRetention to write a retained smoke object and skip cleanup for provider-level retention inspection. No secrets, object payloads, object keys, or provider credentials are printed."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($ContainerName)) {
    $ContainerName = [Environment]::GetEnvironmentVariable("DARWIN_OBJECT_STORAGE_CONTAINER")
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("darwin_object_storage_smoke_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

try {
    $projectPath = Join-Path $tempRoot "Darwin.ObjectStorageSmoke.csproj"
    $programPath = Join-Path $tempRoot "Program.cs"
    $infrastructureProject = Join-Path $repoRoot "src\Darwin.Infrastructure\Darwin.Infrastructure.csproj"

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$infrastructureProject" />
  </ItemGroup>
</Project>
"@ | Set-Content -Path $projectPath -Encoding UTF8

    @'
using System.Text;
using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

static string Env(string name) => Environment.GetEnvironmentVariable(name)?.Trim() ?? string.Empty;
static bool EnvBool(string name, bool fallback = false)
{
    var value = Env(name);
    return string.IsNullOrWhiteSpace(value) ? fallback : bool.TryParse(value, out var parsed) && parsed;
}

var provider = Env("DARWIN_OBJECT_STORAGE_PROVIDER");
var profileName = Env("DARWIN_OBJECT_STORAGE_PROFILE");
if (string.IsNullOrWhiteSpace(profileName))
{
    profileName = "Smoke";
}

var container = Env("DARWIN_OBJECT_STORAGE_CONTAINER");
var prefix = Env("DARWIN_OBJECT_STORAGE_PREFIX");
var pairs = new Dictionary<string, string?>
{
    ["ObjectStorage:Provider"] = provider,
    [$"ObjectStorage:Profiles:{profileName}:Provider"] = provider,
    [$"ObjectStorage:Profiles:{profileName}:ContainerName"] = container,
    [$"ObjectStorage:Profiles:{profileName}:Prefix"] = prefix,
    ["ObjectStorage:S3Compatible:Endpoint"] = Env("DARWIN_OBJECT_STORAGE_S3_ENDPOINT"),
    ["ObjectStorage:S3Compatible:Region"] = Env("DARWIN_OBJECT_STORAGE_S3_REGION"),
    ["ObjectStorage:S3Compatible:AccessKey"] = Env("DARWIN_OBJECT_STORAGE_S3_ACCESS_KEY"),
    ["ObjectStorage:S3Compatible:SecretKey"] = Env("DARWIN_OBJECT_STORAGE_S3_SECRET_KEY"),
    ["ObjectStorage:S3Compatible:BucketName"] = Env("DARWIN_OBJECT_STORAGE_S3_BUCKET"),
    ["ObjectStorage:S3Compatible:UseSsl"] = EnvBool("DARWIN_OBJECT_STORAGE_S3_USE_SSL", true).ToString(),
    ["ObjectStorage:S3Compatible:UsePathStyle"] = EnvBool("DARWIN_OBJECT_STORAGE_S3_USE_PATH_STYLE", true).ToString(),
    ["ObjectStorage:S3Compatible:ForcePathStyle"] = EnvBool("DARWIN_OBJECT_STORAGE_S3_FORCE_PATH_STYLE", true).ToString(),
    ["ObjectStorage:S3Compatible:RequireObjectLock"] = EnvBool("DARWIN_OBJECT_STORAGE_REQUIRE_OBJECT_LOCK", false).ToString(),
    ["ObjectStorage:S3Compatible:DefaultRetentionMode"] = Env("DARWIN_OBJECT_STORAGE_RETENTION_MODE"),
    ["ObjectStorage:S3Compatible:LegalHoldEnabled"] = EnvBool("DARWIN_OBJECT_STORAGE_LEGAL_HOLD_ENABLED", false).ToString(),
    ["ObjectStorage:S3Compatible:PublicBaseUrl"] = Env("DARWIN_OBJECT_STORAGE_PUBLIC_BASE_URL"),
    ["ObjectStorage:S3Compatible:ObjectLockValidationMode"] = Env("DARWIN_OBJECT_STORAGE_VALIDATION_MODE"),
    ["ObjectStorage:AzureBlob:ConnectionString"] = Env("DARWIN_OBJECT_STORAGE_AZURE_CONNECTION_STRING"),
    ["ObjectStorage:AzureBlob:AccountName"] = Env("DARWIN_OBJECT_STORAGE_AZURE_ACCOUNT_NAME"),
    ["ObjectStorage:AzureBlob:ContainerName"] = Env("DARWIN_OBJECT_STORAGE_AZURE_CONTAINER"),
    ["ObjectStorage:AzureBlob:UseManagedIdentity"] = EnvBool("DARWIN_OBJECT_STORAGE_AZURE_USE_MANAGED_IDENTITY", false).ToString(),
    ["ObjectStorage:AzureBlob:ClientId"] = Env("DARWIN_OBJECT_STORAGE_AZURE_CLIENT_ID"),
    ["ObjectStorage:AzureBlob:RequireImmutabilityPolicy"] = EnvBool("DARWIN_OBJECT_STORAGE_REQUIRE_OBJECT_LOCK", false).ToString(),
    ["ObjectStorage:AzureBlob:LegalHoldEnabled"] = EnvBool("DARWIN_OBJECT_STORAGE_LEGAL_HOLD_ENABLED", false).ToString(),
    ["ObjectStorage:AzureBlob:PublicBaseUrl"] = Env("DARWIN_OBJECT_STORAGE_PUBLIC_BASE_URL"),
    ["ObjectStorage:AzureBlob:ImmutabilityValidationMode"] = Env("DARWIN_OBJECT_STORAGE_VALIDATION_MODE"),
    ["ObjectStorage:FileSystem:RootPath"] = Env("DARWIN_OBJECT_STORAGE_FILE_ROOT"),
    ["ObjectStorage:FileSystem:PublicBaseUrl"] = Env("DARWIN_OBJECT_STORAGE_PUBLIC_BASE_URL")
};

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(pairs.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
    .Build();

var services = new ServiceCollection();
services.AddObjectStorageInfrastructure(configuration);
using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
using var scope = serviceProvider.CreateScope();
var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

var objectKey = ObjectStorageKeyBuilder.Build("smoke", DateTime.UtcNow.ToString("yyyyMMdd"), Guid.NewGuid().ToString("N") + ".txt");
var payload = Encoding.UTF8.GetBytes("Darwin object storage smoke " + DateTime.UtcNow.ToString("O"));
var retentionRequired = EnvBool("DARWIN_OBJECT_STORAGE_SMOKE_RETENTION", false);
var retentionMode = retentionRequired ? ObjectRetentionMode.Governance : ObjectRetentionMode.None;
var retainUntil = retentionRequired ? DateTime.UtcNow.AddDays(1) : (DateTime?)null;

await using var stream = new MemoryStream(payload, writable: false);
var write = await storage.SaveAsync(new ObjectStorageWriteRequest(
    ContainerName: container,
    ObjectKey: objectKey,
    ContentType: "text/plain",
    FileName: "smoke.txt",
    Content: stream,
    ContentLength: payload.Length,
    Metadata: new Dictionary<string, string> { ["source"] = "object-storage-smoke" },
    RetentionUntilUtc: retainUntil,
    RetentionMode: retentionMode,
    LegalHold: false,
    OverwritePolicy: ObjectOverwritePolicy.Disallow,
    ProfileName: profileName));

var reference = new ObjectStorageObjectReference(write.ContainerName, write.ObjectKey, write.VersionId, ProfileName: profileName);
if (!await storage.ExistsAsync(reference))
{
    throw new InvalidOperationException("Smoke object existence check failed.");
}

var metadata = await storage.GetMetadataAsync(reference);
if (metadata is null || string.IsNullOrWhiteSpace(metadata.Sha256Hash))
{
    throw new InvalidOperationException("Smoke object metadata check failed.");
}

var read = await storage.ReadAsync(reference);
if (read is null)
{
    throw new InvalidOperationException("Smoke object read failed.");
}

await using (read.Content)
{
    using var ms = new MemoryStream();
    await read.Content.CopyToAsync(ms);
    if (ms.Length != payload.Length)
    {
        throw new InvalidOperationException("Smoke object content length mismatch.");
    }
}

var tempUrl = await storage.GetTemporaryReadUrlAsync(new ObjectStorageTemporaryUrlRequest(reference, TimeSpan.FromMinutes(5)));
if (retentionRequired)
{
    Console.WriteLine("Object storage smoke completed save/read/metadata checks. Delete was skipped because retention smoke is enabled.");
}
else
{
    await storage.DeleteAsync(new ObjectStorageDeleteRequest(reference, "object storage smoke cleanup"));
    if (await storage.ExistsAsync(reference))
    {
        throw new InvalidOperationException("Smoke object delete check failed.");
    }

    Console.WriteLine("Object storage smoke completed save/read/metadata/temp-url/delete checks.");
}

Console.WriteLine("Provider: " + write.Provider);
Console.WriteLine("Temporary URL available: " + (tempUrl is not null));
'@ | Set-Content -Path $programPath -Encoding UTF8

    dotnet run --project $projectPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item -Path $tempRoot -Recurse -Force
    }
}
