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
`field type (e.g. string, integer) | optional or required field |  default value (for optional fields only) | example field value`. Followed by some notes on how to use this field.

Here is the full list:

>#### InputFolder
>`string | optional | empty string | "C:\\Tests\\TestInputFolder\\"`. Full or relative path to the folder that contains all the input JSON test files and WAV files. You will likely want the string to end with "\\\\" since input file names will be appended to this path.  

>#### OutputFolder
>`string | optional | empty string | "C:\\Tests\\TestOutputFolder\\"`. Full or relative path to the folder where output files will be written. The folder will be created if it does not exist. You will likely want the string to end with "\\\\" since output file names will be appended to this path.        |

>#### SubscriptionKey
>`string | required | “01234567890abcdef01234567890abcdef"`. Cognitive Services Speech API Key. Should be a GUID without dashes.

>#### Region
>`string | required | "westus"`. Azure region associated with your [SubscriptionKey](#subscriptionkey).

>#### SRLanguage
>`string | optional | "en-US" | "es-MX"`. Speech Recognition Language. It is the source language of your audio. Must be one of the Locale values mentioned in this [Speech-to-text table](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support).

>#### CustomCommandsAppId
>`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom Commands App ID. Should be a GUID with dashes.

>#### CustomSREndpointId
>`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom SR Endpoint ID. Should be a GUID with dashes. Sets the endpoint ID of a customized speech model that is used for speech recognition.

>#### CustomVoiceDeploymentIds
>`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom Voice Deployment ID. Should be a GUID with dashes.

>#### TTSAudioDurationMargin
> `int | optional | 200 | 100`. Margin to verify the duration of bot-response TTS audio. Units are msec. The test will succeed if the actual TTS audio duration is within TTSAudioDurationMargin msec of the value specified by [ExpectedTTSAudioResponseDuration](#expectedttsaudioresponseduration).  

>#### AppLogEnabled
>`bool | optional | true | false`. When true, application (console) logs will also be written to a text file named VoiceAssistantTest.

>#### SpeechSDKLogEnabled
>`bool | optional | false | true`. When true Speech SDK logs will be written to a text file, with a name that incorporates the date and time the log was created: SpeechSDKLog-yyyy-MM-dd-HH-mm-ss.txt. Note that these logs are extremely verbose. Enable them only per request from Microsoft to assist Microsoft in investigating a report issue.

>#### BotGreeting
>`bool | optional | false | true`. A boolean which defines if your bot or custom command application has an automatic greeting upon connection. This implies that the first turn of the first dialog after connection should verify the bot response activity without providing any client application input in the form of WAV file, text or activity. For an example, see the [getting started guide](#getting-started-guide)

>#### Timeout  
>`int | optional | 5000 | 1234`. A timeout in msec to wait for all bot responses in each turn. If by this time the bot has not sent the expected number of activities, the test will fail.

>#### KeywordRecognitionModel
>`string | optional | null | "C:\\Test\\test.table"`. A full-path name of the keyword model file. For more info, see the section [Keyword Activation Tests](#keyword-activation-tests).

<font color="red">TODO: Make the above relative path to InputFolder?</font>

>#### SetPropertyId
>`JSON string | optional | null | [{12345, "PropertyValue"}]`. A JSON string that is an array of pairs of integer and string values, used for custom settings of the client Speech SDK. Each pair results in a call to [DialogServiceConfig.SetProperty(PropertyId, string)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetProperty_Microsoft_CognitiveServices_Speech_PropertyId_System_String_) or the equivalent method on CustomCommandsConfig for custom command applications. For more detail, see the section [Custom Settings](#custom-settings)

>#### SetPropertyString 
>`JSON string | optional | null | [{"PropertyKey", "PropertyValue"}]`. A JSON string that is an array of pairs of two string values, used for custom settings of the client Speech SDK. Each pair results in a call to [DialogServiceConfig.SetProperty(string, string)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetProperty_System_String_System_String_) or the equivalent method on CustomCommandsConfig for custom command applications. For more detail, see the section [Custom Settings](#custom-settings)

>#### SetServiceProperty
>`JSON string | optional | null | [{"PropertyKey", "PropertyValue"}]`. A JSON string that is an array of pairs of two string values, used for custom settings of the Speech Service. Each pair results in a call to [DialogServiceConfig.SetServiceProperty(String, String, ServicePropertyChannel)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setserviceproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetServiceProperty_System_String_System_String_Microsoft_CognitiveServices_Speech_ServicePropertyChannel_), where ServicePropertyChannel is set to ServicePropertyChannel.UriQueryParameter. Or the equivalent method on CustomCommandsConfig for custom command applications. For more detail, see the section [Custom Settings](#custom-settings)

>#### Tests
>`JSON string | required | [{"FileName":"MyTestFile.json", "SingleConnection": true}]`. An array of JSON objects, each related to a single test configuration. Each of these JSON objects includes:
>
>>##### FileName
>>`string | required | "Test1\\MyTestFile.json"`. The test configuration file name. It can be a file name without path or with a relative path. See the section [Test configuration file](#test-configuration-file) for details on the JSON format of this file.
>>
>>#### IgnoreActivities
>>`JSON string | optional | null | [{"type": "typing","name": "trace"}, {"name": "QnAMaker"}]`. List of bot-response activities that are ignored by tool. For more information see the section [Ignoring certain bot response activities](#ignoring-certain-bot-reply-activities)
>>
>>#### SingleConnection
>>`bool | optional | false | true`. If true, connection with the bot (or custom command application) will be re-established before each dialog test. If false, the connection will be established before the first dialog in the test is run, and it will be kep open while all dialog tests in the file are run.
>>
>>#### Skip
>>`bool | optional | false | true`. If true, the test file will be skipped while executing tests. This is useful when the application configuration file specifies multiple test files, but you only want to run one (or a few) of them. Use Skip to temporary disable tests.

### Test configuration file

The following are the fields supported by the Test Configuration file. Each field is specified using the following format:

#### FieldName
`field type (e.g. string, integer) | optional or required field |  default value (for optional fields only) | example field value`. Followed by some notes on how to use this field.

Here is the full list:

>#### DialogId  
>`string | required | "0"`. A unique value within the test file that identifies this dialog. You can identify a dialog by giving each one a random GUID value, an integer counter, or anything else. Intended to be short.

>#### Description
>`string | optional | null | "Dialog for reserving airline ticket"`. Free-form text description of what this dialog does, to help you remember. Does not have to be unique.

>#### Turns
>`JSON string | required`. An array of JSON objects, each defines a single turn in the dialog to execute. Each of these JSON objects includes:
>
>>##### TurnId
>>`integer | required | 0`. A zero-based count of the turn in the dialog. The first turn in the dialog must have a TurnId value of 0, the second one 1, etc.
>>
>>##### Skip
>>`bool | optional | false | true`. If true, this dialog will not be executed. It will be skipped. This is useful when the test file includes multiple dialogs, but you only want to run one (or a few) of them. Use Skip to temporary disable dialogs in your test file.
>>
>>##### Sleep
>>`int | optional | 0 | 1234`. If set to a positive value, the tread executing the dialog will pause by this amount of duration (in msec units) before executing the turn.
>>
>>##### Utterance
>>`string | optional | null | "what is the weather tomorrow?"`. The field has two usages. If [WavFile](#wavfile) is not specified, this is the text that will be sent up to the bot as a Bot-Framework activity of type "message". Representing a user typed-text input. If [WavFile](#wavfile) is defined, this is the expected recognition result of the audio in the WAV file. If the recognition result does not match what is specified in this field, the test will fail. Note that the text comparison in this case is done while ignoring punctuation marks, upper/lower case differences and white space.
>>
>>##### WavFile
>>` string | optional | null | "test1.WAV"`. Audio from this WAV file is streaming to Direct Line Speech as the input in the turn, by calling the [ListenOnceAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.listenonceasync?view=azure-dotnet) method (or [StartKeywordRecognitionAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.startkeywordrecognitionasync?view=azure-dotnet) method if [Keyword](#keyword) is true). This represents a user speaking to a microphone. It's good practice to have at least one second of silence (non speech) at the end of the WAV file to make sure the speech service properly detects end-of-speech, as it would with a live audio stream from a microphone. When this field is present, you can specify the expected recognition result in the [Utterance](#utterance) field.
>>
>>##### Activity
>>`JSON string | optional | null | "{\"type\”: \"message\",\"text\":\"Test sending text via activity\"}"`. A bot-framework JSON activity string. If this field is specified, you cannot specify the [WavFile](#wavfile) or [Utterance](#utterance) fields. Use this to send any custom activity to your bot using the [SendActivityAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.sendactivityasync?view=azure-dotnet) method.
>>
>>##### Keyword
>>` bool | optional | false | true`. If true, the audio in the supplied [WavFile](#wavfile) starts with a keyword, and the [StartKeywordRecognitionAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.startkeywordrecognitionasync?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConnector_StartKeywordRecognitionAsync_Microsoft_CognitiveServices_Speech_KeywordRecognitionModel_) will be called to detect the keyword and stream audio to Direct Line Speech channel if the keyword was recognized. The field has meaning only if both [WavFile](#wavfile) and [KeywordRecognitionModel](#KeywordRecognitionModel) were defined.
>>
>>##### ExpectedResponses 
>>`JSON string | optional`. An array of Bot-Framework JSON activities. These are the expected bot responses. You only need to specify the activity fields you care about. If the number of bot responses is less than what you specify here, the test will fail. If the number is the same, but some of the fields you specify do not match the fields in the bot reply, the test will fail. Note that the tool orders the bot responses based on their activity [Timestamp](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#timestamp), before comparing the actual bot response to the expected response. If the number of bot responses is greater than the expected number, and there is a match to all expected activities, the test will pass. If ExpectedResponses is null or missing, the turn will run in bootstrapping mode, which is useful to do when writing the tests for the first time. See the [bootstrapping mode](#bootstrapping-mode) section for more details.
>>
>>##### ExpectedTTSAudioResponseDuration 
>>`array of integers | optional | null | [1500, -1, 2000]`. Expected duration (in msec) of bot response TTS audio stream. This allows you to validate that the right audio stream duration was downloaded by the tool. Otherwise the test will fail. The length of this array (if exists) must match the length of the [ExpectedResponses](#expectedresponses) array. Not every bot-response will have a TTS audio stream associated with it. In that case, specify a value of -1 in the array cell. The expected duration does not have to exactly match the actual duration for the test to succeed. This is controlled by the field [TTSAudioDurationMargin](#ttsaudiodurationmargin). Only if the different between expected duration and actual duration is outside this margin, the test will fail.
>>
>>##### ExpectedResponseLatency 
>>`string | optional | null | "500", or "500,0" or "500,1"'. The expected time the tool should have received a particular bot-response activity. If the tool did not receive that activity by this latency value, the test will fail. There are two formats for the string. The first one just includes a positive integer. In this case the bot-response that is timed is the last one expected to arrive (based on the length of the [ExpectedResponses](#ExpectedResponses) array]). So for example of the length of ExpectedResponses is 3, it means the tool will wait until it receives three bot response activities. If the 3rd one was not received by the time specified by  Expectedresponse, the test will fail. The second format of the string is a positive integer (the duration), followed by a comma, followed by a zero-based index. The index specifies which of the bot-response activities should be time-measured, which 0 being the first one specified in the ExpectedResponses array. This second format allows you to put an upper limit on either one of the bot responses, not just the last one.

## Topics

### Bootstrapping mode

Creating the test configuration files for the first time is hard, if your intention is to author the expected bot responses manually ([ExpectedResponses](#ExpectedResponses) field). For that reason we created the bootstrapping mode. This is where you run the test tool such that a particular turn in a dialog is run for the purpose of discovering the current bot responses, not for the purpose of failing or passing the test based on some expected bot responses. After running in bootstrapping mode, you can look at the detailed test result and find the bot responses as a array of JSON activities. You can then copy the array, filter out what you don't need then paste them as the [ExpectedResponses](#ExpectedResponses) ready for the next time your tun the tool.

Here is the recipe to creating a new regression test from scratch:

1. Do manual tests of your bot (or custom commands application) and make sure it behaves as expected. You can use the [Windows Voice Assistant Client](../../csharp-wpf/README.md) for this purpose. Your goal is now to write regression tests to make sure the bot behavior does not change.
1. Author your [application configuration file](#application-configuration-file). You can start [from the template](docs/json-templates/app-config-template.json), modify and delete what you don't need. Define one test configuration file. Here we will assume it is named TestConfig.json.
1. Create your [test configuration file](#test-configuration-file) TestConfig.json with one dialog, one turn, by only specifying the required fields. For example, if your bot supports greeting, you do not need to specify any input in Turn 0, so this is all you need:
    ```json
    [
      {
        "DialogID": "0",
        "Turns": [
          {
            "TurnID": 0
          }
        ]
      }
    ]
    ```
    If your bot does not have a greeting, you do need to specify something to send up to the bot. It can be audio ([WavFile](#wavfile)), text ([Utternace](#utternace)) or a bot-framework activity ([Activity](#activity)). Add one of those fields under the TurnID field (e.g. "Utterance" : "Some text").
1. Run your test. As [ExpectedResponses](#ExpectedResponses) is not specified, in means Turn 0 will run in bootstrapping mode. The test should pass if the connection with your bot was successful and bot response was received within the [Timeout](#timeout) duration. 
1. Open the detailed test result file. If your test file is named TestConfig.json, the detailed test result will be named TestConfigOutput.json. 
1. Copy the ActualResponses field, and optionally the ActualTTSAudioResponseDuration and ActualResponseLatency fields, and paste them into TestConfig.json, in the Turn 0 scope.
1. Renamed those fields to ExpectedResponses, ExpectedTTSAudioResponseDuration and ExpectedResponseLatency.
1. Now edit ExpectedResponses and remove bot-framework activity fields, such that only a few are left -- the ones that you care about when it comes to evaluating bot response regressions. This includes removing fields such as [ID](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#id) and [Timesamp](#https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#timestamp) that are not fixed.
1. If you want your test to fail when the duration of the TTS audio changes (with [TTSAudioDurationMargin](#ttsaudiodurationmargin)), leave the ExpectedTTSAudioResponseDuration field. Otherwise don't include it. If you want the test to fail when the bot response latency exceeds a certain value, edit the ExpectedResponseLatency field to include the upper limit of the latency.
1. Run the test again and make sure it succeeds. 
1. If your bot supports additional turns, you can continue the process described above to populate the test for the next turn. That is, edit your TestConfig.json by adding { "TurnID": 1 } in your Turns array and run the test. This means Turn 0 runs normally, and Turn 1 is in bootstrapping mode.

### Keyword Activation Tests

To test a Bot with an audio input with a keyword, populate "KeywordRecognitionModel" in Application Configuration file with the full path of the table file and set the boolean "Keyword" in the test configuration file to "true" if the WAV file used for testing has a keyword.

The keyword that has been recognized will be populated in the output file.

A dialog can only have a Keyword in Turn 0.

### Ignoring certain bot-reply activities

Tool waits for certain amount of time or until it hits timeout and captures all the Bot-reply activities received for each turn. This time is equal to the length of the ExpectedResponses array that is set in the test configuration file. Tool perfoms validations on these captured Bot-reply activities to check if the turn is passed or failed.

In order to ignore any one of the Bot-reply activities received for each turn by the tool, "IgnoreActivities" field is to be set in the Application Configuration file.
"IgnoreActivities" is an Array that holds the list of activities that are ignored by the tool. The amount of time for which tool waits to capture the Bot-reply activities does not include the Bot-reply activities that were marked ignore.

Example : [{"type": "typing","name": "trace"}, {"name": "QnAMaker"}]

In the above example tool ignores all activities which are of type : "typing"and name : "trace" and all activities which have "name" : "QnAMaker".

### Testing bot response TTS Audio duration

A Bot-reply activity with speak field populated, will have a TTS audio.
Tool stores this TTS audio in a WAV file. The duration of TTS audio is calculated and is verified if it falls within the range of ExpectedTTSAudioResponseDuration +/- AudioDurationMargin.
if the test is passed TTSAudioResponseDurationMatch is set to true otherwise false.

### Testing bot-greetings

Bot-Greeting - It is an automatic Bot response that happens when client connect to the Bot with no input from the client.
Application Configuration file holds a field called "Bot Greeting" that should be set to true when a Bot has a greeting.

For testing the Bot Greeting when,
-SingleConnection = "true", test configuration file should include a Dialog 0, Turn 0 entry with no input fields(Utterance, Activity and WavFile) speciied
-SingleConnection = "false",test configuration file should include a Turn 0 entry on every Dialog with no input fields(Utterance, Activity and WavFile) specified

### Skipping tests or dialogs

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

### Pausing between dialogs or turns

### Running tests in an Azure DevOps pipeline

### Custom Commands

### Custom speech recognition

### Custom TTS voices

### Custom settings
