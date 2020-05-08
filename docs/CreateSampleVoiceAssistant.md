# Step-by-Step Instructions on Building a Sample Voice Assistant

<p align="center">UNDER DEVELOPMENT</p>

## Step 1 - Select a Custom Command demo

We have three demos for you to select from. Read about what voice commands they support and see the associated visualization. When you selected the one you would like to deploy, continue to Step 2
* [Hospitality demo](../custom-commands/hospitality/README.md)
* [Inventory management demo](../custom-commands/inventory/README.md)
* [Automotive demo](../custom-commands/automotive/README.md)


## Step 2 - Run the Azure deployment script

Run a Powershell script to deploy all the Azure resources needed for this demo, in your own Azure subscription. The script will also create and provision the selected Custom Command project.

[Follow the instructions here](../custom-commands/hospitality/deployment/README.md)

At the end of this step, you will have the following values:
* Azure Cognitive Services Speech subscription key (e.g. ")
* Subscription key region (e.g. "westus")
* Custom Commands application ID
* URL for visualization, which can be viewed in any browser

## Step 3 - Select a keyword (optional)

You can configure a client application to always be listening for a keyword of your choice, and respond to your voice commands after the keyword has been spoken. This is optional, the alternative being pressing a microphone button or key on your keyboard before speaking.

To configure keyword activation, you will need to have a keyword model file, which is a binary file with .table extension. We have a few .table files ready for you to use, or you can create a new one for your own keyword.

[Follow the instructions here](../keyword-models/README.md)

## Step 4 - Select a client application and run the demo

Select one of the [sample client application](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant#sample-client-applications) in this repo. Follow the instructions to download the executable or build it from source code. Configure it by entering the values you have from Step 2 (speech subscirption key, key region & Custom Commands application ID), the optional .table file name from Step 3, in order to connect the client application to your Custom Command service. Run the client application and try out several voice command, while viewing the results in the visualization web page.

If you are developing on windows, we recommend you first use the [Windows Voice Assistant Client](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/clients/csharp-wpf). Executable of the latest stable version can be downloaded from the [Release tab](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/releases) (no need to compile code). And it has a nice GUI to configure connection settings.

If you are in the IoT space, you can create a compelling demo by running the [sample C++ code](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/clients/cpp-console) on a Raspberry Pie device. This includes the option to install the Microsoft Audio Stack to enable echo-cancellation and noise suppression, allowing "barge-in" keyword activation.



## Step ? - barge in

## Step 5 - Give us feedback!






