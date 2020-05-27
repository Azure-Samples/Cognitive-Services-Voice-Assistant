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
$output = az role assignment create --role "Storage Blob Data Owner" --assignee $userName --scope $storageResourceId | ConvertFrom-Json
if (!$output) {
    Write-Error "Failed to create role Owner on $storageResource"
    Write-Error $output
    exit
}


$retries = 10
$retrycount = 0
$completed = $false
while (-not $completed) {
    
    if ($retrycount -ge $retries) {
        Write-Error -Message ("Creating container permissions failed the maximum number of {0} times." -f $retrycount) -Category OperationTimeout
        exit
    }
    
    # check if the permissions have taken effect
    $printCount = $retrycount + 1
    Write-Host "Checking if the storage permissions have taken effect. Attempt $printCount, Maximum $retries"
    $output = az role assignment list --scope $storageResourceId | ConvertFrom-Json
    
    if( !$output ) {
        Write-Error "Failed to get role assignment list from Storage resource."
        Write-Error $output
    }
    
    $permissionsSet = $false
    foreach ($item in $output) {
        if($item.roleDefinitionName -eq "Storage Blob Data Owner"){
            $permissionsSet = $true
            $completed = $true
            Write-Host "SUCCCESS! Storage permissions have taken effect."
        }
    }
    
    #if not set sleep and check again
    if($permissionsSet){
        $completed = $true;
    }
    else{
        Write-Host ("Container roles not set yet. Sleep for 30 seconds and try again.")
        Start-Sleep -s 30
        $retrycount++
    }
}

# sometimes the container create can take a bit of time so we will retry the next step a few times.
$retries = 10
$retrycount = 0
$completed = $false
while (-not $completed) {
    
    if ($retrycount -ge $retries) {
        Write-Error -Message ("Creating container command failed the maximum number of {0} times." -f $retrycount) -Category OperationTimeout
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