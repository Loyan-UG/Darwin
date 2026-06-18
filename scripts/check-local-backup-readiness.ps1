param(
    [string]$BackupRoot = "",
    [string]$BackupDate = ""
)

$ErrorActionPreference = "Stop"

function Add-Problem {
    param(
        [System.Collections.Generic.List[string]]$Problems,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $Problems.Add($Message) | Out-Null
}

function Resolve-BackupRoot {
    param([string]$RequestedRoot)

    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        return $RequestedRoot
    }

    $fromEnvironment = [Environment]::GetEnvironmentVariable("DARWIN_LOCAL_BACKUP_ROOT")
    if (-not [string]::IsNullOrWhiteSpace($fromEnvironment)) {
        return $fromEnvironment
    }

    if (Test-Path -LiteralPath "D:\Backup" -PathType Container) {
        return "D:\Backup"
    }

    return "X:\Projects\Darwin.Backup"
}

function Get-ManifestStatus {
    param(
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $item = @($Manifest.statuses | Where-Object { $_.name -eq $Name } | Select-Object -First 1)
    if ($item.Count -eq 0) {
        return ""
    }

    return [string]$item[0].status
}

function Get-ManifestFile {
    param(
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $normalized = $Path.Replace("\", "/")
    $item = @($Manifest.files | Where-Object { $_.path -eq $normalized } | Select-Object -First 1)
    if ($item.Count -eq 0) {
        return $null
    }

    return $item[0]
}

$resolvedBackupRoot = Resolve-BackupRoot -RequestedRoot $BackupRoot
$resolvedBackupDate = if ([string]::IsNullOrWhiteSpace($BackupDate)) { Get-Date -Format "yyyy-MM-dd" } else { $BackupDate.Trim() }
$backupDirectory = Join-Path $resolvedBackupRoot $resolvedBackupDate
$manifestPath = Join-Path $backupDirectory "manifest.json"
$problems = [System.Collections.Generic.List[string]]::new()
$failures = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $backupDirectory -PathType Container)) {
    Add-Problem $problems "Backup directory is missing for date $resolvedBackupDate."
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    Add-Problem $problems "Backup manifest is missing for date $resolvedBackupDate."
}

if ($problems.Count -gt 0) {
    Write-Host "Local backup readiness is blocked."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }
    Write-Host "No credentials, private operational files, provider payloads, or backup file contents were printed."
    exit 2
}

try {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
} catch {
    Write-Host "Local backup readiness failed."
    Write-Host " - Backup manifest could not be parsed as JSON."
    Write-Host "No credentials, private operational files, provider payloads, or backup file contents were printed."
    exit 1
}

foreach ($requiredStatus in @("postgres", "minio", "sensitive-files")) {
    $status = Get-ManifestStatus -Manifest $manifest -Name $requiredStatus
    if ([string]::IsNullOrWhiteSpace($status)) {
        Add-Problem $problems "Manifest is missing '$requiredStatus' backup status."
    } elseif ($status -ne "done") {
        Add-Problem $problems "Manifest status '$requiredStatus' is '$status', expected 'done'."
    }
}

$postgresDumpPath = Join-Path $backupDirectory "databases\postgres-darwin_dev.dump"
$postgresManifestFile = Get-ManifestFile -Manifest $manifest -Path "databases/postgres-darwin_dev.dump"
if (-not (Test-Path -LiteralPath $postgresDumpPath -PathType Leaf)) {
    Add-Problem $problems "PostgreSQL dump file is missing."
} elseif ($null -eq $postgresManifestFile) {
    Add-Problem $problems "PostgreSQL dump is missing from the manifest."
} else {
    $dump = Get-Item -LiteralPath $postgresDumpPath
    if ($dump.Length -le 0) {
        Add-Problem $problems "PostgreSQL dump file is empty."
    }

    if ([int64]$postgresManifestFile.bytes -ne [int64]$dump.Length) {
        Add-Problem $problems "PostgreSQL dump byte count does not match the manifest."
    }

    $actualHash = (Get-FileHash -LiteralPath $postgresDumpPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne ([string]$postgresManifestFile.sha256).ToLowerInvariant()) {
        Add-Problem $problems "PostgreSQL dump SHA-256 does not match the manifest."
    }
}

$minioFileCount = @($manifest.files | Where-Object { ([string]$_.path).StartsWith("minio/", [StringComparison]::OrdinalIgnoreCase) }).Count
if ($minioFileCount -le 0) {
    Add-Problem $problems "Manifest has no MinIO mirror files."
}

$privateFileCount = @($manifest.files | Where-Object { ([string]$_.path).StartsWith("sensitive/", [StringComparison]::OrdinalIgnoreCase) }).Count
if ($privateFileCount -le 0) {
    Add-Problem $problems "Manifest has no private local configuration file group entries."
}

$containerCount = @($manifest.dockerContainers).Count
if ($containerCount -le 0) {
    Add-Problem $problems "Manifest has no Docker container inventory."
}

if ($failures.Count -gt 0) {
    Write-Host "Local backup readiness failed."
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    Write-Host "No credentials, private operational files, provider payloads, or backup file contents were printed."
    exit 1
}

if ($problems.Count -gt 0) {
    Write-Host "Local backup readiness is blocked."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }
    Write-Host "No credentials, private operational files, provider payloads, or backup file contents were printed."
    exit 2
}

$createdAtUtc = if ($null -ne $manifest.createdAtUtc) { [string]$manifest.createdAtUtc } else { "" }
$postgresBytes = if ($null -ne $postgresManifestFile) { [int64]$postgresManifestFile.bytes } else { 0 }

Write-Host "Local backup readiness prerequisites are present."
Write-Host "Backup date: $resolvedBackupDate"
Write-Host "Manifest created at UTC: $createdAtUtc"
Write-Host "PostgreSQL dump: present, byte count and SHA-256 match the manifest."
Write-Host "PostgreSQL dump bytes: $postgresBytes"
Write-Host "MinIO mirror file count: $minioFileCount"
Write-Host "Private local configuration file groups: present."
Write-Host "Docker container inventory count: $containerCount"
Write-Host "No credentials, private operational files, provider payloads, or backup file contents were printed."
exit 0
