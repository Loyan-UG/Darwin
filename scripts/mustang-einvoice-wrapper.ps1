param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"

function Get-ArgumentValue {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Items
    )

    for ($i = 0; $i -lt $Items.Count; $i++) {
        if ($Items[$i] -eq $Name -and ($i + 1) -lt $Items.Count) {
            return $Items[$i + 1]
        }
    }

    return $null
}

function Convert-MinorToAmount {
    param([long]$Minor)
    return ([decimal]$Minor / [decimal]100).ToString("0.00", [Globalization.CultureInfo]::InvariantCulture)
}

function Get-JsonString {
    param($Object, [string[]]$Names, [string]$Fallback = "")

    foreach ($name in $Names) {
        if ($Object.PSObject.Properties.Name -contains $name) {
            $value = $Object.$name
            if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) {
                return [string]$value
            }
        }
    }

    return $Fallback
}

function Convert-DateToCii {
    param([string]$Value)

    $parsed = [DateTimeOffset]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture)
    return $parsed.UtcDateTime.ToString("yyyyMMdd", [Globalization.CultureInfo]::InvariantCulture)
}

function Write-ValidationReport {
    param(
        [string]$Path,
        [bool]$IsValid,
        [string[]]$Issues = @()
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $report = [ordered]@{
        isValid = $IsValid
        issues = $Issues
        generator = "Mustangproject CLI wrapper"
    }

    $report | ConvertTo-Json -Depth 5 | Set-Content -Path $Path -Encoding UTF8
}

function New-CiiInvoiceXml {
    param($Source)

    $invoiceId = Get-JsonString $Source @("invoiceNumber", "invoiceId") "INV-SMOKE"
    $currency = Get-JsonString $Source @("currency") "EUR"
    $issuedAt = Convert-DateToCii (Get-JsonString $Source @("issuedAtUtc") ([DateTimeOffset]::UtcNow.ToString("O")))
    $seller = $Source.issuer
    $buyer = $Source.customer
    $sellerName = Get-JsonString $seller @("legalName", "companyName", "name") "Darwin Smoke Seller"
    $sellerTaxId = Get-JsonString $seller @("taxId", "vatId") "DE123456789"
    $sellerStreet = Get-JsonString $seller @("addressLine1") "Seller Street 1"
    $sellerPostal = Get-JsonString $seller @("postalCode") "10115"
    $sellerCity = Get-JsonString $seller @("city") "Berlin"
    $sellerCountry = Get-JsonString $seller @("country") "DE"
    $buyerName = Get-JsonString $buyer @("legalName", "companyName", "name", "firstName") "Customer"
    $buyerStreet = Get-JsonString $buyer @("addressLine1") "Buyer Street 2"
    $buyerPostal = Get-JsonString $buyer @("postalCode") "10115"
    $buyerCity = Get-JsonString $buyer @("city") "Berlin"
    $buyerCountry = Get-JsonString $buyer @("country") "DE"

    $lineElements = New-Object System.Text.StringBuilder
    $lineTotalMinor = 0L
    $taxTotalMinor = 0L
    $lineNumber = 1
    foreach ($line in @($Source.lines)) {
        $description = [Security.SecurityElement]::Escape((Get-JsonString $line @("description") "Invoice line"))
        $quantity = if ($line.PSObject.Properties.Name -contains "quantity") { [int]$line.quantity } else { 1 }
        $unitNetMinor = if ($line.PSObject.Properties.Name -contains "unitPriceNetMinor") { [long]$line.unitPriceNetMinor } else { 0L }
        $totalNetMinor = if ($line.PSObject.Properties.Name -contains "totalNetMinor") { [long]$line.totalNetMinor } else { $unitNetMinor * $quantity }
        $totalGrossMinor = if ($line.PSObject.Properties.Name -contains "totalGrossMinor") { [long]$line.totalGrossMinor } else { $totalNetMinor }
        $taxRate = if ($line.PSObject.Properties.Name -contains "taxRate") { [decimal]$line.taxRate } else { [decimal]19 }
        $lineTotalMinor += $totalNetMinor
        $taxTotalMinor += ($totalGrossMinor - $totalNetMinor)

        [void]$lineElements.AppendLine(@"
    <ram:IncludedSupplyChainTradeLineItem>
      <ram:AssociatedDocumentLineDocument><ram:LineID>$lineNumber</ram:LineID></ram:AssociatedDocumentLineDocument>
      <ram:SpecifiedTradeProduct><ram:Name>$description</ram:Name></ram:SpecifiedTradeProduct>
      <ram:SpecifiedLineTradeAgreement><ram:NetPriceProductTradePrice><ram:ChargeAmount>$(Convert-MinorToAmount $unitNetMinor)</ram:ChargeAmount></ram:NetPriceProductTradePrice></ram:SpecifiedLineTradeAgreement>
      <ram:SpecifiedLineTradeDelivery><ram:BilledQuantity unitCode="C62">$quantity</ram:BilledQuantity></ram:SpecifiedLineTradeDelivery>
      <ram:SpecifiedLineTradeSettlement>
        <ram:ApplicableTradeTax><ram:TypeCode>VAT</ram:TypeCode><ram:CategoryCode>S</ram:CategoryCode><ram:RateApplicablePercent>$($taxRate.ToString("0.00", [Globalization.CultureInfo]::InvariantCulture))</ram:RateApplicablePercent></ram:ApplicableTradeTax>
        <ram:SpecifiedTradeSettlementLineMonetarySummation><ram:LineTotalAmount>$(Convert-MinorToAmount $totalNetMinor)</ram:LineTotalAmount></ram:SpecifiedTradeSettlementLineMonetarySummation>
      </ram:SpecifiedLineTradeSettlement>
    </ram:IncludedSupplyChainTradeLineItem>
"@)
        $lineNumber++
    }

    if ($lineNumber -eq 1) {
        throw "The source invoice contains no invoice lines."
    }

    $grossMinor = if ($Source.PSObject.Properties.Name -contains "totalGrossMinor") { [long]$Source.totalGrossMinor } else { $lineTotalMinor + $taxTotalMinor }
    $taxBasisMinor = $lineTotalMinor
    $taxMinor = $grossMinor - $taxBasisMinor

    return @"
<?xml version="1.0" encoding="UTF-8"?>
<rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100" xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100">
  <rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>urn:cen.eu:en16931:2017</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext>
  <rsm:ExchangedDocument><ram:ID>$([Security.SecurityElement]::Escape($invoiceId))</ram:ID><ram:TypeCode>380</ram:TypeCode><ram:IssueDateTime><udt:DateTimeString format="102">$issuedAt</udt:DateTimeString></ram:IssueDateTime></rsm:ExchangedDocument>
  <rsm:SupplyChainTradeTransaction>
$($lineElements.ToString())
    <ram:ApplicableHeaderTradeAgreement>
      <ram:BuyerReference>not provided</ram:BuyerReference>
      <ram:SellerTradeParty><ram:Name>$([Security.SecurityElement]::Escape($sellerName))</ram:Name><ram:PostalTradeAddress><ram:PostcodeCode>$([Security.SecurityElement]::Escape($sellerPostal))</ram:PostcodeCode><ram:LineOne>$([Security.SecurityElement]::Escape($sellerStreet))</ram:LineOne><ram:CityName>$([Security.SecurityElement]::Escape($sellerCity))</ram:CityName><ram:CountryID>$([Security.SecurityElement]::Escape($sellerCountry))</ram:CountryID></ram:PostalTradeAddress><ram:SpecifiedTaxRegistration><ram:ID schemeID="VA">$([Security.SecurityElement]::Escape($sellerTaxId))</ram:ID></ram:SpecifiedTaxRegistration></ram:SellerTradeParty>
      <ram:BuyerTradeParty><ram:Name>$([Security.SecurityElement]::Escape($buyerName))</ram:Name><ram:PostalTradeAddress><ram:PostcodeCode>$([Security.SecurityElement]::Escape($buyerPostal))</ram:PostcodeCode><ram:LineOne>$([Security.SecurityElement]::Escape($buyerStreet))</ram:LineOne><ram:CityName>$([Security.SecurityElement]::Escape($buyerCity))</ram:CityName><ram:CountryID>$([Security.SecurityElement]::Escape($buyerCountry))</ram:CountryID></ram:PostalTradeAddress></ram:BuyerTradeParty>
    </ram:ApplicableHeaderTradeAgreement>
    <ram:ApplicableHeaderTradeDelivery><ram:ActualDeliverySupplyChainEvent><ram:OccurrenceDateTime><udt:DateTimeString format="102">$issuedAt</udt:DateTimeString></ram:OccurrenceDateTime></ram:ActualDeliverySupplyChainEvent></ram:ApplicableHeaderTradeDelivery>
    <ram:ApplicableHeaderTradeSettlement>
      <ram:InvoiceCurrencyCode>$([Security.SecurityElement]::Escape($currency))</ram:InvoiceCurrencyCode>
      <ram:ApplicableTradeTax><ram:CalculatedAmount>$(Convert-MinorToAmount $taxMinor)</ram:CalculatedAmount><ram:TypeCode>VAT</ram:TypeCode><ram:BasisAmount>$(Convert-MinorToAmount $taxBasisMinor)</ram:BasisAmount><ram:CategoryCode>S</ram:CategoryCode><ram:RateApplicablePercent>19.00</ram:RateApplicablePercent></ram:ApplicableTradeTax>
      <ram:SpecifiedTradePaymentTerms><ram:Description>Due on receipt</ram:Description></ram:SpecifiedTradePaymentTerms>
      <ram:SpecifiedTradeSettlementHeaderMonetarySummation><ram:LineTotalAmount>$(Convert-MinorToAmount $lineTotalMinor)</ram:LineTotalAmount><ram:ChargeTotalAmount>0.00</ram:ChargeTotalAmount><ram:AllowanceTotalAmount>0.00</ram:AllowanceTotalAmount><ram:TaxBasisTotalAmount>$(Convert-MinorToAmount $taxBasisMinor)</ram:TaxBasisTotalAmount><ram:TaxTotalAmount currencyID="$([Security.SecurityElement]::Escape($currency))">$(Convert-MinorToAmount $taxMinor)</ram:TaxTotalAmount><ram:GrandTotalAmount>$(Convert-MinorToAmount $grossMinor)</ram:GrandTotalAmount><ram:DuePayableAmount>$(Convert-MinorToAmount $grossMinor)</ram:DuePayableAmount></ram:SpecifiedTradeSettlementHeaderMonetarySummation>
    </ram:ApplicableHeaderTradeSettlement>
  </rsm:SupplyChainTradeTransaction>
</rsm:CrossIndustryInvoice>
"@
}

$inputPath = Get-ArgumentValue "--input" $Arguments
$outputPath = Get-ArgumentValue "--output" $Arguments
$format = Get-ArgumentValue "--format" $Arguments
$validationReportPath = Get-ArgumentValue "--validation-report" $Arguments

# Darwin's external-command adapter uses a stable positional contract for the
# validation profile and validation-report path after the named input/output
# arguments. Keep accepting the named form as a convenience for direct CLI use.
if ([string]::IsNullOrWhiteSpace($validationReportPath) -and $Arguments.Count -ge 8) {
    $validationReportPath = $Arguments[7]
}

if ([string]::IsNullOrWhiteSpace($inputPath) -or [string]::IsNullOrWhiteSpace($outputPath) -or [string]::IsNullOrWhiteSpace($format)) {
    throw "Arguments --input, --output, and --format are required."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$jarPath = $env:DARWIN_MUSTANG_CLI_JAR
if ([string]::IsNullOrWhiteSpace($jarPath)) {
    $jarPath = Join-Path $repoRoot ".darwin-tools\mustang\Mustang-CLI-2.23.1.jar"
}

if (-not (Test-Path $jarPath -PathType Leaf)) {
    throw "Mustang CLI jar was not found. Run scripts/install-mustang-cli.ps1 first or set DARWIN_MUSTANG_CLI_JAR."
}

$source = Get-Content -Path $inputPath -Raw | ConvertFrom-Json
$workRoot = Join-Path ([IO.Path]::GetTempPath()) ("darwin_mustang_wrapper_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $workRoot | Out-Null

try {
    $ciiPath = Join-Path $workRoot "invoice.xml"
    New-CiiInvoiceXml $source | Set-Content -Path $ciiPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $validationOutput = & java "-Xmx1G" "-Dfile.encoding=UTF-8" -jar $jarPath --action validate --no-notices --source $ciiPath 2>$null
    $validationExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($validationExitCode -ne 0) {
        Write-ValidationReport $validationReportPath $false @("Mustang validation failed for generated CII source XML.")
        exit 20
    }

    if ($format -eq "xrechnung") {
        Copy-Item -Path $ciiPath -Destination $outputPath -Force
        Write-ValidationReport $validationReportPath $true @()
        exit 0
    }

    if ($format -ne "zugferd-factur-x") {
        Write-ValidationReport $validationReportPath $false @("Unsupported e-invoice format '$format'.")
        exit 21
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $pdfOutput = & java "-Xmx1G" "-Dfile.encoding=UTF-8" -jar $jarPath --action pdf --language en --source $ciiPath --out $outputPath 2>$null
    $pdfExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($pdfExitCode -ne 0 -or -not (Test-Path $outputPath -PathType Leaf) -or (Get-Item $outputPath).Length -eq 0) {
        Write-ValidationReport $validationReportPath $false @("Mustang PDF/A-3 generation did not produce a non-empty ZUGFeRD/Factur-X PDF artifact.")
        exit 22
    }

    Write-ValidationReport $validationReportPath $true @()
}
finally {
    if (Test-Path $workRoot) {
        Remove-Item -Path $workRoot -Recurse -Force
    }
}
