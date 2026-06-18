param()

$ErrorActionPreference = "Stop"

function Add-Blocked {
    param(
        [System.Collections.Generic.List[string]]$Issues,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $Issues.Add($Message)
}

function Get-XmlElementText {
    param(
        [Parameter(Mandatory = $true)][xml]$Document,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $node = $Document.Project.PropertyGroup |
        ForEach-Object { $_.$Name } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1

    if ($null -eq $node) {
        return ""
    }

    return $node.ToString().Trim()
}

function Test-LowerDottedIdentifier {
    param([string]$Value)

    return $Value -match '^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*){2,}$'
}

function Test-PositiveInteger {
    param([string]$Value)

    $parsed = 0
    return [int]::TryParse($Value, [ref]$parsed) -and $parsed -gt 0
}

function Test-Project {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [System.Collections.Generic.List[string]]$Issues
    )

    if (-not (Test-Path $ProjectPath -PathType Leaf)) {
        Add-Blocked $Issues "$ProjectName project file is missing."
        return
    }

    [xml]$project = Get-Content $ProjectPath -Raw
    $projectText = Get-Content $ProjectPath -Raw
    $projectDirectory = Split-Path -Parent $ProjectPath
    $manifestPath = Join-Path $projectDirectory "Platforms\Android\AndroidManifest.xml"

    $targetFrameworks = Get-XmlElementText $project "TargetFrameworks"
    $applicationId = Get-XmlElementText $project "ApplicationId"
    $applicationTitle = Get-XmlElementText $project "ApplicationTitle"
    $displayVersion = Get-XmlElementText $project "ApplicationDisplayVersion"
    $applicationVersion = Get-XmlElementText $project "ApplicationVersion"
    $useMaui = Get-XmlElementText $project "UseMaui"
    $singleProject = Get-XmlElementText $project "SingleProject"
    $defaultLanguage = Get-XmlElementText $project "DefaultLanguage"

    if ($targetFrameworks -notmatch '(^|;)net10\.0-android(;|$)') {
        Add-Blocked $Issues "$ProjectName does not target net10.0-android."
    }

    if ($useMaui -ne "true" -or $singleProject -ne "true") {
        Add-Blocked $Issues "$ProjectName must remain a MAUI single-project app."
    }

    if (-not (Test-LowerDottedIdentifier $applicationId)) {
        Add-Blocked $Issues "$ProjectName ApplicationId must be a lower-case dotted Android package id."
    }

    if ([string]::IsNullOrWhiteSpace($applicationTitle)) {
        Add-Blocked $Issues "$ProjectName ApplicationTitle is missing."
    }

    if ([string]::IsNullOrWhiteSpace($displayVersion)) {
        Add-Blocked $Issues "$ProjectName ApplicationDisplayVersion is missing."
    }

    if (-not (Test-PositiveInteger $applicationVersion)) {
        Add-Blocked $Issues "$ProjectName ApplicationVersion must be a positive integer."
    }

    if ($defaultLanguage -ne "de-DE") {
        Add-Blocked $Issues "$ProjectName DefaultLanguage must remain de-DE for the current launch baseline."
    }

    if ($projectText -notmatch 'ValidateAndroidPushFirebaseConfig' -or
        $projectText -notmatch "google-services\.json is required for Android Release builds with FCM push integration\." -or
        $projectText -notmatch '<GoogleServicesJson Include="google-services\.json" Condition="Exists\(''google-services\.json''\)" />') {
        Add-Blocked $Issues "$ProjectName must keep the Android Release Firebase configuration guard and conditional GoogleServicesJson item."
    }

    if (-not (Test-Path $manifestPath -PathType Leaf)) {
        Add-Blocked $Issues "$ProjectName AndroidManifest.xml is missing."
        return
    }

    $manifest = Get-Content $manifestPath -Raw
    if ($manifest -notmatch 'android:usesCleartextTraffic="false"' -or $manifest -match 'android:usesCleartextTraffic="true"') {
        Add-Blocked $Issues "$ProjectName Android manifest must disable app-wide cleartext traffic."
    }

    if ($manifest -notmatch 'android:allowBackup="false"' -or $manifest -match 'android:allowBackup="true"') {
        Add-Blocked $Issues "$ProjectName Android manifest must disable platform backup."
    }

    foreach ($permission in @(
        "android.permission.ACCESS_NETWORK_STATE",
        "android.permission.INTERNET",
        "android.permission.CAMERA",
        "android.permission.POST_NOTIFICATIONS")) {
        if ($manifest -notmatch [regex]::Escape($permission)) {
            Add-Blocked $Issues "$ProjectName Android manifest is missing $permission."
        }
    }

    if ($manifest -notmatch '<uses-feature android:name="android\.hardware\.camera" android:required="false" />' -or
        $manifest -notmatch '<uses-feature android:name="android\.hardware\.camera\.autofocus" android:required="false" />') {
        Add-Blocked $Issues "$ProjectName Android manifest must keep camera hardware optional."
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$issues = New-Object System.Collections.Generic.List[string]

Push-Location $repoRoot
try {
    $projects = @(
        @{ Name = "Consumer"; Path = "src\Darwin.Mobile.Consumer\Darwin.Mobile.Consumer.csproj" },
        @{ Name = "Business"; Path = "src\Darwin.Mobile.Business\Darwin.Mobile.Business.csproj" }
    )

    foreach ($project in $projects) {
        Test-Project -ProjectName $project.Name -ProjectPath $project.Path -Issues $issues
    }

    $consumerProject = Get-Content "src\Darwin.Mobile.Consumer\Darwin.Mobile.Consumer.csproj" -Raw
    if ($consumerProject -notmatch 'ValidateAndroidGoogleMapsApiKey' -or
        $consumerProject -notmatch 'GOOGLE_MAPS_API_KEY is required for Android Release builds\.' -or
        $consumerProject -notmatch 'ANDROID_GOOGLE_MAPS_API_KEY') {
        Add-Blocked $issues "Consumer must keep the Android Release Google Maps key guard."
    }

    $consumerManifest = Get-Content "src\Darwin.Mobile.Consumer\Platforms\Android\AndroidManifest.xml" -Raw
    if ($consumerManifest -notmatch 'com\.google\.android\.geo\.API_KEY' -or
        $consumerManifest -notmatch '@string/google_maps_api_key') {
        Add-Blocked $issues "Consumer Android manifest must reference the generated Google Maps key resource."
    }

    $applicationIds = foreach ($project in $projects) {
        [xml]$document = Get-Content $project.Path -Raw
        Get-XmlElementText $document "ApplicationId"
    }

    if (($applicationIds | Select-Object -Unique).Count -ne $applicationIds.Count) {
        Add-Blocked $issues "Consumer and Business Android ApplicationId values must be unique."
    }

    $trackedFirebaseFiles = @(git ls-files -- "src/Darwin.Mobile.Consumer/google-services.json" "src/Darwin.Mobile.Business/google-services.json" 2>$null)
    if ($trackedFirebaseFiles.Count -gt 0) {
        Add-Blocked $issues "Firebase mobile configuration files must not be tracked in git; provide them through the approved build environment."
    }
}
finally {
    Pop-Location
}

if ($issues.Count -gt 0) {
    Write-Host "Android project readiness is blocked. Fix these local project prerequisites first:"
    foreach ($issue in $issues) {
        Write-Host " - $issue"
    }

    Write-Host "This check reads project and manifest metadata only. It does not read or print signing keys, keystore paths, Firebase file contents, API keys, OAuth secrets, private package artifacts, provider payloads, customer data, or device logs."
    exit 2
}

Write-Host "Android project readiness prerequisites are present."
Write-Host "Checked Consumer and Business Android target frameworks, app ids, version metadata, manifest transport/backup/camera/notification guards, Release Firebase guards, Consumer Maps guard, and git-tracked Firebase file safety."
Write-Host "No signing key, keystore path, Firebase file content, API key, OAuth secret, private artifact, provider response, customer data, or device log was accepted or printed."
