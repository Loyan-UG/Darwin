param(
    [switch]$IncludeIgnored
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root
try {
    $patterns = @(
        "xkeysib-",
        "BEGIN .*PRIVATE KEY",
        "POSTGRES_PASSWORD:\s*[^$]",
        "PGADMIN_DEFAULT_PASSWORD:\s*[^$]",
        "Password\s*=\s*[^.;\s][^;\r\n]*",
        "ApiKey\s*[:=]\s*""[^""]+""",
        "SigningKey\s*[:=]\s*""[^""]+""",
        "WebhookPassword\s*[:=]\s*""[^""]+""",
        "ClientSecret\s*[:=]\s*""[^""]+"""
    )

    if ($IncludeIgnored) {
        $files = git ls-files --cached --others
    }
    else {
        $files = git ls-files --cached --others --exclude-standard
    }

    $matches = $files |
        Where-Object { Test-Path $_ -PathType Leaf } |
        Where-Object {
            $_ -notmatch '(^|/)(bin|obj|node_modules|artifacts|\.codex-runtime|\.codex-build|\.codex-obj|\.vs|TestResults)(/|$)' -and
            $_ -notmatch '(^|/)\.dotnet(-runtime)?(/|$)' -and
            $_ -notmatch '(^|/)_shared_keys(/|$)'
        } |
        Select-String -Pattern $patterns -AllMatches |
        ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }

    if ($matches) {
        Write-Error ("Potential committed secret material found:`n" + ($matches -join "`n"))
        exit 1
    }

    Write-Host "No potential committed secrets found."
}
finally {
    Pop-Location
}
