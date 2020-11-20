#Requires -Version 6

Param(
    [Parameter(Mandatory, HelpMessage = "Please enter a supported app. automotive, hospitality, inventory or careroom")]
    [ValidateSet('automotive', 'hospitality', 'inventory', 'careroom', IgnoreCase = $false, ErrorMessage = "Value '{0}' is invalid. Try one of these in lower case: '{1}'")]
    [string] $appName = $(Read-Host -prompt "appName"),

    [Parameter (Mandatory, HelpMessage = "Please enter a langauge (calture) code. The only value support now is en-us")]
    [ValidateSet('en-us', IgnoreCase = $false, ErrorMessage = "Value '{0}' is invalid. Try one of these in lower case: '{1}'")]
    [string] $language = $(Read-Host -prompt "language"),

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

# See region availability here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-custom-commands-application
$supportedRegions = "westus", "westus2", "eastus", "eastus2", "northeurope", "westeurope", "westcentralus", "centralindia", "eastasia", "southeastasia"
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
$luisPrediction = "$resourceName-prediction-$randomNumber"
$luisAuthoring  = "$resourceName-authoring-$randomNumber"
$storageName = "$resourceName$randomNumber".ToLower()
$cognitiveservice_speech_name = "$resourceName-speech"
$functionURL = "https://$functionName.azurewebsites.net/api/$((Get-Culture).TextInfo.ToTitleCase($appName))Demo"
$customCommandsWebEndpoint = $functionURL
$visualizationEndpoint = "https://$storageName.blob.core.windows.net/www/$appName.html?room=test1"

Write-Host -ForegroundColor Yellow "The deployment will be using default subscription ($subscriptionName) with following details:"
Write-Host "Demo name:            $appName"
Write-Host "Language:             $language"
Write-Host "Resource Group:       $resourceGroup"
Write-Host "Region:               $region"
Write-Host "LUIS prediction:      $luisPrediction"
Write-Host "LUIS authoring:       $luisAuthoring"
Write-Host "Storage name:         $storageName"
Write-Host "Azure function URL:   $functionURL"
Write-Host "Visualization URL:    $visualizationEndpoint"
Write-Host 
if ($(Write-Host -ForegroundColor Yellow "Please enter 'y' to proceed, or any other character to quit:"; Read-Host) -ne "y") {
    exit
}

$command = ".\deployTemplate.ps1 -resourceName $resourceName -region $region -luisPrediction $luisPrediction -luisAuthoring $luisAuthoring -functionName $functionName -storageName $storageName"
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

Write-Host "Getting additional Azure resouces needed to deploy a new Custom Command project"
$speechResourceKey = az cognitiveservices account keys list -g $resourceName -n $cognitiveservice_speech_name | ConvertFrom-Json
$speechResourceKey = $speechResourceKey.key1

$luisPredictionResourceId = az cognitiveservices account show -g $resourceName -n $luisPrediction | ConvertFrom-JSon 
$luisPredictionResourceId = $luisPredictionResourceId.id

$luisAuthoringResourceId = az cognitiveservices account show -g $resourceName -n $luisAuthoring | ConvertFrom-JSon 
$luisAuthoringResourceId = $luisAuthoringResourceId.id

$command = "./deployCustomCommands.ps1 -appName $appName -language $language -region $region -speechResourceKey $speechResourceKey -resourceName $resourceName -luisAuthoringResourceId $luisAuthoringResourceId -luisPredictionResourceId $luisPredictionResourceId -customCommandsWebEndpoint $customCommandsWebEndpoint"
Write-Host -ForegroundColor Yellow "Calling deployCustomCommands"
Write-Host -ForegroundColor Yellow $command
Invoke-Expression $command

Write-Host "    SpeechSubscriptionKey = $speechResourceKey"
Write-Host "    SpeechRegion          = $region"
Write-Host
Write-Host " To visualize the demo scene, point your browser to this address:"
Write-Host "    $visualizationEndpoint"
Write-Host
Write-Host "*******************************************************************"