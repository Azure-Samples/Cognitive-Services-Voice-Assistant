$resourceGroup="TestGroup2"
$functionAppName="mynewResource-virtualroommanager"
$publishZip="publish.zip"
$publishFolder="../skill/VirtualRoomApp/bin/Release/netcoreapp2.1/publish"

dotnet publish ../skill/VirtualRoomApp.sln -c Release

$absolutePublishFolder=Convert-Path($publishFolder)
$absoluteCurrentFolder=Convert-Path(".")
if(Test-path "$publishZip") {Remove-item "$publishZip"}
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($absolutePublishFolder, "$absoluteCurrentFolder/$publishZip")

# deploy the zipped package
az functionapp deployment source config-zip -g $resourceGroup -n $functionAppName --src $publishZip