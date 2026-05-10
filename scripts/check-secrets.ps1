param(
    [switch]$IncludeIgnored
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root
try {
    $configurationPatterns = @(
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

    $sourcePatterns = @(
        "xkeysib-[A-Za-z0-9_-]{16,}",
        "BEGIN .*PRIVATE KEY",
        "\bApiKey\s*=\s*""[^""]{8,}""",
        "\bClientSecret\s*=\s*""[^""]{8,}""",
        "\bWebhookPassword\s*=\s*""[^""]{8,}""",
        "\bJwtSigningKey\s*=\s*""[A-Za-z0-9+/=]{40,}""",
        "\bSigningKey\s*=\s*""[A-Za-z0-9+/=]{40,}""",
        "\bPassword\s*=\s*""[^""]{16,}"""
    )

    if ($IncludeIgnored) {
        $files = git ls-files --cached --others
    }
    else {
        $files = git ls-files --cached --others --exclude-standard
    }

    $candidateFiles = $files |
        Where-Object { Test-Path $_ -PathType Leaf } |
        Where-Object {
            $extension = [System.IO.Path]::GetExtension($_)
            $fileName = [System.IO.Path]::GetFileName($_)
            $_ -notmatch '(^|/)(bin|obj|node_modules|artifacts|\.[^.\/]+-(runtime|build|obj)|\.vs|TestResults)(/|$)' -and
            $_ -notmatch '(^|/)\.dotnet(-runtime)?(/|$)' -and
            $_ -notmatch '(^|/)_shared_keys(/|$)' -and
            $_ -notmatch '(^|/)scripts/' -and
            $_ -notmatch '(^|/)tests/' -and
            (
                $extension -in @('.cs', '.cshtml', '.razor', '.ts', '.tsx', '.js', '.jsx', '.json', '.yml', '.yaml', '.md', '.config', '.props', '.targets', '.pubxml') -or
                $fileName -eq '.env.example' -or
                $fileName -like 'appsettings*'
            )
        }

    $matches = foreach ($file in $candidateFiles) {
        $extension = [System.IO.Path]::GetExtension($file)
        $patterns = if ($extension -in @('.cs', '.cshtml', '.razor', '.ts', '.tsx', '.js', '.jsx')) {
            $sourcePatterns
        }
        else {
            $configurationPatterns
        }

        Select-String -Path $file -Pattern $patterns -AllMatches |
            Where-Object {
                $_.Line -notmatch 'change-this|REPLACE_|SET_BY_SECRET|NOT_FOR_PRODUCTION|example\.test|localhost|127\.0\.0\.1|xkeysib-\.\.\.|\$\{|xxxxxxxxxx|string\.Empty'
            } |
            ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    }

    if ($matches) {
        Write-Error ("Potential committed secret material found:`n" + ($matches -join "`n"))
        exit 1
    }

    Write-Host "No potential committed secrets found."
}
finally {
    Pop-Location
}
