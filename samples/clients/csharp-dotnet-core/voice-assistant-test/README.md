
# Voice Assistant Test Tool

## Overview

The Voice Assistant Test (VST) tool is a configurable .Net core C# console application for end-to-end regression tests and intent scoring for your Microsoft [Voice Assistant](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/voice-assistants). It uses the [Microsoft.CognitiveServices.Speech.Dialog](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog?view=azure-dotnet) APIs in the [Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) to execute a set of single or multi-turn dialogs against your bot or Custom Commands web application. JSON are authored to specify what the client sends to the bot and the expected bot response, for every turn in the dialog. The tool can then run manually or automated as part of CI/CD pipeline to prevent regressions in your bot. It supports sending an audio stream to the bot, simple text messages or full [Bot-Framework Activities](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md). The tool can also aggregate test results for the purpose of creating an intent (task execution) score.  

## Prerequisites

* A subscription key for the Speech service. See [Try the speech service for free](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started).
* A pre-configured bot created using Bot Framework. See [here for steps on how to create a bot](https://blog.botframework.com/2018/05/07/build-a-microsoft-bot-framework-bot-with-the-bot-builder-sdk-v4/). The bot needs to be registered with [Direct Line Speech](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) channel to send and receive voice. Use your own deployed bot to try out this tool, or one of the many [Bot-Framework samples](https://github.com/Microsoft/BotBuilder-Samples)
* Alternatively, a [Custom Commands](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands) web application instead of a Bot-Framework bot.
* A Windows PC with Windows 10 or later and .NET Core 3.1
* [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/)

Note: The source code should compile and run on any of the [supported .NET Core platforms](https://github.com/dotnet/core/blob/master/release-notes/3.1/3.1-supported-os.md) but the only platform verified so far is Windows.

## Samples List - This will move to another MD file

Get started implementing your own Application using Azure Cognitive Services. To use the samples provided, clone this GitHub repository using Git.

```
git clone https://github.com/Microsoft/repoName.git
cd repoName
```

## Getting Started with Sample - This will move to another MD file


1. > Follow the [Voice-enable your bot using the Speed SDK Tutorial](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) to enable the your bot to use the Direct Line Speech Channel.
2. > Copy the Cognitive Services Speech API Key by clicking on the Azure Speech resource created in the above listed tutorial
3. > Set up the Configuration file and Input files
4. > Copy the path of the Configuration file.
5. > Open Command Prompt, navigate to the location of the executable 
6. > Call the executable with path of Configuration file.

```
Note : If you want to run the application through a Visual Studio debugger add the configuration file path to application arguments. 
Click on Solution > Properties > Debug > Enter the configuration file path to application arguments
```
## Getting started guide

Click here for a step by step introduction to the tool.

## Reference guide

### Definitions

* The application executes a single or multiple __Tests__ when it runs. 
* Each test contains a single or multiple  __Dialogs__. 
* A dialog contains a single or multiple __Turns__. 

The tests are set up by authoring two types of [JSON files](https://tools.ietf.org/html/rfc7159): 

* [__Application Configuration__](#Application-Configuration-JSON-File) JSON file - Configuration settings that apply globally to all dialogs in all tests. This JSON file is the only input argument to the application.
* [__Test Configuration__](#Test-Configuration-JSON-File) JSON file - Settings that are unique to this test, including specifications of all the dialogs and their turns for this test.

The Application Configuration JSON file will list one or more Test Configuration JSON files.

### JSON templates

When creating new tests, you may find it useful to start from these templates and modify them:
* [Example of an Application Configuration file](docs\json-templates\app-config-template.json), containing all possible fields
* [Example of a Test Configuration file](docs\json-templates\test-config-template.json), containing all possible fields

#### Application configuration file

The following are the fields  in Configuration file:

|Field Name     | Type 	     | Required/Optional| Default   | Example	     | Description|
|:--------------|:-----------| :---------------:|:----------|:---------------|:----------|
|FileName       | string	 | required		    |           | Start.json	 | Name of the input file|
|InputFolder    | string     | required         |           |"C:\\\LUAccuracytool\\\SydneyAssistant\\\" | Path that contains all the input files and input WAV files |
|OutputFolder   | string     | required         |           |"C:\\\LUAccuracytool\\\SydneyAssistant\\\" |  Path where the output test files will be generated |
|SubscriptionKey| string     | required         |           |“9814793187f7486787898p35f26e9247”|  Cognitive Services Speech API Key. Should be a GUID without dashes |
|Region         | string     | required         |           |"westus" |  Azure region of your key in the format specified by the "Speech SDK Parameter" |
|IgnoringActivities | Array of JObject	 | Optional	|	          |[{"type": "typing","name": "trace"}, {"name": "setDynamicGrammar"}]| List of activities that are ignored by tool|
|SRLanguage   | string     | Optional             |             |"en-US" |  Speech Recognition Language. It is the source language of your audio.  [Checkout the list of languages supported]:https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support |
|CustomCommandsAppId  | string     | Optional         |             |"80c787bf-806b-402c-9493-79154c08a67d" |  Custom Commands App ID. Should be a GUID. |
|CustomSREndpointId   | string     | Optional         |             | |  Custom SR Endpoint ID. Sets the endpoint ID of a customized speech model that is used for speech recognition |
|CustomVoiceDeploymentIds   | string     | Optional         |             |"07a189pb4-U734-47b2-q89l-56e12g0a71h0" | Custom Voice Deployment ID.|
|AudioDurationMargin   | string     | Optional         |   200          | 100 |  Margin to verify the duration of Bot response TTS audio.|
|AppLogEnabled   | boolean     | Optional         |   false          | true |   A boolean that enables Application Logging.|
|SpeechSDKLogEnabled   | string     | Optional         |   false          | true |   A boolean that enables generating a Speech SDK Logging.|
|Timeout   | int     | Optional         |   5000          | 2000 |  A global timeout that waits for each bot response.|

#### Test configuration file

The following are the fields  in Input File:

|Field Name      | Type 	       | Required/Optional| Default   | Example	         | Description|
|:---------------|:----------------| :---------------:|:----------|:-----------------|:----------|
|DialogId        | string	       | required		  |           |   "0"	         | A unique value that identifies each dialog|
|TurnId          | int             | required         |           |    1             | A unique value that identifies each turn in a specific Dialog. |
|Utterance       | string          | required         |           |“Open Start Menu” | Text that is send up to communicate with the Bot.  |
|Activity        | string          | required         |           |"{\"type\”: \"message\",\"text\":\"Test sending text via activity\"}"|  Input Activity. Activity that is send up to Bot.|
|WavFile         | string          | required         |           |"test1.WAV" | Input WAV file. Audio that is streamed to Bot |
|ExpectedResponses| Array of JObject| required        |           |                   |List of Expected responses from Bot. |
|ExpectedIntents | Array of JObject   | Optional	      |	          |[{"Item1": "NONE","Item2": 1},{"Item1":"L_DEVICECONTROL","Item2": 2}]|List of expected Intents|
|ExpectedSlots| Array of JObject| Optional         |           |                   | List of expected Slots.|
|ExpectedResponseDuration  | int    | Optional        |   2000    | 1500              |  Expected duration of Bot response TTS audio.|
|ExpectedLatency  | string         | Optional         |  Expectedresponse index to check for is defaulted to Zero |"500,1" |This is a combination of expected latency and Index of the expected response from the list of expected responses that we care for calculating latency.|

## Topics

### Ignoring certain bot-reply activities

### Writing tests with keyword spotting

### Ignoring certain bot-reply activities

### Testing bot response duration

### Testing bot-greetings

### Single connection and multipel connection tests

### Skipping tests

### Pausing between dailogs or turns

### Creating new tests ("bootstrapping")

## Running tests in an Azure DevOps pipeline
