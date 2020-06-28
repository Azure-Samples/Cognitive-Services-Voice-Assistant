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
    "Content-Type"              = "application/json"
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

# change the model based on the local json file
write-host "patching the $speechAppName $appName commands model"
$newModel = Get-Content $skillJson | Out-String | ConvertFrom-Json
$newModel.httpEndpoints[0].url = $websiteAddress

# send the updated model up to the application
write-host "updating $speechAppName with the new $appName commands model"
try {
    $response = invoke-restmethod -Method PUT -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/v1.0/apps/$appId/slots/default/languages/en-us/model" -Body ($newModel | ConvertTo-Json  -depth 100) -Header $headers
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
$response = invoke-webrequest -Method POST -Uri "https://$CustomCommandsRegion.commands.speech.microsoft.com/v1.0/apps/$appId/slots/default/languages/en-us/train?force=true" -Header $headers
$OperationLocation = $response.Headers["Operation-Location"]
write-host -NoNewline "training Operation Location: $OperationLocation"

#
# wait until the training is complete
#

try {
    $response = invoke-restmethod -Method GET -Uri "$OperationLocation" -Header $headers
}
catch {
    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}

while ($response.status -ne "Succeeded") {
    start-sleep -seconds 1
    write-host -NoNewline "."
    try {
        $response = invoke-restmethod -Method GET -Uri "$OperationLocation" -Header $headers
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
# Direct following code will be interrupted by response status 405.
# $response = invoke-restmethod -Method POST -Uri "$($OperationLocation.replace('/train/','/publish/'))" -Header $headers
# Direct following code will not produce error, but response status 405 still is the result, which is same as running train-and-publish.sh. WVAC can not test properly after deployment.
$response = curl -s -X POST "$($OperationLocation.replace('/train/', '/publish/'))" -H "Ocp-Apim-Subscription-Key: $speechResourceKey" -H "Content-Length: 0"

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
