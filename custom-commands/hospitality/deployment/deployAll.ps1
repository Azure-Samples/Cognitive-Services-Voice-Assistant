#Requires -Version 6

Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $region = $(Read-Host -prompt "region")
)

if( $resourceName.Length -gt 23 ){
    Write-Output "Resource Name cannot be longer than 23 characters, this is a requirement for storage accounts. Please enter a shorter name."
    exit
}

# get the current default subscription ID 
$defaultSubscription = az account list --output json | ConvertFrom-Json | where { $_.isDefault -eq "true" }

$azureSubscriptionID = $defaultSubscription.id 
$resourceGroup = $resourceName
$cognitiveservice_speech_name = "$resourceName-speech"
$cognitiveservice_luis_authoringkey_name = "$resourceName-luisauthoringkey"
$luisAuthoringRegion = "westus"
$CustomCommandsRegion = $region
$websiteAddress = "https://$resourceName-virtualroommanager.azurewebsites.net/api/RoomDemo"

.\deployTemplate.ps1 -resourceName $resourceName -region $region
.\deployContainerFiles.ps1 -resourceName $resourceName
.\deployAzureFunction.ps1 -resourceName $resourceName

#get the keys we need for the custom command deployment
$speechResourceKey = az cognitiveservices account keys list -g $resourceName -n $cognitiveservice_speech_name | ConvertFrom-Json
$speechResourceKey = $speechResourceKey.key1

$luisAuthoringKey = az cognitiveservices account keys list -g $resourceName -n $cognitiveservice_luis_authoringkey_name | ConvertFrom-Json
$luisAuthoringKey = $luisAuthoringKey.key1


./deployCustomCommands.ps1 `
-speechResourceKey $speechResourceKey `
-resourceName $resourceName `
-azureSubscriptionId $azureSubscriptionID `
-resourceGroup $resourceGroup `
-luisAuthoringKey $luisAuthoringKey `
-luisAuthoringRegion $luisAuthoringRegion `
-CustomCommandsRegion $CustomCommandsRegion `
-websiteAddress $websiteAddress

$visualizationEndpoint = "https://$resourceName.blob.core.windows.net/www/demo.html?room=test1"

Write-Host "    Speech Region = $region"
Write-Host "***********************"
Write-Host "To view your visualization go to this link."
Write-Host "    Visualization Endpoint = $visualizationEndpoint"
Write-Host "***********************"