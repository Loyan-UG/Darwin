$ErrorActionPreference = "Stop"

$checks = @(
    @{ Name = "Secrets scan"; Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\check-secrets.ps1"); ExpectedBlockedExitCode = $null },
    @{ Name = "Stripe test-mode smoke prerequisites"; Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-stripe-testmode.ps1", "-CreateSmokeOrder"); ExpectedBlockedExitCode = 2 },
    @{ Name = "DHL live smoke prerequisites"; Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-dhl-live.ps1"); ExpectedBlockedExitCode = 2 },
    @{ Name = "Brevo readiness smoke prerequisites"; Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-brevo-readiness.ps1"); ExpectedBlockedExitCode = 2 },
    @{ Name = "VIES live smoke prerequisites"; Command = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts\smoke-vies-live.ps1"); ExpectedBlockedExitCode = 2 }
)

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [string]$Detail = ""
    )

    $results.Add([pscustomobject]@{
        Name = $Name
        Status = $Status
        ExitCode = $ExitCode
        Detail = $Detail.Trim()
    })
}

foreach ($check in $checks) {
    $output = & $check.Command[0] $check.Command[1..($check.Command.Count - 1)] 2>&1
    $exitCode = $LASTEXITCODE

    $status = "Failed"
    if ($exitCode -eq 0) {
        $status = "Ready"
    }
    elseif ($null -ne $check.ExpectedBlockedExitCode -and $exitCode -eq $check.ExpectedBlockedExitCode) {
        $status = "Blocked"
    }

    Add-Result -Name $check.Name -Status $status -ExitCode $exitCode -Detail (($output | ForEach-Object { $_.ToString() }) -join "`n")
}

$archiveDecisionPath = "docs\archive-storage-provider-decision.md"
if (-not (Test-Path $archiveDecisionPath -PathType Leaf)) {
    Add-Result -Name "Invoice archive object-storage provider decision" -Status "Failed" -ExitCode 1 -Detail "Missing $archiveDecisionPath."
}
else {
    $archiveDecision = Get-Content $archiveDecisionPath -Raw
    if ($archiveDecision -match "No option is selected yet\." -and
        $archiveDecision -match "Do not choose Azure, AWS, MinIO, or another provider implicitly from code\.") {
        Add-Result -Name "Invoice archive object-storage provider decision" -Status "Blocked" -ExitCode 2 -Detail "Provider selection is still open. Use docs/archive-storage-provider-decision.md before implementing a production archive adapter."
    }
    else {
        Add-Result -Name "Invoice archive object-storage provider decision" -Status "Ready" -ExitCode 0 -Detail "Provider decision document no longer states that selection is open. Verify implementation and smoke coverage before production."
    }
}

$eInvoiceDecisionPath = "docs\e-invoice-tooling-decision.md"
if (-not (Test-Path $eInvoiceDecisionPath -PathType Leaf)) {
    Add-Result -Name "E-invoice tooling decision" -Status "Failed" -ExitCode 1 -Detail "Missing $eInvoiceDecisionPath."
}
else {
    $eInvoiceDecision = Get-Content $eInvoiceDecisionPath -Raw
    if ($eInvoiceDecision -match "No library or tooling is selected yet\." -and
        $eInvoiceDecision -match "Do not claim full e-invoice compliance from JSON, HTML, or CSV outputs\.") {
        Add-Result -Name "E-invoice tooling decision" -Status "Blocked" -ExitCode 2 -Detail "Tooling selection is still open. Use docs/e-invoice-tooling-decision.md before implementing ZUGFeRD/Factur-X generation."
    }
    else {
        Add-Result -Name "E-invoice tooling decision" -Status "Ready" -ExitCode 0 -Detail "Tooling decision document no longer states that selection is open. Verify generation, validation, and download coverage before production."
    }
}

Write-Host "Darwin go-live readiness dry-run summary:"
foreach ($result in $results) {
    Write-Host " - $($result.Name): $($result.Status)"
}

$failed = $results | Where-Object { $_.Status -eq "Failed" }
if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed checks:"
    foreach ($result in $failed) {
        Write-Host "[$($result.Name)]"
        Write-Host $result.Detail
    }

    exit 1
}

$blocked = $results | Where-Object { $_.Status -eq "Blocked" }
if ($blocked.Count -gt 0) {
    Write-Host ""
    Write-Host "Blocked go-live prerequisites:"
    foreach ($result in $blocked) {
        Write-Host "[$($result.Name)]"
        Write-Host $result.Detail
    }

    exit 2
}

Write-Host "All local readiness checks are ready. External smoke execution still requires explicit operator approval."
