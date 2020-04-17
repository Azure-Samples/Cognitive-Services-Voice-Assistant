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


az group create --name $resourceName --location $region

az group deployment create --resource-group $resourceName --template-file ./azuredeploy.json --parameters './azuredeploy.parameters.json'