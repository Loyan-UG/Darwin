param(
    [string]$BackupRoot = "",
    [string]$BackupDate = "",
    [string]$PostgresContainerName = ""
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

function Resolve-PostgresContainerName {
    param([string]$RequestedName)

    if (-not [string]::IsNullOrWhiteSpace($RequestedName)) {
        return $RequestedName
    }

    $fromEnvironment = [Environment]::GetEnvironmentVariable("DARWIN_POSTGRES_CONTAINER")
    if (-not [string]::IsNullOrWhiteSpace($fromEnvironment)) {
        return $fromEnvironment
    }

    $runningContainers = @(docker ps --format "{{.Names}}" 2>$null)
    foreach ($candidate in @("darwin-postgres", "mit-maxx-postgres")) {
        if ($runningContainers -contains $candidate) {
            return $candidate
        }
    }

    $firstPostgres = @($runningContainers | Where-Object { $_ -match "postgres" } | Sort-Object | Select-Object -First 1)
    if ($firstPostgres.Count -gt 0) {
        return [string]$firstPostgres[0]
    }

    return ""
}

function Invoke-DockerChecked {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$FailureMessage = "Docker command failed."
    )

    $output = & docker @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw $FailureMessage
    }

    return @($output | ForEach-Object { $_.ToString() })
}

$resolvedBackupRoot = Resolve-BackupRoot -RequestedRoot $BackupRoot
$resolvedBackupDate = if ([string]::IsNullOrWhiteSpace($BackupDate)) { Get-Date -Format "yyyy-MM-dd" } else { $BackupDate.Trim() }
$backupDirectory = Join-Path $resolvedBackupRoot $resolvedBackupDate
$dumpPath = Join-Path $backupDirectory "databases\postgres-darwin_dev.dump"
$containerName = Resolve-PostgresContainerName -RequestedName $PostgresContainerName
$problems = [System.Collections.Generic.List[string]]::new()

if ([string]::IsNullOrWhiteSpace($containerName)) {
    Add-Problem $problems "No running PostgreSQL Docker container was found."
} elseif (-not ((docker ps --format "{{.Names}}") -contains $containerName)) {
    Add-Problem $problems "Configured PostgreSQL Docker container is not running."
}

if (-not (Test-Path -LiteralPath $dumpPath -PathType Leaf)) {
    Add-Problem $problems "PostgreSQL backup dump file is missing."
}

if ($problems.Count -gt 0) {
    Write-Host "Local PostgreSQL restore readiness is blocked."
    foreach ($problem in $problems) {
        Write-Host " - $problem"
    }
    Write-Host "No credentials, private operational files, provider payloads, dump contents, or restored data were printed."
    exit 2
}

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$restoreDatabaseName = "darwin_restore_smoke_$timestamp"
$containerDumpPath = "/tmp/darwin-restore-smoke-$timestamp.dump"
$createdDatabase = $false

try {
    Invoke-DockerChecked -Arguments @("cp", $dumpPath, "${containerName}:$containerDumpPath") -FailureMessage "Could not copy PostgreSQL dump into the restore container." | Out-Null

    Invoke-DockerChecked -Arguments @("exec", $containerName, "createdb", "-U", "postgres", $restoreDatabaseName) -FailureMessage "Could not create temporary restore database." | Out-Null
    $createdDatabase = $true

    Invoke-DockerChecked -Arguments @("exec", $containerName, "pg_restore", "--exit-on-error", "--no-owner", "-U", "postgres", "-d", $restoreDatabaseName, $containerDumpPath) -FailureMessage "PostgreSQL restore failed. Use the private restore log or rerun manually in the deployment evidence environment." | Out-Null

    $schemaCountQuery = "select count(*) from information_schema.schemata where schema_name not in ('pg_catalog','information_schema') and schema_name not like 'pg_toast%';"
    $tableCountQuery = "select count(*) from information_schema.tables where table_schema not in ('pg_catalog','information_schema') and table_schema not like 'pg_toast%';"
    $migrationTableCountQuery = "select count(*) from information_schema.tables where table_schema = 'public' and table_name = '__EFMigrationsHistory';"

    $schemaCountOutput = Invoke-DockerChecked -Arguments @("exec", $containerName, "psql", "-U", "postgres", "-d", $restoreDatabaseName, "-A", "-t", "-c", $schemaCountQuery) -FailureMessage "Could not inspect restored schema count."
    $tableCountOutput = Invoke-DockerChecked -Arguments @("exec", $containerName, "psql", "-U", "postgres", "-d", $restoreDatabaseName, "-A", "-t", "-c", $tableCountQuery) -FailureMessage "Could not inspect restored table count."
    $migrationTableCountOutput = Invoke-DockerChecked -Arguments @("exec", $containerName, "psql", "-U", "postgres", "-d", $restoreDatabaseName, "-A", "-t", "-c", $migrationTableCountQuery) -FailureMessage "Could not inspect restored migration history table."

    $schemaCount = [int]($schemaCountOutput | Select-Object -First 1)
    $tableCount = [int]($tableCountOutput | Select-Object -First 1)
    $migrationTableCount = [int]($migrationTableCountOutput | Select-Object -First 1)

    if ($schemaCount -le 0 -or $tableCount -le 0 -or $migrationTableCount -le 0) {
        Write-Host "Local PostgreSQL restore readiness is blocked."
        if ($schemaCount -le 0) { Write-Host " - Restored database has no application schemas." }
        if ($tableCount -le 0) { Write-Host " - Restored database has no application tables." }
        if ($migrationTableCount -le 0) { Write-Host " - Restored database has no EF migration history table." }
        Write-Host "No credentials, private operational files, provider payloads, dump contents, or restored data were printed."
        exit 2
    }

    Write-Host "Local PostgreSQL restore readiness prerequisites are present."
    Write-Host "Backup date: $resolvedBackupDate"
    Write-Host "Restore container: $containerName"
    Write-Host "Temporary restore database: created and restored, then scheduled for cleanup."
    Write-Host "Restore mode: pg_restore --no-owner into an isolated temporary database."
    Write-Host "Restored application schema count: $schemaCount"
    Write-Host "Restored application table count: $tableCount"
    Write-Host "Restored EF migration history table: present"
    Write-Host "No credentials, private operational files, provider payloads, dump contents, or restored data were printed."
}
catch {
    Write-Host "Local PostgreSQL restore readiness failed."
    Write-Host " - $($_.Exception.Message)"
    Write-Host "No credentials, private operational files, provider payloads, dump contents, or restored data were printed."
    exit 1
}
finally {
    if ($createdDatabase) {
        & docker exec $containerName dropdb -U postgres --if-exists $restoreDatabaseName 2>$null | Out-Null
    }

    if (-not [string]::IsNullOrWhiteSpace($containerDumpPath) -and -not [string]::IsNullOrWhiteSpace($containerName)) {
        & docker exec $containerName rm -f $containerDumpPath 2>$null | Out-Null
    }
}
