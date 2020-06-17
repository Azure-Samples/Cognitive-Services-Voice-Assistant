Param(
    [string] $appName = $(Read-Host -prompt "appName"),
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $functionName = $(Read-Host -prompt "functionName")
)

[Console]::ResetColor()
$ErrorActionPreference = "Stop"
$resourceGroup = $resourceName
$publishZip = "publish.zip"
$publishFolder = "../$appName/azure-function/$((Get-Culture).TextInfo.ToTitleCase($appName))App/bin/Release/netcoreapp2.1/publish"
$sln = "../$appName/azure-function/$((Get-Culture).TextInfo.ToTitleCase($appName))App.sln"

Write-Host "Cleaning and building solution at $sln"
$output = dotnet clean $sln -c Release
Write-Verbose "$output"
$output = dotnet publish $sln -c Release
Write-Verbose "$output"

# zip up the publish folder
$absolutePublishFolder = Convert-Path($publishFolder)
$absoluteCurrentFolder = Convert-Path(".")
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($absolutePublishFolder, "$absoluteCurrentFolder/$publishZip")

Write-Host "Deploying zipped folder at $publishZip"
# deploy the zipped package
$output = az functionapp deployment source config-zip -g $resourceGroup -n $functionName --src $publishZip | ConvertFrom-Json
if (Test-path "$publishZip") { Remove-item "$publishZip" }
if (!$output) {
    Write-Error "Failed to deploy Azure function"
    Write-Error $output
    exit
}