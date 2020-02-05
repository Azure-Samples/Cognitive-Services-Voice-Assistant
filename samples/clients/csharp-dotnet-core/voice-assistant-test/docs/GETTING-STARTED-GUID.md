# Getting Started Guide

## Overview
This is a step-by-step guide for first time users of the tool, showing how to write configuration files for the most common end-to-end tests, and how to run the tool. The guide uses Bot-Framework's "Echo Bot" and "Core Bot" as the example bot to be tested.

## Step 1: Clone the repo and build the tool

Per the prerequisite, make sure you have the following installed on your Windows 10 PC:
- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [Git for Windows](https://git-scm.com/downloads)
- [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/)

Clone the repo to some folder on your PC (e.g. "c:\repo" in this example) and build the tool from the command line:

```cmd
cd /d c:\repo
git clone https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant.git
cd Cognitive-Services-Voice-Assistant\samples\clients\csharp-dotnet-core\voice-assistant-test\tool
dotnet build
```

The build will create a self-contained .NET core executable VoiceAssistantTest.exe that you can copy to your working folder, where you will run your tests. It's best not to run the test under your repo folder. Create a test folder (e.g. "c:\test" in this example), copy the executable to this folder and run it without arguments:

```cmd
cd /d c:\test
copy c:\repo\Cognitive-Services-Voice-Assistant\samples\clients\csharp-dotnet-core\voice-assistant-test\tool\XXXX\VoiceAssistnatText.exe
VoiceAssistantText.exe
```
You will get the following error:
```
VoiceAssistantTest Error: 0 : System.ArgumentException: Configuration file is not specified. Please pass a valid configuration file as an argument.
```
This is good. The tool works. You are now ready to author your first application and test configuration files. The application configuration file is needed as the single run-time argument when running the tool. But before we write the test, we need to make sure we have a bot web service hosted to test against.

## Step 2: Deploy the Echo Bot



## Getting Started with Sample

1. > Follow the [Voice-enable your bot using the Speed SDK Tutorial](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) to enable the your bot to use the Direct Line Speech Channel.
2. > Copy the Cognitive Services Speech API Key by clicking on the Azure Speech resource created in the above listed tutorial
3. > [Set up the Configuration file and Input files](###Sample-Configurations-and-Tests)

### Sample Configurations and Tests

Navigate to [docs/examples](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/samples/clients/csharp-dotnet-core/voice-assistant-test/docs/examples) to find Core Bot and Echo Bot folder with sample configurations and tests. Paste the appropriate Bot Speech Key and Region in the Config.json files in each example folder.

Please see [Configuration File Structure](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/sajjadp/VAReadmeUpdateFeb42020/samples/clients/csharp-dotnet-core/voice-assistant-test/README.md#Application-Configuration-file) for reference and modify the sample configurations appropriately

For examples of configuration and test files, please see the templates in [docs/json-templates/](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/samples/clients/csharp-dotnet-core/voice-assistant-test/docs/json-templates)

| Examples  |                                   Echo Bot                                    |                                              Core Bot                                               |
| :-------- | :---------------------------------------------------------------------------: | :-------------------------------------------------------------------------------------------------: |
| Example 1 |                         Greeting upon Bot Connection                          |                              Greeting and Message upon Bot Connection                               |
| Example 2 |           Mutiple tests containing multi-turn dialog Text as Input            | Multiple tests containing multi-turn dialog one with text as input and other with wav file as input |
| Example 3 | Multiple tests containing multi-turn dialogs with text and wav files as input |                                                                                                     |

### Modifying Core Bot Configuration File

1. > cd to docs/examples/corebot/example1
2. > Open CoreBotConfig.json
3. > Populate the InputFolder, and OuputFolder fields with the full path to this folder
4. > Enter the Speech Key and Region associated with your Voice Enabled Core Bot in the SubscriptionKey and Region fields respectively.
5. > [Run Voice Assistant Test](###Run-Voice-Assistant-Test)
6. > Open the folder specified in the OutputFolder to find the OutputFiles generated for the Test.
7. > cd to docs/examples/corebot/example2 and repeat steps 2 - 6

### Modifying Core Bot Test File

After running corebot/example2, take a look at VoiceAssistantTest.log. You will notice that Dialog 0 for SingleDialogTest1.json failed. Now take a look at SingleDialogTest1Ouput/SingleDialogTest1Output.txt, you will see that the text and speak fields in ExpectedResponses for Dialog 0 Turn 2 do not match the text and speak fields in ActualResponses.
To Fix this,
Paste the following for Dialog 0 Turn 2 in SingleDialogTest1.json

```
      {
        "TurnID": 2,
        "Utterance": "Yes",
        "Activity": "",
        "WavFile": "",
        "ExpectedResponses": [
          {
            "type": "message",
            "text": "I have you booked to New York from Seattle on 10th February 2025",
            "speak": "I have you booked to New York from Seattle on 10th February 2025",
            "inputHint": "ignoringInput"
          },
          {
            "type": "message",
            "text": "What else can I do for you?",
            "speak": "What else can I do for you?",
            "inputHint": "expectingInput"
          }
        ]
      }
```

Looking at the VoiceAssistantTest.log, you will see that Dialog 0 has now passed. The tool checks Expected with Actual to determine if a dialog and turn passes or fails.

### Run Voice Assistant Test

1. ```
   Open Command prompt

   cd to Cognitive-Services-Voice-Assistant/samples/clients/csharp-dotnet-core/voice-assistant-test/tool/
   dotnet build -c Release
   cd to bin/Release/netcoreapp3.1/
   dotnet.exe VoiceAssistantTest.dll {Full path of configuration file to run}
   ```

2. In the path specified in the OutputFolder of the configuration file, you will find the VoiceAssistantTest logs, report, and test output for each Test File.

```
Note : If you want to run the application through a Visual Studio debugger add the configuration file path to application arguments.
Click on Solution > Properties > Debug > Enter the configuration file path to application arguments
```
