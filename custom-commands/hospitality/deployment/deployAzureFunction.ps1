Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $functionName = $(Read-Host -prompt "functionName")
)

$ErrorActionPreference = "Stop"
$resourceGroup= $resourceName
$publishZip="publish.zip"
$publishFolder="../skill/VirtualRoomApp/bin/Release/netcoreapp2.1/publish"
$sln = "../skill/VirtualRoomApp.sln"

dotnet clean $sln -c Release

dotnet publish $sln -c Release

#zip up the publish folder
$absolutePublishFolder=Convert-Path($publishFolder)
$absoluteCurrentFolder=Convert-Path(".")
if(Test-path "$publishZip") {Remove-item "$publishZip"}
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($absolutePublishFolder, "$absoluteCurrentFolder/$publishZip")

# deploy the zipped package
az functionapp deployment source config-zip -g $resourceGroup -n $functionName --src $publishZip