Param(
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $storageName = $(Read-Host -prompt "storageName"),
    [string] $functionURL = $(Read-Host -prompt "functionURL")
)
Write-Host "Deploy Container Files called`n resourceName = $resourceName`n storageName = $storageName`nfunctionURL = $functionURL"

$ErrorActionPreference = "Stop"
$resourceGroup = $resourceName
$containerName = "www"

# get the current signed in user
$signedInUser = az ad signed-in-user show | ConvertFrom-Json

$userName = $signedInUser.userPrincipalName

#get the storage resource id
$storageResource = az resource list --name $storageName | ConvertFrom-Json

$storageResourceId = $storageResource.id

#assign proper roles for this user
Write-Host "Creating Blob Owner role assignee = $userName storageResourceId = $storageResourceId" 
az role assignment create --role "Storage Blob Data Owner" --assignee $userName --scope $storageResourceId
    
#create the actual container
Write-Host "Creating container ContainerName = $containerName account-name = $storageName" 
az storage container create --account-name $storageName --name $containerName --public-access container --auth-mode login

#Update the demo.html file with the correct urls
$connectionString=az storage account show-connection-string -n $storageName -g $resourceGroup --query connectionString -o tsv

$storageURL = az storage blob url --container-name $containerName --connection-string $connectionString --name $storageName
$storageURL = $storageURL.Trim("`"")
$storageURL = $storageURL.TrimEnd("/$storageName")


$newFile = (Get-Content '../storage-files/demo.html')
$newFile = $newFile | Foreach-Object { $_ -replace "AZURE_STORAGE_URL", $storageURL }
$newFile = $newFile | Foreach-Object { $_ -replace "AZURE_FUNCTION_URL", $functionURL }
$newFile | Set-Content '../storage-files/demo.html'

#upload the files
az storage blob upload-batch -d $containerName -s ../storage-files --auth-mode login --account-name $storageName

#update the Azure function project with the connection string for the storage
$storageConnectionString = az storage account show-connection-string --resource-group $resourceGroup --name $storageName | ConvertFrom-Json

$newFile = (Get-Content '../skill/VirtualRoomApp/RoomDemo.cs')
$newFile = $newFile | Foreach-Object { $_ -replace "STORAGE_CONNECTION_STRING", $storageConnectionString.connectionString }
$newFile | Set-Content '../skill/VirtualRoomApp/RoomDemo.cs'
