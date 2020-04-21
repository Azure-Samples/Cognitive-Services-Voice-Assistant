Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $luisName = $(Read-Host -prompt "luisName"),
    [string] $functionName = $(Read-Host -prompt "functionName"),
    [string] $storageName = $(Read-Host -prompt "storageName"),
    [string] $region = $(Read-Host -prompt "region")
)
$ErrorActionPreference = "Stop"

# change the parameters file based on the local json file

$newFile = (Get-Content './azuredeploy.parameters.json') | Out-String | ConvertFrom-Json
$newFile.parameters.resourceName.value = $resourceName
$newFile.parameters.luisName.value = $luisName
$newFile.parameters.functionName.value = $functionName
$newFile.parameters.storageName.value = $storageName
$newFile | ConvertTo-Json -depth 100 | Set-Content './azuredeploy.parameters.json'

Write-Host "Creating resource group"
$output = az group create --name $resourceName --location $region
if( !$output ){
    Write-Error "Failed to create resource group"
    Write-Error "$output"
    exit
}

Write-Host "Deploying azure template at ./azuredeploy.json"
$output = az group deployment create --resource-group $resourceName --template-file ./azuredeploy.json --parameters './azuredeploy.parameters.json' | ConvertFrom-Json

if( !$output ){
    Write-Error "Failed to deploy template"
    Write-Error "$output"
    exit
}