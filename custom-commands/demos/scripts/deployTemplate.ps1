Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $luisPredictionName = $(Read-Host -prompt "luisPrediction"),
    [string] $luisAuthoringName = $(Read-Host -prompt "luisAuthoring"),
    [string] $functionName = $(Read-Host -prompt "functionName"),
    [string] $storageName = $(Read-Host -prompt "storageName"),
    [string] $region = $(Read-Host -prompt "region")
)

[Console]::ResetColor()
$ErrorActionPreference = "Stop"

# create a temporary parameters file based on the local json file
$tempParametersFile = './temp.azuredeploy.parameters.json'
$newContent = (Get-Content './azuredeploy.parameters.json') | Out-String | ConvertFrom-Json
$newContent.parameters.resourceName.value = $resourceName
$newContent.parameters.luisPredictionName.value = $luisPredictionName
$newContent.parameters.luisAuthoringName.value = $luisAuthoringName
$newContent.parameters.functionName.value = $functionName
$newContent.parameters.storageName.value = $storageName
$newContent | ConvertTo-Json -depth 100 | Set-Content $tempParametersFile

Write-Host "Creating resource group"
$output = az group create --name $resourceName --location $region | ConvertFrom-Json
if (!$output) {
    Write-Error "Failed to create resource group"
    Write-Error "$output"
    Remove-Item $tempParametersFile
    exit
}

Write-Host "Deploying azure template at ./azuredeploy.json"
$output = az deployment group create --resource-group $resourceName --template-file ./azuredeploy.json --parameters $tempParametersFile | ConvertFrom-Json
Remove-Item $tempParametersFile

if (!$output) {
    Write-Error "Failed to deploy template"
    Write-Error "$output"
    exit
}