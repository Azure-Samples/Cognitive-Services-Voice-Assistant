Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $region = $(Read-Host -prompt "region")
)

# change the parameters file based on the local json file

$newFile = (Get-Content './azuredeploy.parameters.json') | Out-String | ConvertFrom-Json
$newFile.parameters.resourceName.value = $resourceName
$newFile | ConvertTo-Json -depth 100 | Set-Content './azuredeploy.parameters.json'


az group create --name $resourceName --location $region

az group deployment create --resource-group $resourceName --template-file ./azuredeploy.json --parameters './azuredeploy.parameters.json'