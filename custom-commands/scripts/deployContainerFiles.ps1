Param(
    [string] $appName = $(Read-Host -prompt "appName"),
    [string] $resourceName = $(Read-Host -prompt "resourceName"),
    [string] $storageName = $(Read-Host -prompt "storageName"),
    [string] $functionURL = $(Read-Host -prompt "functionURL")
)

[Console]::ResetColor()
$ErrorActionPreference = "Stop"
$resourceGroup = $resourceName
$containerName = "www"

# get the current signed in user
$signedInUser = az ad signed-in-user show | ConvertFrom-Json
$userName = $signedInUser.userPrincipalName

# get the storage resource id
$storageResource = az resource list --name $storageName | ConvertFrom-Json
$storageResourceId = $storageResource.id

# assign proper roles for this user
Write-Host "Creating Blob Owner role assignee = $userName storageResourceId = $storageResourceId" 
# az role assignment create --role "Storage Blob Data Owner" --assignee $userName --scope $storageResourceId
$output = az role assignment create --role "Storage Blob Data Reader" --assignee $userName --scope $storageResourceId | ConvertFrom-Json
if (!$output) {
    Write-Error "Failed to create role Storage Blob Data Reader on $storageResource"
    Write-Error $output
    exit
}

$output = az role assignment create --role "Storage Blob Data Contributor" --assignee $userName --scope $storageResourceId | ConvertFrom-Json
if (!$output) {
    Write-Error "Failed to create role Storage Blob Data Contributor on $storageResource"
    Write-Error $output
    exit
}

# sometimes the container create can take a bit of time so we will retry the next step a few times.
$retries = 5
$retrycount = 0
$completed = $false
while (-not $completed) {
    
    if ($retrycount -ge $retries) {
        Write-Error ("Creating container command failed the maximum number of {0} times." -f $retrycount)
        Write-Error "$output"
        exit
    }
    
    # create the actual container
    Write-Host "Creating container ContainerName = $containerName account-name = $storageName" 
    $output = az storage container create --account-name $storageName --name $containerName --public-access container --auth-mode login | ConvertFrom-Json
    
    if ( !$output ) {
        Write-Host ("Creating container command failed. Retrying in 30 seconds. Sometimes it takes a while for the creation of the storage to take effect.")
        Start-Sleep -s 30
        $retrycount++
    }
    else {
        Write-Host "Container created!" 
        $completed = $true
    }
}

# update ConnectionURLS.json with the correct function url
Write-Host "Updating AZURE_FUNCTION_URL in ConnectionURLS.json with $functionURL"
$newFile = (Get-Content “../$appName/visualization/ConnectionURLS.json”) | Out-String | ConvertFrom-Json
$newFile.AZURE_FUNCTION_URL = $functionURL
$newFile | ConvertTo-Json -depth 100 | Set-Content “../$appName/visualization/ConnectionURLS.json”

# sometimes the role assignment can take a bit of time so we will retry the next step a few times.
$retries = 10
$retrycount = 0
$completed = $false
while (-not $completed) {
    #check for max retries
    if ($retrycount -ge $retries) {
        Write-Error -Message ("Container upload command failed the maximum number of {0} times." -f $retrycount) -Category OperationTimeout
        exit
    }
    
    # upload the files
    Write-Host "Uploading files to new container" 
    $output = az storage blob upload-batch -d $containerName -s ../$appName/visualization --auth-mode login --account-name $storageName | ConvertFrom-Json
    
    if (!$output) {
        Write-Host ("Container upload command failed. Retrying in 30 seconds. Sometimes it takes a while for the permissions to take effect.")
        Start-Sleep -s 30
        $retrycount++
    }
    else {
        Write-Host "Uploading files completed!" 
        $completed = $true
    }
}

# update the Azure function project with the connection string for the storage
Write-Host "Getting storage connection string"
$storageConnectionString = az storage account show-connection-string --resource-group $resourceGroup --name $storageName | ConvertFrom-Json

Write-Host "Updating Connections.json with new connection string"
$titleAppName = (Get-Culture).TextInfo.ToTitleCase($appName)
$newFile = (Get-Content "../$appName/azure-function/$($titleAppName)App/Connections.json") | Out-String | ConvertFrom-Json
$newFile.AZURE_STORAGE_URL = $storageConnectionString.connectionString
$newFile | ConvertTo-Json -depth 100 | Set-Content "../$appName/azure-function/$($titleAppName)App/Connections.json"