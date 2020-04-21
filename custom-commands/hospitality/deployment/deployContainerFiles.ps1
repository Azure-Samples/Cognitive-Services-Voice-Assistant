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

#Start-Sleep -s 5

#create the actual container
Write-Host "Creating container ContainerName = $containerName account-name = $storageName" 
az storage container create --account-name $storageName --name $containerName --public-access container --auth-mode login

#Update the demo.html file with the correct urls
$connectionString=az storage account show-connection-string -n $storageName -g $resourceGroup --query connectionString -o tsv

Write-Host "Getting blob url" 
$storageURL = az storage blob url --container-name $containerName --connection-string $connectionString --name $storageName
$storageURL = $storageURL.Trim("`"")
$storageURL = $storageURL.TrimEnd("/$storageName")

Write-Host "Updating demo.html with new blob url" 
$newFile = (Get-Content '../storage-files/demo.html')
$newFile = $newFile | Foreach-Object { $_ -replace "AZURE_STORAGE_URL", $storageURL }
$newFile = $newFile | Foreach-Object { $_ -replace "AZURE_FUNCTION_URL", $functionURL }
$newFile | Set-Content '../storage-files/demo.html'

#sometimes the role assignment can take a bit of time so we will retry the next step a few times.
$retries = 5
$retrycount = 0
$completed = $false
while (-not $completed) {
        #upload the files
        Write-Host "Uploading files to new container" 
        $output = az storage blob upload-batch -d $containerName -s ../storage-files --auth-mode login --account-name $storageName | ConvertFrom-Json
    if ($retrycount -ge $retries) {
        Write-Error ("Container command failed the maximum number of {1} times." -f $retrycount)
        exit
    }
    
    if( !$output ) {
        Write-Host ("Container upload command failed. Retrying.")
        Start-Sleep -s 15
        $retrycount++
    } else {
        Write-Host "Uploading files completed!" 
        $completed = $true
    }
}

#update the Azure function project with the connection string for the storage
Write-Host "Getting storage connection string" 
$storageConnectionString = az storage account show-connection-string --resource-group $resourceGroup --name $storageName | ConvertFrom-Json

Write-Host "Updating RoomDemo.cs with new connection string" 
$newFile = (Get-Content '../skill/VirtualRoomApp/RoomDemo.cs')
$newFile = $newFile | Foreach-Object { $_ -replace "STORAGE_CONNECTION_STRING", $storageConnectionString.connectionString }
$newFile | Set-Content '../skill/VirtualRoomApp/RoomDemo.cs'