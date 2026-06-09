param(
    [string]$BackupRoot = "X:\Projects\Darwin.Backup",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Add-BackupStatus {
    param(
        [System.Collections.Generic.List[object]]$Items,
        [string]$Name,
        [string]$Status,
        [string]$Detail = ""
    )

    $Items.Add([pscustomobject]@{
        name = $Name
        status = $Status
        detail = $Detail
    }) | Out-Null
}

function Assert-CommandAvailable {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found."
    }
}

function Get-DockerContainerEnv {
    param([string]$ContainerName)

    $envLines = docker inspect $ContainerName --format '{{range .Config.Env}}{{println .}}{{end}}' 2>$null
    $map = @{}
    foreach ($line in $envLines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line -notmatch "=") {
            continue
        }

        $parts = $line.Split("=", 2)
        $map[$parts[0]] = $parts[1]
    }

    return $map
}

function Test-DockerContainerRunning {
    param([string]$ContainerName)

    $state = docker inspect -f '{{.State.Running}}' $ContainerName 2>$null
    return $LASTEXITCODE -eq 0 -and $state -eq "true"
}

function Copy-IfExists {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        return $false
    }

    $parent = Split-Path -Parent $Destination
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
    return $true
}

function Get-RelativePathCompat {
    param(
        [string]$BasePath,
        [string]$Path
    )

    $baseUri = New-Object System.Uri((Resolve-Path -LiteralPath $BasePath).Path.TrimEnd("\") + "\")
    $pathUri = New-Object System.Uri((Resolve-Path -LiteralPath $Path).Path)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace("/", "\")
}

function Set-SensitiveAcl {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    try {
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        & icacls $Path /inheritance:r /grant:r "${currentUser}:(OI)(CI)F" "Administrators:(OI)(CI)F" /T /C | Out-Null
    } catch {
        Write-Warning "Could not restrict ACL for sensitive backup folder. Review permissions manually."
    }
}

function Get-FileManifest {
    param([string]$Root)

    if (-not (Test-Path -LiteralPath $Root)) {
        return @()
    }

    Get-ChildItem -LiteralPath $Root -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $relative = Get-RelativePathCompat -BasePath $Root -Path $_.FullName
            [pscustomobject]@{
                path = $relative.Replace("\", "/")
                bytes = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }
}

Assert-CommandAvailable -Name "docker"

if (-not (Test-Path -LiteralPath $BackupRoot)) {
    throw "Backup root '$BackupRoot' does not exist. Refusing to create a fallback inside the repository."
}

$today = Get-Date -Format "yyyy-MM-dd"
$backupDir = Join-Path $BackupRoot $today
$databaseDir = Join-Path $backupDir "databases"
$minioDir = Join-Path $backupDir "minio"
$sensitiveDir = Join-Path $backupDir "sensitive"
$manifestPath = Join-Path $backupDir "manifest.json"
$statuses = [System.Collections.Generic.List[object]]::new()

if ($DryRun) {
    Write-Host "Dry run: would update backup at $backupDir"
    Write-Host "Dry run: would dump PostgreSQL, SQL Server, MinIO, and local sensitive operational files."
    exit 0
}

New-Item -ItemType Directory -Force -Path $databaseDir, $minioDir, $sensitiveDir | Out-Null
Set-SensitiveAcl -Path $sensitiveDir

if (Test-DockerContainerRunning -ContainerName "darwin-postgres") {
    try {
        $pgDumpPath = Join-Path $databaseDir "postgres-darwin_dev.dump"
        docker exec darwin-postgres sh -lc "rm -f /tmp/postgres-darwin_dev.dump && pg_dump -U darwin -d darwin_dev -Fc -f /tmp/postgres-darwin_dev.dump" | Out-Null
        docker cp "darwin-postgres:/tmp/postgres-darwin_dev.dump" $pgDumpPath | Out-Null
        Add-BackupStatus -Items $statuses -Name "postgres" -Status "done" -Detail "PostgreSQL dump completed."
    } catch {
        Add-BackupStatus -Items $statuses -Name "postgres" -Status "failed" -Detail $_.Exception.Message
    }
} else {
    Add-BackupStatus -Items $statuses -Name "postgres" -Status "blocked" -Detail "darwin-postgres is not running."
}

if (Test-DockerContainerRunning -ContainerName "darwin-sqlserver") {
    try {
        $sqlBackupContainerDir = "/var/opt/mssql/backup/darwin-local"
        docker exec darwin-sqlserver bash -lc "mkdir -p '$sqlBackupContainerDir' && rm -f '$sqlBackupContainerDir'/*.bak" | Out-Null
        $dbNames = docker exec darwin-sqlserver bash -lc "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P `"`$MSSQL_SA_PASSWORD`" -C -h -1 -W -Q `"set nocount on; select name from sys.databases where database_id > 4 and state_desc = 'ONLINE';`""
        foreach ($dbNameRaw in $dbNames) {
            $dbName = if ($null -eq $dbNameRaw) { "" } else { $dbNameRaw.Trim() }
            if ([string]::IsNullOrWhiteSpace($dbName)) {
                continue
            }

            $safeDbName = $dbName -replace "[^A-Za-z0-9_.-]", "_"
            $bakName = "$safeDbName.bak"
            docker exec darwin-sqlserver bash -lc "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P `"`$MSSQL_SA_PASSWORD`" -C -Q `"BACKUP DATABASE [$dbName] TO DISK = N'$sqlBackupContainerDir/$bakName' WITH INIT, COMPRESSION, CHECKSUM;`"" | Out-Null
        }

        docker cp "darwin-sqlserver:$sqlBackupContainerDir/." $databaseDir | Out-Null
        Add-BackupStatus -Items $statuses -Name "sqlserver" -Status "done" -Detail "SQL Server user database backups completed."
    } catch {
        Add-BackupStatus -Items $statuses -Name "sqlserver" -Status "failed" -Detail $_.Exception.Message
    }
} else {
    Add-BackupStatus -Items $statuses -Name "sqlserver" -Status "blocked" -Detail "darwin-sqlserver is not running."
}

if (Test-DockerContainerRunning -ContainerName "darwin-minio") {
    try {
        $minioExportContainerDir = "/tmp/darwin-minio-export"
        docker exec darwin-minio sh -lc "rm -rf '$minioExportContainerDir' && mkdir -p '$minioExportContainerDir' && mc alias set darwin-local http://127.0.0.1:9000 `"`$MINIO_ROOT_USER`" `"`$MINIO_ROOT_PASSWORD`" >/dev/null" | Out-Null
        $bucketJsonLines = docker exec darwin-minio mc ls --json darwin-local
        foreach ($bucketJson in $bucketJsonLines) {
            if ([string]::IsNullOrWhiteSpace($bucketJson)) {
                continue
            }

            $bucketInfo = $bucketJson | ConvertFrom-Json
            $bucket = if ($null -eq $bucketInfo.key) { "" } else { $bucketInfo.key.TrimEnd("/") }
            if ([string]::IsNullOrWhiteSpace($bucket)) {
                continue
            }

            docker exec darwin-minio mc mirror --overwrite "darwin-local/$bucket" "$minioExportContainerDir/$bucket" | Out-Null
        }
        if (Test-Path -LiteralPath $minioDir) {
            Remove-Item -LiteralPath $minioDir -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $minioDir | Out-Null
        docker cp "darwin-minio:$minioExportContainerDir/." $minioDir | Out-Null
        Add-BackupStatus -Items $statuses -Name "minio" -Status "done" -Detail "MinIO bucket mirror completed."
    } catch {
        Add-BackupStatus -Items $statuses -Name "minio" -Status "failed" -Detail $_.Exception.Message
    }
} else {
    Add-BackupStatus -Items $statuses -Name "minio" -Status "blocked" -Detail "darwin-minio is not running."
}

$sensitiveCopied = 0
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$candidateSensitiveFiles = @(
    (Join-Path $repoRoot ".env"),
    (Join-Path $repoRoot ".env.local"),
    (Join-Path $repoRoot "src\Darwin.Web\.env.local")
)

foreach ($file in $candidateSensitiveFiles) {
    if (Test-Path -LiteralPath $file) {
        $destination = Join-Path $sensitiveDir ("repo\" + (Get-RelativePathCompat -BasePath $repoRoot -Path $file))
        if (Copy-IfExists -Source $file -Destination $destination) {
            $sensitiveCopied++
        }
    }
}

$oauthDir = "D:\_Projects\Loyan\OAuth"
if (Test-Path -LiteralPath $oauthDir) {
    $destination = Join-Path $sensitiveDir "oauth"
    Copy-Item -LiteralPath $oauthDir -Destination $destination -Recurse -Force
    $sensitiveCopied++
}

$userSecretsDir = Join-Path $env:APPDATA "Microsoft\UserSecrets"
if (Test-Path -LiteralPath $userSecretsDir) {
    $destination = Join-Path $sensitiveDir "dotnet-user-secrets"
    Copy-Item -LiteralPath $userSecretsDir -Destination $destination -Recurse -Force
    $sensitiveCopied++
}

Add-BackupStatus -Items $statuses -Name "sensitive-files" -Status "done" -Detail "$sensitiveCopied sensitive source groups copied."
Set-SensitiveAcl -Path $sensitiveDir

$manifest = [pscustomobject]@{
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    machineName = $env:COMPUTERNAME
    backupRoot = $BackupRoot
    backupDirectory = $backupDir
    gitHead = (git -C $repoRoot rev-parse --short HEAD 2>$null)
    dockerContainers = @(docker ps --format "{{.Names}}|{{.Image}}|{{.Status}}" | ForEach-Object {
        $parts = $_.Split("|", 3)
        [pscustomobject]@{
            name = $parts[0]
            image = $parts[1]
            status = $parts[2]
        }
    })
    statuses = $statuses
    files = @(Get-FileManifest -Root $backupDir)
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Backup updated: $backupDir"
Write-Host "Manifest: $manifestPath"
