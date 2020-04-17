This README is under development and not guaranteed to be accurate or functional.

## Things you will need
* An Azure subscription. [Azure](https://portal.azure.com)
* Powershell 6.0 or greater [Download Powershell](https://github.com/PowerShell/PowerShell/releases)
* .Net Core SDK [Install SDK](https://docs.microsoft.com/en-us/dotnet/core/install/sdk?pivots=os-windows)

## Deploying Azure Resources
Clone the repository and change directory so you are in the root/custom-commands/hospitality/deployment folder.

Call az login to log your powershell into Azure. Run the powershell script. Replace "mynewresource" with the name you would like. Try to make this name unique as it is required to be GLOBALLY unique. It will be used to generate a url. Note: This will use your default Azure subscription if you have more than one.

    ./az login
    ./deployAll.ps1 -resourceName mynewresource -region westus2

wait...

You should be good to go! The script will print out some resources you will need to start using your hospitality deployment.

* Speech resource key
* Custom Commands App ID
* Visualization Endpoint

## What just happened...

There should be a set of azure resources created in your azure subscription. In the Azure portal is should look something like this:

![Resources](../../../docs/images/Resources.png)

The resources were created using an azure template which is stored in the [./azuredeploy.json](./azuredeploy.json) file.</br>
Then the [../storage files](../storage-files) were deployed into the storage resource.</br>
After that the azure function project located in [../skill](../skill) was built using the command line .NET tool and deployed to the Azure function resource.

The Custom Commands application was created from the json file [../skill/hospitalityCustomCommands.json](../skill/hospitalityCustomCommands.json) and deployed to your Azure subscription. You can view that in the [Microsoft Speech portal](https://speech.microsoft.com/).

If you would like to dig deeper into the powershell scripts you will see there are some simple string replacements we do to update the links between the azure function and the html file in the storage account that is ultimatly the web page you see.

