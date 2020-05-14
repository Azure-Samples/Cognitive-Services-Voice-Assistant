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
#az role assignment create --role "Storage Blob Data Owner" --assignee $userName --scope $storageResourceId
$output = az role assignment create --role "Storage Blob Data Reader" --assignee $userName --scope $storageResourceId | ConvertFrom-Json

if( !$output ){
    Write-Error "Failed to create role Storage Blob Data Reader on $storageResource"
    Write-Error $output
    exit
}

$output = az role assignment create --role "Storage Blob Data Contributor" --assignee $userName --scope $storageResourceId | ConvertFrom-Json

if( !$output ){
    Write-Error "Failed to create role Storage Blob Data Contributor on $storageResource"
    Write-Error $output
    exit
}

#sometimes the container create can take a bit of time so we will retry the next step a few times.
$retries = 5
$retrycount = 0
$completed = $false
while (-not $completed) {

    #create the actual container
    Write-Host "Creating container ContainerName = $containerName account-name = $storageName" 
    $output = az storage container create --account-name $storageName --name $containerName --public-access container --auth-mode login | ConvertFrom-Json

    if ($retrycount -ge $retries) {
        Write-Error ("Creating container command failed the maximum number of {1} times." -f $retrycount)
        Write-Error "$output"
        exit
    }
    
    if( !$output ) {
        Write-Host ("Creating container command failed. Retrying in 30 seconds. Sometimes it takes a while for the creation of the storage to take effect.")
        Start-Sleep -s 30
        $retrycount++
    } else {
        Write-Host "Container created!" 
        $completed = $true
    }
}


#Update the demo.html file with the correct urls
$connectionString=az storage account show-connection-string -n $storageName -g $resourceGroup --query connectionString -o tsv

Write-Host "Getting blob url" 
$storageURL = az storage blob url --container-name $containerName --connection-string $connectionString --name $storageName
$storageURL = $storageURL.Trim("`"")
$storageURL = $storageURL.TrimEnd("/$storageName")

Write-Host "Updating demo.html with new blob url - $storageURL. Function - $functionURL" 
$newFile = (Get-Content '../storage-files/ConnectionURLS.json') | Out-String | ConvertFrom-Json
$newFile.AZURE_FUNCTION_URL = $functionURL
$newFile | ConvertTo-Json -depth 100 | Set-Content '../storage-files/ConnectionURLS.json'

#sometimes the role assignment can take a bit of time so we will retry the next step a few times.
$retries = 5
$retrycount = 0
$completed = $false
while (-not $completed) {
        #upload the files
        Write-Host "Uploading files to new container" 
        $output = az storage blob upload-batch -d $containerName -s ../storage-files --auth-mode login --account-name $storageName | ConvertFrom-Json
    if ($retrycount -ge $retries) {
        Write-Error ("Container upload command failed the maximum number of {1} times." -f $retrycount)
        Write-Error "$output"
        exit
    }
    
    if( !$output ) {
        Write-Host ("Container upload command failed. Retrying in 30 seconds. Sometimes it takes a while for the permissions to take effect.")
        Start-Sleep -s 30
        $retrycount++
    } else {
        Write-Host "Uploading files completed!" 
        $completed = $true
    }
}

#update the Azure function project with the connection string for the storage
Write-Host "Getting storage connection string" 
$storageConnectionString = az storage account show-connection-string --resource-group $resourceGroup --name $storageName | ConvertFrom-Json

Write-Host "Updating Connections.json with new connection string" 
$newFile = (Get-Content '../azure-function/VirtualRoomApp/Connections.json') | Out-String | ConvertFrom-Json
$newFile.AZURE_STORAGE_URL = $storageConnectionString.connectionString
$newFile | ConvertTo-Json -depth 100 | Set-Content '../azure-function/VirtualRoomApp/Connections.json'