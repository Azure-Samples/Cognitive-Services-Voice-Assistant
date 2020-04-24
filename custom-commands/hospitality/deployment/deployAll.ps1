#Requires -Version 6

Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $region = $(Read-Host -prompt "region"),
    [string] $randomID
)

$ErrorActionPreference = "Stop"

if( $resourceName.Length -gt 19 ){
    Write-Output "Resource Name cannot be longer than 19 characters. This is a requirement because we add up to 4 digits of a random number to try to keep the names unique and the storage resource has a limit of 23 characters. Please enter a shorter name."
    exit
}

$FreeAppServiceSupportedRegions = "westus", "northeurope"

$isRegionSupported = $FreeAppServiceSupportedRegions -eq $region

if("" -eq $isRegionSupported ){
    Write-Output "Region '$region' is currently not supported for free app service place. Please choose a region from the following: $FreeAppServiceSupportedRegions."
    exit
}

$supportedRegions = "westus", "westus2", "northeurope"
$isRegionSupported = $supportedRegions -eq $region

if("" -eq $isRegionSupported ){
    Write-Output "Region '$region' is currently not supported for Custom Commands. Please choose a region from the following: $supportedRegions."
    exit
}

if( $randomID){
    $randomNumber = $randomID
} else {
    $randomNumber = Get-Random -maximum 9999
    Write-Host "Using random ID = $randomNumber"
    Write-Host "pass this ID back into the command if you need to retry -randomID $randomNumber"
} 

$functionName = "$resourceName-$randomNumber"
$luisName = "$resourceName-$randomNumber"
$luisKeyName = "$luisName-authoringkey"
$storageName = "$resourceName$randomNumber".ToLower()
$functionURL = "https://$functionName.azurewebsites.net/api/RoomDemo"
# get the current default subscription ID 
$defaultSubscription = az account list --output json | ConvertFrom-Json | Where-Object { $_.isDefault -eq "true" }
$subscriptionName = $defaultSubscription.name

Write-Host "Using default subscription: $subscriptionName"
$azureSubscriptionID = $defaultSubscription.id 
$resourceGroup = $resourceName
$cognitiveservice_speech_name = "$resourceName-speech"
$luisAuthoringRegion = "westus"
$CustomCommandsRegion = $region
$websiteAddress = "https://$functionName.azurewebsites.net/api/RoomDemo"
$command = ".\deployTemplate.ps1 -resourceName $resourceName -region $region -luisName $luisName -functionName $functionName -storageName $storageName"
Write-Host "Calling deployTemplate"
Write-Host "$command"
Invoke-Expression $command

$command = ".\deployContainerFiles.ps1 -resourceName $resourceName -storageName $storageName -functionURL $functionURL"
Write-Host "Calling deployContainerFiles"
Write-Host "$command"
Invoke-Expression $command

$command = ".\deployAzureFunction.ps1 -resourceName $resourceName -functionName $functionName"
Write-Host "Calling deployAzureFunction"
Write-Host "$command"
Invoke-Expression $command

#get the keys we need for the custom command deployment
Write-Host "Getting new keys"
Write-Host "resource name = $resourceName"
Write-Host "speech name = $cognitiveservice_speech_name"
Write-Host "luis name = $luisKeyName"
$speechResourceKey = az cognitiveservices account keys list -g $resourceName -n $cognitiveservice_speech_name | ConvertFrom-Json
$speechResourceKey = $speechResourceKey.key1

$luisAuthoringKey = az cognitiveservices account keys list -g $resourceName -n $luisKeyName | ConvertFrom-Json
$luisAuthoringKey = $luisAuthoringKey.key1

$command = "./deployCustomCommands.ps1 -speechResourceKey $speechResourceKey -resourceName $resourceName -azureSubscriptionId $azureSubscriptionID -resourceGroup $resourceGroup -luisKeyName $luisKeyName -luisAuthoringKey $luisAuthoringKey -luisAuthoringRegion $luisAuthoringRegion -CustomCommandsRegion $CustomCommandsRegion -websiteAddress $websiteAddress"
Write-Host "Calling deployCustomCommands"
Write-Host $command
Invoke-Expression $command

$visualizationEndpoint = "https://$storageName.blob.core.windows.net/www/demo.html?room=test1"

Write-Host "    Speech Region = $region"
Write-Host "***********************"
Write-Host "To view your visualization go to this link."
Write-Host "    Visualization Endpoint = $visualizationEndpoint"
Write-Host "***********************"