#Requires -Version 6

Param(
    [string] $appName = $(Read-Host -prompt "appName"),
    [string] $language = $(Read-Host -prompt "language"),
    [string] $region = $(Read-Host -prompt "region"),
    [string] $speechResourceKey = $(Read-Host -prompt "speechResourceKey"),
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $luisAuthoringResourceId = $(Read-Host -prompt "luisAuthoringResourceId"),
    [string] $luisPredictionResourceId = $(Read-Host -prmpot "luisPredictionResoureceId"), 
    [string] $customCommandsWebEndpoint = $(Read-Host -prompt "cutomCommandsWebEndpoint")
)

[Console]::ResetColor()
$ErrorActionPreference = "Stop"

#
# Create and provision a new Custom Command application
#

$customCommandsAppName = "$resourceName-commands"
write-host "Creating the speech custom command project '$customCommandsAppName'"
$skillJson = "../$appName/skill/$language/$((Get-Culture).TextInfo.ToTitleCase($appName))Demo.json"

# Load the CC JSON model file
write-host "patching the $customCommandsAppName $appName commands model"
$dialogModel = Get-Content $skillJson | Out-String | ConvertFrom-Json
$dialogModel.webEndpoints[0].url = $customCommandsWebEndpoint

# Define the body of the web API call
$body = @{
    details = @{
        name = $customCommandsAppName
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
                        authoringRegion = $region
                        predictionResourceId = $luisPredictionResourceId
                        predictionRegion = $region
                    }
                    dialogModel = $dialogModel
                }
            }
        }
    }
}

# This ARM token allows the Custom Command service access to your subscription, in order to get the LUIS prediction and authoring keys
$armToken = az account get-access-token | ConvertFrom-Json
$armToken = $armToken.accessToken

# Define the HTTP headers of the web API call
$headers = @{
    "Content-Type"              = "application/json"
    "Ocp-Apim-Subscription-Key" = $speechResourceKey
    "Arm-Token"                 = $armToken
}

$appId = new-guid
write-host "Generated a new project Id $appId"

try {
    $response = invoke-restmethod -Method PUT -Uri "https://$region.commands.speech.microsoft.com/v1.0/apps/$appId" -Body (ConvertTo-Json $body -depth 100) -Header $headers
}
catch {
    # dig into the exception to get the Response details.
    # note that value__ is not a typo.
    Write-Host $_.Exception
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    exit
}

#
# Start the training for the model
#

write-host "Starting the model training"
$response = invoke-webrequest -Method POST -Uri "https://$region.commands.speech.microsoft.com/v1.0/apps/$appId/slots/default/languages/$language/train?force=true" -Header $headers
$OperationLocation = $response.Headers["Operation-Location"]
write-host "training Operation Location: $OperationLocation"

#
# Wait until the training is complete
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
write-host "...training is completed"

#
# Publish the model
#

write-host "Publishing the model"
$response = invoke-restmethod -Method PUT -Uri "$($OperationLocation.replace('/train/','/publish/'))" -Header $headers
write-host "...model is published"

#
# print out the relevant info for the user to put in the application
#

Write-Host
Write-Host "*******************************************************************"
Write-Host
Write-Host " Your Custom Commands demo has been published!"
Write-Host
Write-Host " Customized your client app with the following, in order to connect:"
Write-Host "    CustomCommandsAppId   = $appId"

