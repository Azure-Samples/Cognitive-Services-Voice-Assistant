#Requires -Version 6

Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $region = $(Read-Host -prompt "region")
)

if( $resourceName.Length -gt 19 ){
    Write-Output "Resource Name cannot be longer than 19 characters, this is a requirement for storage accounts.`
    Storage accounts are limited to 23 characters and we are appending 4 random numbers to help make our URL's unique. Please enter a shorter name."
    exit
}

$randomNumber = Get-Random -maximum 9999
$uniqueResourceName = "$resourceName$randomNumber"

# get the current default subscription ID 
$defaultSubscription = az account list --output json | ConvertFrom-Json | Where-Object { $_.isDefault -eq "true" }

$azureSubscriptionID = $defaultSubscription.id 
$resourceGroup = $uniqueResourceName
$cognitiveservice_speech_name = "$uniqueResourceName-speech"
$cognitiveservice_luis_authoringkey_name = "$uniqueResourceName-luisauthoringkey"
$luisAuthoringRegion = "westus"
$CustomCommandsRegion = $region
$websiteAddress = "https://$uniqueResourceName-virtualroommanager.azurewebsites.net/api/RoomDemo"

.\deployTemplate.ps1 -resourceName $uniqueResourceName -region $region
.\deployContainerFiles.ps1 -resourceName $uniqueResourceName
.\deployAzureFunction.ps1 -resourceName $uniqueResourceName

#get the keys we need for the custom command deployment
$speechResourceKey = az cognitiveservices account keys list -g $uniqueResourceName -n $cognitiveservice_speech_name | ConvertFrom-Json
$speechResourceKey = $speechResourceKey.key1

$luisAuthoringKey = az cognitiveservices account keys list -g $uniqueResourceName -n $cognitiveservice_luis_authoringkey_name | ConvertFrom-Json
$luisAuthoringKey = $luisAuthoringKey.key1


./deployCustomCommands.ps1 `
-speechResourceKey $speechResourceKey `
-resourceName $uniqueResourceName `
-azureSubscriptionId $azureSubscriptionID `
-resourceGroup $resourceGroup `
-luisAuthoringKey $luisAuthoringKey `
-luisAuthoringRegion $luisAuthoringRegion `
-CustomCommandsRegion $CustomCommandsRegion `
-websiteAddress $websiteAddress

$visualizationEndpoint = "https://$uniqueResourceName.blob.core.windows.net/www/demo.html?room=test1"

Write-Host "    Speech Region = $region"
Write-Host "***********************"
Write-Host "To view your visualization go to this link."
Write-Host "    Visualization Endpoint = $visualizationEndpoint"
Write-Host "***********************"