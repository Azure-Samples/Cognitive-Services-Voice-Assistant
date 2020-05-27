#Requires -Version 6

Param(
    [Parameter(Mandatory, HelpMessage = "Please enter a supported app. automotive, hospitality, or inventory")]
    [ValidateSet('automotive', 'hospitality', 'inventory', IgnoreCase = $false, ErrorMessage = "Value '{0}' is invalid. Try one of these in lower case: '{1}'")]
    [string] $appName = $(Read-Host -prompt "appName"),
    [Parameter (Mandatory, HelpMessage = "Please enter a name for your resource. It must be < 19 characters and  Alphanumeric only")]
    [ValidatePattern("^\w+$", ErrorMessage = "resourceName must be alphanumeric")]
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [Parameter (Mandatory, HelpMessage = "Please enter a region. Supported regions are westus, northeurope")]
    [string] $region = $(Read-Host -prompt "region"),
    [string] $randomID
)

[Console]::ResetColor()
$ErrorActionPreference = "Stop"

if ($resourceName.Length -gt 19) {
    Write-Output "Resource Name cannot be longer than 19 characters. This is a requirement because we add up to 4 digits of a random number to try to keep the names unique and the storage resource has a limit of 23 characters. Please enter a shorter name."
    exit
}

$FreeAppServiceSupportedRegions = "westus", "northeurope"
$isRegionSupported = $FreeAppServiceSupportedRegions -eq $region
if ("" -eq $isRegionSupported) {
    Write-Output "Region '$region' is currently not supported for free app service plans. Please choose a region from the following: $FreeAppServiceSupportedRegions."
    exit
}

$supportedRegions = "westus", "westus2", "northeurope"
$isRegionSupported = $supportedRegions -eq $region
if ("" -eq $isRegionSupported) {
    Write-Output "Region '$region' is currently not supported for Custom Commands. Please choose a region from the following: $supportedRegions."
    exit
}

if ($randomID) {
    $randomNumber = $randomID
}
else {
    $randomNumber = Get-Random -maximum 9999
    Write-Host "Using random ID = $randomNumber"
}

# get the current default subscription ID
$userName = (az ad signed-in-user show | ConvertFrom-Json).userPrincipalName
Write-Host -ForegroundColor Yellow "`nThe logged in Azure account ($userName) has following subscription(s):"
az account list --all --output json | ConvertFrom-Json | Select-Object -Property isDefault, state, name, id | Out-Default
$defaultSubscription = az account list --all --output json | ConvertFrom-Json | Where-Object { $_.isDefault -eq "true" }
$subscriptionName = $defaultSubscription.name
$azureSubscriptionID = $defaultSubscription.id

if ($defaultSubscription.state -ne "Enabled") {
    Write-Output "The subscription $subscriptionName is not Enabled. Please select an enabled subscription to proceed."
    exit
}

$output = az group exists --name $resourceName
if ($output -eq $true) {
    Write-Error "Resource Group already exists. Please give a new resource name."
    exit
}

$resourceGroup = $resourceName
$functionName = "$resourceName-$randomNumber"
$luisName = "$resourceName-$randomNumber"
$luisKeyName = "$luisName-authoringkey"
$storageName = "$resourceName$randomNumber".ToLower()
$cognitiveservice_speech_name = "$resourceName-speech"
$luisAuthoringRegion = "westus"
$CustomCommandsRegion = $region
$functionURL = "https://$functionName.azurewebsites.net/api/$((Get-Culture).TextInfo.ToTitleCase($appName))Demo"
$websiteAddress = "https://$functionName.azurewebsites.net/api/$((Get-Culture).TextInfo.ToTitleCase($appName))Demo"

Write-Host -ForegroundColor Yellow "The deployment will be using default subscription ($subscriptionName) with following details:"
Write-Host "App name:             $appName"
Write-Host "Resource Group:       $resourceGroup"
Write-Host "Region:               $region"
Write-Host "LUIS name:            $luisName"
Write-Host "LUIS Key name:        $luisKeyName"
Write-Host "Storage name:         $storageName"
Write-Host "Website address:      $websiteAddress"
Write-Host ""
if ($(Write-Host -ForegroundColor Yellow "Please enter 'y' to proceed, or any other character to quit:"; Read-Host) -ne "y") {
    exit
}

$command = ".\deployTemplate.ps1 -resourceName $resourceName -region $region -luisName $luisName -functionName $functionName -storageName $storageName"
Write-Host -ForegroundColor Yellow "Calling deployTemplate"
Write-Host -ForegroundColor Yellow "$command"
Invoke-Expression $command

$command = ".\deployContainerFiles.ps1 -appName $appName -resourceName $resourceName -storageName $storageName -functionURL $functionURL"
Write-Host -ForegroundColor Yellow "Calling deployContainerFiles"
Write-Host -ForegroundColor Yellow "$command"
Invoke-Expression $command

$command = ".\deployAzureFunction.ps1 -appName $appName -resourceName $resourceName -functionName $functionName"
Write-Host -ForegroundColor Yellow "Calling deployAzureFunction"
Write-Host -ForegroundColor Yellow "$command"
Invoke-Expression $command

# get the keys we need for the custom command deployment
Write-Host "Getting new keys"
Write-Host "resource name = $resourceName"
Write-Host "speech name = $cognitiveservice_speech_name"
Write-Host "luis name = $luisKeyName"
$speechResourceKey = az cognitiveservices account keys list -g $resourceName -n $cognitiveservice_speech_name | ConvertFrom-Json
$speechResourceKey = $speechResourceKey.key1

$luisAuthoringKey = az cognitiveservices account keys list -g $resourceName -n $luisKeyName | ConvertFrom-Json
$luisAuthoringKey = $luisAuthoringKey.key1

$command = "./deployCustomCommands.ps1 -appName $appName -speechResourceKey $speechResourceKey -resourceName $resourceName -azureSubscriptionId $azureSubscriptionID -resourceGroup $resourceGroup -luisKeyName $luisKeyName -luisAuthoringKey $luisAuthoringKey -luisAuthoringRegion $luisAuthoringRegion -CustomCommandsRegion $CustomCommandsRegion -websiteAddress $websiteAddress"
Write-Host -ForegroundColor Yellow "Calling deployCustomCommands"
Write-Host -ForegroundColor Yellow $command
Invoke-Expression $command

$visualizationEndpoint = "https://$storageName.blob.core.windows.net/www/$appName.html?room=test1"

Write-Host "    Speech Region = $region"
Write-Host "***********************"
Write-Host "To view your visualization go to this link."
Write-Host "    Visualization Endpoint = $visualizationEndpoint"
Write-Host "***********************"