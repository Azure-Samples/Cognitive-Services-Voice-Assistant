[Welcome to Voice Assistant Test Tool](githublink "Enter Github Link to Repo")
===

Overview
===
This branch contains code for Voice Assistant Test Tool.
This sample should be used as a guiding tool for developers to implement their own solutions using the Speech SDK or their own Speech SDK.<br>
 If you are new to Azure Cognitive Services visit [Getting Started with Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/ "Azure Cognitive Services").

Prerequisites
===
* A subscription key for the Speech service. See [Try the speech service for free](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started).
* A pre-configured bot created using Bot Framework version 4.2 or above. See [here for steps on how to create a bot](https://blog.botframework.com/2018/05/07/build-a-microsoft-bot-framework-bot-with-the-bot-builder-sdk-v4/). The bot would need to subscribe to [Direct Line Speech](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) to send and receive voice inputs. 
* A Windows PC with Windows 10 or later
* [Microsoft Visual Studio 2017](https://visualstudio.microsoft.com/), Community Edition or higher.

Samples List
===
Get started implementing your own Application using Azure Cognitive Services. To use the samples provided, clone this GitHub repository using Git.

```
git clone https://github.com/Microsoft/repoName.git
cd repoName
```

Getting Started with Sample
===


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

# Application Configuration file

**The following are the fields  in Configuration file**

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


# Input File

**The following are the fields  in Input File**

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