param(
    [string]$Version = "2.23.1",
    [string]$InstallDirectory = ".darwin-tools\mustang"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$targetDirectory = if ([System.IO.Path]::IsPathRooted($InstallDirectory)) {
    $InstallDirectory
}
else {
    Join-Path $repoRoot $InstallDirectory
}

New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null

$jarName = "Mustang-CLI-$Version.jar"
$jarPath = Join-Path $targetDirectory $jarName
$downloadUrl = "https://repo1.maven.org/maven2/org/mustangproject/Mustang-CLI/$Version/$jarName"

if (-not (Test-Path $jarPath -PathType Leaf)) {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $jarPath -TimeoutSec 180
}

$versionOutput = & java "-Xmx1G" "-Dfile.encoding=UTF-8" -jar $jarPath --help 2>&1 | Select-Object -First 1
if ($versionOutput -notmatch [regex]::Escape($Version)) {
    throw "Mustang CLI did not start correctly from '$jarPath'."
}

[pscustomobject]@{
    Version = $Version
    JarPath = (Resolve-Path $jarPath).Path
}
