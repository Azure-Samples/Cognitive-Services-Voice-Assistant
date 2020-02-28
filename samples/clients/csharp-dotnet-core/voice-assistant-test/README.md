# Voice Assistant Test Tool

## Overview

The Voice Assistant Test (VST) tool is a configurable .NET core C# console application for end-to-end functional regression tests for your Microsoft [Voice Assistant](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/voice-assistants). It uses the [Microsoft.CognitiveServices.Speech.Dialog](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog?view=azure-dotnet) APIs in the [Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) to execute a set of single or multi-turn dialogs against your bot or Custom Commands web application. JSON files are authored to specify what the client sends to the bot and the expected bot response, for every turn in the dialog. The tool can run manually as a console command or automated as part of Azure DevOps CI/CD pipeline to prevent regressions in your bot.

Voice Assistant Test supports the following:

- Any Bot-Framework bot or Custom Commands web application
- Sending a text message, full [Bot-Framework Activity](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md) or audio from a WAV file up to Direct Line Speech channel
- Specifying the expected bot-reply activities
- Ability to filter out (ignore) bot-reply activities as needed
- Verifying the duration of the Text-to-Speech (TTS) audio response from the bot
- Verifying a bot greeting (automatic Activities sent from the bot after initial connection)
- Measuring the duration it took for the bot to reply
- Keyword activation on the input audio

## Prerequisites

- A subscription key for the Speech service. See [Try the speech service for free](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started).
- A bot built using [Microsoft Bot Framework](https://blog.botframework.com/2018/05/07/build-a-microsoft-bot-framework-bot-with-the-bot-builder-sdk-v4/) and hosted on Azure or other cloud service. The bot needs to be voice-enabled and registered with [Direct Line Speech](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) channel to send and receive audio. [See this tutorial](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) to get your started. Use your own deployed bot to try out this tool, or one of the many [Bot-Framework samples](https://github.com/Microsoft/BotBuilder-Samples). 
- Alternatively, a [Custom Commands](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands) web application hosted on Azure on your behalf, instead of a Bot-Framework bot.
- A Windows PC with Windows 10 or later and [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [Git for Windows](https://git-scm.com/downloads)
- [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/)

Note: The source code should compile and run on any of the [supported .NET Core platforms](https://github.com/dotnet/core/blob/master/release-notes/3.1/3.1-supported-os.md) but the only platform verified so far is Windows.

## Getting started guide

First time users, please follow the [Getting Started Guide](docs/GETTING-STARTED-GUIDE.md) for a step by step introduction on how to write configuration files and run tests.

## Reference guide

### Definitions

- The application executes a single or multiple **Tests** when it runs.
- Each test contains a single or multiple **Dialogs**.
- A dialog contains a single or multiple **Turns**.

The tests are set up by authoring two types of [JSON files](https://tools.ietf.org/html/rfc7159):

- [**Application Configuration**](#application-configuration-file) JSON file - Configuration settings that apply globally to all dialogs in all tests. This JSON file is the only input argument to the application.
- [**Test Configuration**](#test-configuration-file) JSON file - Settings that are unique to this test, including specifications of all the dialogs and their turns for this test.

The Application Configuration file lists one or more Test Configuration files.

### JSON templates

When creating new tests, you may find it useful to start from these templates and modify them, as they contain all the supported JSON fields. Fields that are optional are present and set to their default value. You can delete optional fields if they are not needed.

- [Example of an Application Configuration file](docs/json-templates/app-config-template.json)
- [Example of a Test Configuration file](docs/json-templates/test-config-template.json)

### Application configuration file

The following are the fields supported by the Application Configuration file. Each field is specified using the following format:

#### FieldName
`field type (string, integer, array of integers,) | optional or required field |  default value (for optional fields only) | example field value`. Followed by some notes on how to use this field.

Here is the full list:

#### InputFolder
`string | optional | empty string | "C:\\Tests\\TestInputFolder\\"`. Full or relative path to the folder that contains all the input JSON test files and WAV files. You will likely want the string to end with "\\\\" since input file names will be appended to this path.  

#### OutputFolder
`string | optional | empty string | "C:\\Tests\\TestOutputFolder\\"`. Full or relative path to the folder where output files will be written. The folder will be created if it does not exist. You will likely want the string to end with "\\\\" since output file names will be appended to this path.        |

#### SubscriptionKey
`string | required | “01234567890abcdef01234567890abcdef"`. Cognitive Services Speech API Key. Should be a GUID without dashes.

#### Region
`string | required | "westus"`. Azure region associated with your [SubscriptionKey](#subscriptionkey).

#### SRLanguage
`string | optional | "en-US" | "es-MX"`. Speech Recognition Language. It is the source language of your audio. Must be one of the Locale values mentioned in this [Speech-to-text table](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support).

#### CustomCommandsAppId
`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom Commands App ID. Should be a GUID with dashes.

#### CustomSREndpointId
`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom SR Endpoint ID. Should be a GUID with dashes. Sets the endpoint ID of a customized speech model that is used for speech recognition.

#### CustomVoiceDeploymentIds
`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom Voice Deployment ID. Should be a GUID with dashes.

 #### TTSAudioDurationMargin
 `int | optional | 200 | 100`. Margin to verify the duration of bot-response TTS audio. Units are msec. The test will succeed if the actual TTS audio duration is within TTSAudioDurationMargin msec of the value specified by [ExpectedTTSAudioResponseDuration](#expectedttsaudioresponseduration).  

#### AppLogEnabled
`bool | optional | true | false`. When true, application (console) logs will also be written to a text file named VoiceAssistantTest.

#### SpeechSDKLogEnabled
`bool | optional | false | true`. When true Speech SDK logs will be written to a text file, with a name that incorporates the date and time the log was created: SpeechSDKLog-yyyy-MM-dd-HH-mm-ss.txt. Note that these logs are extremely verbose. Enable them only per request from Microsoft to assist Microsoft in investigating a report issue.
                                                                 |
#### BotGreeting
`bool | optional | false | true`. A boolean which defines if your bot or custom command application has an automatic greeting upon connection. This implies that the first turn of the first dialog after connection should verify the bot response activity without providing any client application input in the form of WAV file, text or activity. For an example, see the [getting started guide](#getting-started-guide)

#### Timeout  
`int | optional | 5000 | 1234`. A timeout in msec to wait for all bot responses in each turn. If by this time the bot has not sent the expected number of activities, the test will fail.

 #### KeywordRecognitionModel
 `string | optional | null | "C:\\Test\\test.table"`. A full-path name of the keyword model file. For more info, see the section [Keyword Activation Tests](#keyword-activation-tests).

<font color="red">TODO: Make the above relative path to InputFolder?</font>

#### SetPropertyId
`JSON string | optional | null | [{12345, "PropertyValue"}]`. A JSON string that is an array of pairs of integer and string values, used for custom settings of the client Speech SDK. Each pair results in a call to [DialogServiceConfig.SetProperty(PropertyId, string)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetProperty_Microsoft_CognitiveServices_Speech_PropertyId_System_String_). For more detail, see the section [Custom Settings](#custom-settings)

#### SetPropertyString 
`JSON string | optional | null | [{"PropertyKey", "PropertyValue"}]`. A JSON string that is an array of pairs of two string values, used for custom settings of the client Speech SDK. Each pair results in a call to [DialogServiceConfig.SetProperty(string, string)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetProperty_System_String_System_String_). For more detail, see the section [Custom Settings](#custom-settings)

#### SetServiceProperty
`JSON string | optional | null | [{"PropertyKey", "PropertyValue"}]`. A JSON string that is an array of pairs of two string values, used for custom settings of the Speech Service. Each pair results in a call to [DialogServiceConfig.SetServiceProperty(String, String, ServicePropertyChannel)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setserviceproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetServiceProperty_System_String_System_String_Microsoft_CognitiveServices_Speech_ServicePropertyChannel_), where ServicePropertyChannel is set to ServicePropertyChannel.UriQueryParameter. For more detail, see the section [Custom Settings](#custom-settings)

#### Tests
`JSON string | required | [{"FileName":"MyTestFile.json", "SingleConnection": true}]`. An array of JSON objects, each related to a single test configuration. Each of these JSON objects includes:

#### FileName
`string | required | "Test1\\MyTestFile.json"`. The test configuration file name. It can be a file name without path or with a relative path. See the section [Test configuration file](#test-configuration-file) for details on the JSON format of this file.

#### IgnoreActivities
`JSON string | optional | null | [{"type": "typing","name": "trace"}, {"name": "QnAMaker"}]`. List of bot-response activities that are ignored by tool. For more information see the section [Ignoring certain bot response activities](#ignoring-certain-bot-reply-activities)

#### SingleConnection
`bool | optional | false | true`. If true, connection with the bot (or custom command application) will be re-established before each dialog test. If false, the connection will be established before the first dialog in the test is run, and it will be kep open while all dialog tests in the file are run.

#### Skip
`bool | optional | false | true`. If true, the test file will be skipped while executing tests. This is useful when the application configuration file specifies multiple test files, but you only want to run one (or a few) of them. Use Skip to temporary disable tests.

#### Test configuration file

The following are the fields supported by the Test Configuration file:

| Field Name                       | Type             | Required/Optional | Default                                                  | Example                                                               | Description                                                                                                                                            |
| :------------------------------- | :--------------- | :---------------: | :------------------------------------------------------- | :-------------------------------------------------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------- |
| DialogId                         | string           |     required      |                                                          | "0"                                                                   | A unique value that identifies each dialog                                                                                                             |
| Description                      | string           |     Optional      |                                                          | "Testing a Dialog"                                                    | Describes the what the test does                                                                                                                       |
| Skip                             | boolean          |     Optional      | false                                                    | true                                                                  | Boolean which defines whether a Dialog is to be skipped or not                                                                                         |
| Sleep                            | int              |     Optional      | 0                                                        | 10                                                                    |                                                                                                                                                        |
| TurnId                           | int              |     required      |                                                          | 1                                                                     | A unique value that identifies each turn in a specific Dialog.                                                                                         |
| Utterance                        | string           |     required      |                                                          | “Open Start Menu”                                                     | Text that is send up to communicate with the Bot.                                                                                                      |
| Activity                         | string           |     required      |                                                          | "{\"type\”: \"message\",\"text\":\"Test sending text via activity\"}" | Input Activity. Activity that is send up to Bot.                                                                                                       |
| WavFile                          | string           |     required      |                                                          | "test1.WAV"                                                           | Input WAV file. Audio that is streamed to Bot                                                                                                          |
| Keyword                          | boolean          |     Optional      | false                                                    | true                                                                  | Boolean which defines if input WAV file has a keyword or not. [ For more info click on this link](#writing-tests-with-keyword-spotting)                |
| ExpectedResponses                | Array of JObject |     required      |                                                          |                                                                       | List of Expected responses from Bot.                                                                                                                   |                                   |
| ExpectedTTSAudioResponseDuration | int              |     Optional      | 2000                                                     | 1500                                                                  | Expected duration of Bot response TTS audio. [ For more info click on this link](#testing-bot-response-tts-audio-duration)                             |
| ExpectedResponseLatency          | string           |     Optional      | Expectedresponse index to check for is defaulted to Zero | "500,1"                                                               | This is a combination of expected latency and Index of the expected response from the list of expected responses that we care for calculating latency. |

## Topics

#### Keyword Activation Tests

To test a Bot with an audio input with a keyword, populate "KeywordRecognitionModel" in Application Configuration file with the full path of the table file and set the boolean "Keyword" in the test configuration file to "true" if the WAV file used for testing has a keyword.

The keyword that has been recognized will be populated in the output file.

A dialog can only have a Keyword in Turn 0.

#### Ignoring certain bot-reply activities

Tool waits for certain amount of time or until it hits timeout and captures all the Bot-reply activities received for each turn. This time is equal to the length of the ExpectedResponses array that is set in the test configuration file. Tool perfoms validations on these captured Bot-reply activities to check if the turn is passed or failed.

In order to ignore any one of the Bot-reply activities received for each turn by the tool, "IgnoreActivities" field is to be set in the Application Configuration file.
"IgnoreActivities" is an Array that holds the list of activities that are ignored by the tool. The amount of time for which tool waits to capture the Bot-reply activities does not include the Bot-reply activities that were marked ignore.

Example : [{"type": "typing","name": "trace"}, {"name": "QnAMaker"}]

In the above example tool ignores all activities which are of type : "typing"and name : "trace" and all activities which have "name" : "QnAMaker".

#### Testing bot response TTS Audio duration

A Bot-reply activity with speak field populated, will have a TTS audio.
Tool stores this TTS audio in a WAV file. The duration of TTS audio is calculated and is verified if it falls within the range of ExpectedTTSAudioResponseDuration +/- AudioDurationMargin.
if the test is passed TTSAudioResponseDurationMatch is set to true otherwise false.

#### Testing bot-greetings

Bot-Greeting - It is an automatic Bot response that happens when client connect to the Bot with no input from the client.
Application Configuration file holds a field called "Bot Greeting" that should be set to true when a Bot has a greeting.

For testing the Bot Greeting when,
-SingleConnection = "true", test configuration file should include a Dialog 0, Turn 0 entry with no input fields(Utterance, Activity and WavFile) speciied
-SingleConnection = "false",test configuration file should include a Turn 0 entry on every Dialog with no input fields(Utterance, Activity and WavFile) specified


#### Skipping tests

Tests can be skipped either at the Test Configuration file level or at the Dialog level.

In order to skip a file from the test suite, set the skip field for that file to true in the Application Configuration file.

Example:

```

"InputFiles":[
  {
    "FileName": "test-config-template1.json",
    "SingleConnection": true,
    "Skip": true
    "IgnoreActivities": []
  },
  {
    "FileName": "test-config-template2.json",
    "SingleConnection": true,
    "Skip": true
    "IgnoreActivities": []
  }
]

```

In the above example test configuration file "test-config-template1.json" will be skipped from testing.

In order to skip a Dialog from testing, set the skip field to true for that Dialog in the test configuration file

Example:

```

[
  {
    "DialogID": 0,
    "Description": "Dialog - 0",
    "Skip": true,
    "Turns": [
      {
        "TurnID": 0,
        "Sleep": 10,
        "Utterance": "Testing Dialog 0",
        "Activity": "",
        "WavFile": "",
        "ExpectedResponses": [
          {
            "text": "Testing turn 0."
          }
        ],
      },
    ]
  },
  {
    "DialogID": 1,
    "Description": "Dialog - 1",
    "Skip": false,
    "Turns": [
      {
        "TurnID": 0,
        "Sleep": 0,
        "Utterance": "Testing Dialog 1",
        "Activity": "",
        "WavFile": "",
        "ExpectedResponses": [
          {
            "text": "testing turn 1"
          }
        ],
      }
    ]
  }
]

```

In the above example Dialog 0 will be skipped from testing.

#### Pausing between dailogs or turns

#### Creating new tests ("bootstrapping")

While creating your own Test configuration file,bootstrapping mode is useful in order to capture all the bot responses

In order to set a turn to bootstrapping mode,in Test Configuration file set the ExpectedResponses field to either null or empty or dont specify it in the

In this mode,tool captures all the bot responses ,doesnt perform any validations and sets the test to pass.

#### Running tests in an Azure DevOps pipeline

#### Custom Commands

#### Custom Speech Recognition

#### Custom TTS voices

#### Custom Settings

```

```
