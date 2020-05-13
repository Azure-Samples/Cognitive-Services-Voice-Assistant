#Requires -Version 6

Param(
    [string] $appName = $(Read-Host -prompt "appName"),
    [string] $speechResourceKey = $(Read-Host -prompt "speechResourceKey"),
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $azureSubscriptionId = $(Read-Host -prompt "azureSubscriptionId"),
    [string] $resourceGroup,
    [string] $luisAuthoringKey = $(Read-Host -prompt "luisAuthoringKey"),
    [string] $luisAuthoringRegion = "westus",
    [string] $luisKeyName = $(Read-Host -prompt "luisKeyName"),
    [string] $CustomCommandsRegion = "westus2",
    [string] $websiteAddress = $(Read-Host -prompt "websiteAddress")
)

[Console]::ResetColor()
$ErrorActionPreference = "Stop"

if (-not $resourceGroup) {
    $resourceGroup = $resourceName
}

$speechAppName = "$resourceName-commands"
$skillJson = "../$appName/skill/$((Get-Culture).TextInfo.ToTitleCase($appName))Demo.json"

#
# create the custom speech app
#

write-host "Creating the speech custom command project '$speechAppName'"
$body = @{
    name                    = $speechAppName
    stage                   = "default"
    culture                 = "en-us"
    description             = "updating the speech solution accelerator"
    skillEnabled            = "true"
    luisAuthoringResourceId = "/subscriptions/$azureSubscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.CognitiveServices/accounts/$luisKeyName"
    luisAuthoringKey        = $luisAuthoringKey
    luisAuthoringRegion     = $luisAuthoringRegion
}

$headers = @{
    "Ocp-Apim-Subscription-Key" = $speechResourceKey
    "accept"                    = "application/json"
    "Content-Type"              = "application/json-patch+json"
}

try {
    $response = invoke-restmethod -Method POST -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps" -Body (ConvertTo-Json $body) -Header $headers
}
catch {
    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}

$appId = $response.appId
write-host "Created project Id $appId"

#
# update the dialog model of the app
#

# get the current model so that we can modify it
write-host "getting the initial $speechAppName $appName commands model"
try {
    $model = invoke-restmethod -Method GET -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps/$appId/stages/default/cultures/en-us" -Header $headers
}
catch {
    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}

# change the model based on the local json file
write-host "patching the $speechAppName $appName commands model"
$newModel = Get-Content $skillJson | Out-String | ConvertFrom-Json
$model.httpEndpoints = $newModel.httpEndpoints
$model.httpEndpoints[0].url = $websiteAddress
$model.lgTemplates = $newModel.lgTemplates
$model.globalParameters = $newModel.globalParameters
$model.commands = $newModel.commands

# send the updated model up to the application
write-host "updating $speechAppName with the new $appName commands model"
try {
    $response = invoke-restmethod -Method PUT -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps/$appId/stages/default/cultures/en-us" -Body ($model | ConvertTo-Json  -depth 100) -Header $headers
}
catch {
    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception.Response
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}
write-host "...model update completed"

#
# start the training for the model
#

write-host "starting the training"
$response = invoke-restmethod -Method POST -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps/$appId/stages/default/cultures/en-us/train" -Header $headers
$versionId = $response.versionId
write-host -NoNewline "training version $versionId"

#
# wait until the training is complete
#

try {
    $response = invoke-restmethod -Method GET -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps/$appId/stages/default/cultures/en-us/train/$versionId" -Header $headers
}
catch {
    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}

while ($response.completed -ne "true") {
    start-sleep -seconds 1
    write-host -NoNewline "."
    try {
        $response = invoke-restmethod -Method GET -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps/$appId/stages/default/cultures/en-us/train/$versionId" -Header $headers
    }
    catch {
        # dig into the exception to get the Response details.
        # note that value__ is not a typo.
        Write-Host $_.Exception
        Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
        Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
        exit
    }
}
write-host
write-host "...training is completed"

#
# publish the model
#

write-host "publishing the model"
$response = invoke-restmethod -Method POST -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/apps/$appId/stages/default/cultures/en-us/publish/$versionId" -Header $headers
write-host "...model is published"

#
# print out the relevant info for the user to put in the application
#

Write-Host
Write-Host "***********************"
Write-Host "Custom commands has been published."

Write-Host "Update these parameters in your client to use the Custom Commands Application"
Write-Host "    CustomCommandsId = $appId"
Write-Host "    SpeechSubscriptionKey = $speechResourceKey"
