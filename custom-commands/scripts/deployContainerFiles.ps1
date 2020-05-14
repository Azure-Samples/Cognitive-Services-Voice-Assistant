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

# create the actual container
Write-Host "Creating container ContainerName = $containerName account-name = $storageName" 
az storage container create --account-name $storageName --name $containerName --public-access container --auth-mode login

# update the demo.html file with the correct urls
$connectionString = az storage account show-connection-string -n $storageName -g $resourceGroup --query connectionString -o tsv

Write-Host "Getting blob url"
$storageURL = az storage blob url --container-name $containerName --connection-string $connectionString --name $storageName
$storageURL = $storageURL.Trim("`"")
$storageURL = $storageURL.TrimEnd("/$storageName")

Write-Host "Updating $appName.html with new blob url - $storageURL. Function - $functionURL"
$demoHtmlFile = "../$appName/visualization/$appName.html"
$newFile = (Get-Content $demoHtmlFile)
$newFile = $newFile | Foreach-Object { $_ -replace "AZURE_STORAGE_URL", $storageURL }
$newFile = $newFile | Foreach-Object { $_ -replace "AZURE_FUNCTION_URL", $functionURL }
$newFile | Set-Content $demoHtmlFile

# sometimes the role assignment can take a bit of time so we will retry the next step a few times.
$retries = 5
$retrycount = 0
$completed = $false
while (-not $completed) {
    # upload the files
    Write-Host "Uploading files to new container" 
    $output = az storage blob upload-batch -d $containerName -s ../$appName/visualization --auth-mode login --account-name $storageName | ConvertFrom-Json
    if ($retrycount -ge $retries) {
        Write-Error ("Container command failed the maximum number of {1} times." -f $retrycount)
        Write-Error "$output"
        exit
    }
    
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

$titleAppName = (Get-Culture).TextInfo.ToTitleCase($appName)
Write-Host "Updating ($titleAppName)Demo.cs with new connection string"
$demoCSFile = "../$appName/azure-function/Virtual$($titleAppName)App/$($titleAppName)Demo.cs"
$newFile = (Get-Content $demoCSFile)
$newFile = $newFile | Foreach-Object { $_ -replace "STORAGE_CONNECTION_STRING", $storageConnectionString.connectionString }
$newFile | Set-Content $demoCSFile