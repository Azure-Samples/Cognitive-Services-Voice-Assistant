#Requires -Version 6

Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $region = $(Read-Host -prompt "region")
)

$ErrorActionPreference = "Stop"

if( $resourceName.Length -gt 23 ){
    Write-Output "Resource Name cannot be longer than 23 characters, this is a requirement for storage accounts. Please enter a shorter name."
    exit
}

$supportedRegions = "westus", "westus2", "northeurope"
$isRegionSupported = $supportedRegions -eq $region

if("" -eq $isRegionSupported ){
    Write-Output "Region is currently not supported for Custom Commands. Please choose a region from the following: $supportedRegions."
    exit
}

$randomNumber = Get-Random -maximum 9999
$functionName = "$resourceName-$randomNumber"
$luisName = "$resourceName-$randomNumber"
$luisKeyName = "$luisName-authoringkey"
$storageName = "$resourceName$randomNumber"
$functionURL = "https://$functionName.azurewebsites.net/api/RoomDemo"
# get the current default subscription ID 
$defaultSubscription = az account list --output json | ConvertFrom-Json | Where-Object { $_.isDefault -eq "true" }

$azureSubscriptionID = $defaultSubscription.id 
$resourceGroup = $resourceName
$cognitiveservice_speech_name = "$resourceName-speech"
$luisAuthoringRegion = "westus"
$CustomCommandsRegion = $region
$websiteAddress = "https://$functionName.azurewebsites.net/api/RoomDemo"

Write-Host "Calling deployTemplate"
.\deployTemplate.ps1 -resourceName $resourceName -region $region -luisName $luisName -functionName $functionName -storageName $storageName

Write-Host "Calling deployContainerFiles"
.\deployContainerFiles.ps1 -resourceName $resourceName -storageName $storageName -functionURL $functionURL

Write-Host "Calling deployAzureFunction"
.\deployAzureFunction.ps1 -resourceName $resourceName -functionName $functionName

#get the keys we need for the custom command deployment
Write-Host "Getting new keys"
Write-Host "resource name = $resourceName"
Write-Host "speech name = $cognitiveservice_speech_name"
Write-Host "luis name = $luisKeyName"
$speechResourceKey = az cognitiveservices account keys list -g $resourceName -n $cognitiveservice_speech_name | ConvertFrom-Json
$speechResourceKey = $speechResourceKey.key1

$luisAuthoringKey = az cognitiveservices account keys list -g $resourceName -n $luisKeyName | ConvertFrom-Json
$luisAuthoringKey = $luisAuthoringKey.key1

Write-Host "Calling deployCustomCommands"

./deployCustomCommands.ps1 `
-speechResourceKey $speechResourceKey `
-resourceName $resourceName `
-azureSubscriptionId $azureSubscriptionID `
-resourceGroup $resourceGroup `
-luisKeyName $luisKeyName `
-luisAuthoringKey $luisAuthoringKey `
-luisAuthoringRegion $luisAuthoringRegion `
-CustomCommandsRegion $CustomCommandsRegion `
-websiteAddress $websiteAddress

$visualizationEndpoint = "https://$storageName.blob.core.windows.net/www/demo.html?room=test1"

Write-Host "    Speech Region = $region"
Write-Host "***********************"
Write-Host "To view your visualization go to this link."
Write-Host "    Visualization Endpoint = $visualizationEndpoint"
Write-Host "***********************"