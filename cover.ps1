#!/usr/bin/pwsh

Param(
    [switch]
    [Parameter(
        Mandatory = $false,
        HelpMessage = "Enable generation of a cobertura report")]
    $cobertura
)

$ErrorActionPreference="Stop"
Set-StrictMode -Version Latest

function script:exec {
    [CmdletBinding()]

	param(
		[Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
		[Parameter(Position=1,Mandatory=0)][string]$errorMessage = ("Error executing command: {0}" -f $cmd)
    )
	& $cmd
	if ($lastexitcode -ne 0)
	{
		throw $errorMessage
	}
}

# Install ReportGenerator
if (!(Test-Path "tools/reportgenerator") -and !(Test-Path "tools/reportgenerator.exe"))
{
    #Using alternate nuget.config due to https://github.com/dotnet/cli/issues/9586
    exec { dotnet tool install --configfile nuget.tool.config --tool-path tools dotnet-reportgenerator-globaltool }
}

Write-Host "Running dotnet test"
$filter = '\"[*TestAdapter*]*,[*]*.Startup*,[*]*.Program,[*.Test*]*,[nunit*]*\"'
$attributeFilter = '\"Obsolete,GeneratedCode,CompilerGeneratedAttribute\"'
exec { dotnet test `
    --configuration Release `
    --filter=TestCategory!=ApiTests `
    /p:CollectCoverage=true `
    /p:Exclude=$filter `
    /p:ExcludeByAttribute=$attributeFilter `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput='../coverage/Fluent.Net.coverage.cobertura.xml' `
    /p:Threshold=80 `
    /p:ThresholdType=branch `
    "Fluent.Net.Test/Fluent.Net.Test.csproj"}
$filter = '\"[*TestAdapter*]*,[*]*.Startup*,[*]*.Program,[*.Test*]*,[nunit*]*,[Fluent.Net]*\"'
exec { dotnet test `
    --configuration Release `
    --filter=TestCategory!=ApiTests `
    /p:CollectCoverage=true `
    /p:ExcludeByAttribute=$attributeFilter `
    /p:Exclude=$filter `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput='../coverage/Fluent.Net.SyntaxTest.coverage.cobertura.xml' `
    /p:Threshold=80 `
    /p:ThresholdType=branch `
    "Fluent.Net.SyntaxTest.Test/Fluent.Net.SyntaxTest.Test.csproj"}

Write-Host "Running ReportGenerator"
$reportTypes="-reporttypes:Html"
if ($cobertura)
{
    $reportTypes += ";Cobertura";
}
$coberturaFiles = "-reports:coverage/Fluent.Net.coverage.cobertura.xml;" + `
    "coverage/Fluent.Net.SyntaxTest.coverage.cobertura.xml"
exec { tools/reportgenerator $coberturaFiles "-targetdir:coverage" $reportTypes }
