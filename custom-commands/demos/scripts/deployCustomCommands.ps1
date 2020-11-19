#Requires -Version 6

Param(
    [string] $appName = $(Read-Host -prompt "appName"),
    [string] $language = $(Read-Host -prompt "language"),
    [string] $speechResourceKey = $(Read-Host -prompt "speechResourceKey"),
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $azureSubscriptionId = $(Read-Host -prompt "azureSubscriptionId"),
    [string] $resourceGroup,
    [string] $luisKeyName = $(Read-Host -prompt "luisKeyName"),
    [string] $luisAuthoringResourceId = $(Read-Host -prompt "luisAuthoringResourceId"),
    [string] $luisAuthoringRegion = "westus",
    [string] $luisPredictionResourceId = $(Read-Host -prmpot "luisPredictionResoureceId"), 
    [string] $customCommandsRegion = "westus2",
    [string] $customCommandsWebEndpoint = $(Read-Host -prompt "cutomCommandsWebEndpoint")
)

write-host "+++++++++++++++++++++++++++++++++++++++++"
write-host "appName = $appName"
write-host "langauge = $language"
write-host "speechResourceKey = $speechResourceKey"
write-host "resourceName = $resourceName"
write-host "azureSubscriptionId = $azureSubscriptionId"
write-host "resourceGroup = $resourceGroup"
write-host "luisKeyName = $luisKeyName"
write-host "luisAuthoringResourceId = $luisAuthoringResourceId"
write-host "luisAuthoringRegion = $luisAuthoringRegion"
write-host "luisPredictionResourceId = $luisPredictionResourceId"
write-host "customCommandsRegion = $customCommandsRegion"
write-host "customCommandsWebEndpoint = $customCommandsWebEndpoint"
write-host "+++++++++++++++++++++++++++++++++++++++++"

[Console]::ResetColor()
$ErrorActionPreference = "Stop"

if (-not $resourceGroup) {
    $resourceGroup = $resourceName
}

#
# TODO:
# - Change speechAppName to CustomCommandAppName
# - Fix regions - same value for all. Thereofre use a common name

$speechAppName = "$resourceName-commands"
$skillJson = "../$appName/skill/$language/$((Get-Culture).TextInfo.ToTitleCase($appName))Demo.json"

#
# Create the custom speech app
#
write-host "Creating the speech custom command project '$speechAppName'"

$body = @{
    details = @{
        name = $speechAppName
        skillEnabled = "true"
        description  = ""
        baseLanguage =  $language
    }
    slots = @{
        default = @{
            languages = @{
                $language = @{
                    luisResources = @{
                        authoringResourceId = $luisAuthoringResourceId
                        authoringRegion = $luisAuthoringRegion
                        predictionResourceId = $luisPredictionResourceId
                        predictionRegion = $luisAuthoringRegion
                    }
                    dialogModel = $null
                }
            }
        }
    }
}

$jsonBody = (ConvertTo-Json $body -depth 100)

write-host "JSON Body ="
write-host $jsonBody

$armToken = az account get-access-token | ConvertFrom-Json
$armToken = $armToken.accessToken

$headers = @{
    "Content-Type"              = "application/json"
    "Ocp-Apim-Subscription-Key" = $speechResourceKey
    "Arm-Token"                 = $armToken
}

$appId = new-guid
write-host "Generated new project Id $appId"

write-host "Headers ="
write-host (ConvertTo-Json $headers)

$uri = "https://$customCommandsRegion.commands.speech.microsoft.com/v1.0/apps/$appId"

write-host "URI = $uri"

try {
    $response = invoke-restmethod -Method PUT -Uri "https://$customCommandsRegion.commands.speech.microsoft.com/v1.0/apps/$appId" -Body (ConvertTo-Json $body -depth 100) -Header $headers
}
catch {
    write-host "Response ="
    write-host (ConvertTo-Json $response)

    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}

#
# update the dialog model of the app
#

# change the model based on the local json file
write-host "patching the $speechAppName $appName commands model"
$newModel = Get-Content $skillJson | Out-String | ConvertFrom-Json
$newModel.webEndpoints[0].url = $customCommandsWebEndpoint

# send the updated model up to the application
write-host "updating $speechAppName with the new $appName commands model"
try {
    $response = invoke-restmethod -Method PUT -Uri "https://$customCommandsRegion.commands.speech.microsoft.com/v1.0/apps/$appId/slots/default/languages/$language/model" -Body ($newModel | ConvertTo-Json  -depth 100) -Header $headers
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
$response = invoke-webrequest -Method POST -Uri "https://$customCommandsRegion.commands.speech.microsoft.com/v1.0/apps/$appId/slots/default/languages/$language/train?force=true" -Header $headers
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
write-host $OperationLocation
write-host $headers
$response = invoke-restmethod -Method PUT -Uri "$($OperationLocation.replace('/train/','/publish/'))" -Header $headers

write-host "...model is published"

#
# print out the relevant info for the user to put in the application
#

Write-Host
Write-Host "*******************************************************************"
Write-Host "Your Custom Commands demo has been published!"
Write-Host
Write-Host "Customized your client app with the following, in order to connect:"
Write-Host "    CustomCommandsAppId   = $appId"

