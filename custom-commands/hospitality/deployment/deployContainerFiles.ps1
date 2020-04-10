$storageName = "mynewresource"
$containerName = "www"

$signedInUser = az ad signed-in-user show | ConvertFrom-Json

$userName = $signedInUser.userPrincipalName

$storageResource = az resource list --name $storageName | ConvertFrom-Json

$storageResourceId = $storageResource.id

az role assignment create --role "Storage Blob Data Owner" --assignee $userName --scope $storageResourceId
    
az storage container create --account-name $storageName --name $containerName --public-access container --auth-mode login
az storage blob upload-batch -d $containerName -s ../storage-files --auth-mode login --account-name $storageName