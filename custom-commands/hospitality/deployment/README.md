# Deploying a Sample Custom Command Application to Your Azure Subscription

<p align="center">UNDER DEVELOPMENT</p>

## Things you will need
* An Azure account. [sign up for free](https://azure.microsoft.com/free/ai/).
* Powershell 6.0 or greater. [Download Powershell here](https://github.com/PowerShell/PowerShell/releases). 
    * On Windows, download and run an .msi file (e.g. "PowerShell-7.0.0-win-x64.msi")
* Azure CLI. [Insall Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) version 2.4.0 or higher.
* .NET Core SDK 2.1 or higher. [Install SDK here](https://docs.microsoft.com/en-us/dotnet/core/install/sdk?pivots=os-windows).

## What are you deploying?
 
 TODO: Need diagram of CC and Visualization, and a description of the components and how they interact

## Deploying Azure Resources
Clone the repository and change directory so you are in the \<<repo-root\>>\custom-commands\hospitality\deployment folder.

You will need to unrestrict powershell's script execution policy by running the following in an administrator powershell:
```powershell
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```
This will reset the policy once this powershell session has ended.

Call az login to log your powershell into Azure. If you have more than one Azure login credentials, you will be prompted to selected one or them:
```powershell
    az login
```
 Run the following powershell script. 
 * Replace "MyResourceGroupName" with the an Azure Resource Group name of your choice. This name should be no more than 19 characters, alphanumeric only. Make sure an Azure resource group by this name does not already exist in your subscription. This name will also be used to construct names of all the Azure resources and URL that will be associated with this Custom Commands application and visualization. Some of these names need to be globally unique, so the script will append a random number to the name you selected
 * Replace "westus" with an Azure region near you, form the list of [Voice Assistant supported regions](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/regions#voice-assistants). Read more about Azure Regions [here](https://azure.microsoft.com/en-us/global-infrastructure/regions/). Note that if you are using a free trial Azure subscription, you are limited to Azure regions westus or northeurope
```powershell
    ./deployAll.ps1 -resourceName MyResourceGroupName -region westus
```
It will take a few minutes for the scripts to run. On a successful completion, you should see a message at the end similar to the following, with all the information you need to configure one of the selected Voice Assistant client samples, and a URL to visualize the results of your voice commands:
```console
***********************
Custom commands has been published.
Update these parameters in your client to use the Custom Commands Application
    CustomCommandsId = ########-####-####-####-############
    SpeechSubscriptionKey = ################################
    Speech Region = westus
***********************
To view your visualization go to this link.
    Visualization Endpoint = https://#########.blob.core.windows.net/www/demo.html?room=test1
***********************
```
If you now look at your [Azure Resource groups](https://portal.azure.com/#blade/HubsExtension/BrowseResourceGroups) in the Azure portal, you will see a new resource group has been created with the name you selected, with 6 resources in it, similar to what you see here:
<!-- Save this for reference, we may want to go back to a table and add descriptions...
| Name  | Type          |
| ------- | ---------------- |
| MyResourceGroupName-###  | Cognitive Services |
| MyResourceGroupName-### | App Service |
| MyResourceGroupName-###-authoringkey | Cognitive Services |
| MyResourceGroupName-serverfarm | App Service Plan |
| MyResourceGroupName-speech | Cognitive Services |
| MyResourceGroupName### | Storage account
-->
<p align="center">
<img src="images/resource-group.png"/>
</a>
</p>
If you see errors in the script, refer to the Troubleshooting section below. Before running the script again due to errors, please delete the Azure Resource if you plan to run with the same Azure Resource name:
```powershell
az group delete --name MyResourceGroupName
```

## Troubleshooting

* *"The subscription '########-####-####-####-############' is disabled and therefore marked as read only. You cannot perform any write actions on this subscription until it is re-enabled.
Write-Error: Failed to create resource group"* - This may be because your free trial period for Azure subscription has ended. Upgrade your subscription.

## Deploying Azure Resources - Deeper Dive

There should be a set of azure resources created in your azure subscription. In the Azure portal is should look something like this:

![Resources](../../../docs/images/Resources.png)

The resources were created using an azure template which is stored in the [./azuredeploy.json](./azuredeploy.json) file.</br>
Then the [../storage files](../storage-files) were deployed into the storage resource.</br>
After that the azure function project located in [../skill](../skill) was built using the command line .NET tool and deployed to the Azure function resource.

The Custom Commands application was created from the json file [../skill/hospitalityCustomCommands.json](../skill/hospitalityCustomCommands.json) and deployed to your Azure subscription. You can view that in the [Microsoft Speech portal](https://speech.microsoft.com/).

If you would like to dig deeper into the powershell scripts you will see there are some simple string replacements we do to update the links between the azure function and the html file in the storage account that is ultimatly the web page you see.

