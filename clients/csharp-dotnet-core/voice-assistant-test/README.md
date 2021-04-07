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

>#### SpeechSubscriptionKey
>`string | required | "01234567890abcdef01234567890abcdef"`. Cognitive Services Speech API Key. Should be a GUID without dashes.

>#### SpeechRegion
>`string | required | "westus"`. Azure region associated with your [SpeechSubscriptionKey](#speechubscriptionkey).

>#### SRLanguage
>`string | optional | "en-US" | "es-MX"`. Speech Recognition Language. It is the source language of your audio. Must be one of the Locale values mentioned in this [Speech-to-text table](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support).

>#### CustomCommandsAppId
>`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom Commands App ID. Should be a GUID with dashes.

>#### CustomSREndpointId
>`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom SR Endpoint ID. Should be a GUID with dashes. Sets the endpoint ID of a customized speech model that is used for speech recognition.

>#### CustomVoiceDeploymentIds
>`string | optional | null | "01234567-89ab-cdef-0123-456789abcdef"`. Custom Voice Deployment ID. Should be a GUID with dashes.

>#### TTSAudioDurationMargin
> `int | optional | 200 | 100`. Margin to verify the duration of bot-response TTS audio. Units are msec. The test will succeed if the actual TTS audio duration is within TTSAudioDurationMargin msec of the value specified by [ExpectedTTSAudioResponseDurations](#expectedttsaudioresponseduration).  

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

<!--<font color="red">TODO: Make the above relative path to InputFolder?</font>-->

>#### KeywordVerificationEnabled
>`bool | optional | false | true`. A boolean which indicates if your bot or custom command application is configured to use keyword verification. The default value is `true`. Note that if a [CustomSREndpointId](#customsrendpointid) is specified, this value is ignored and keyword verification is automatically disabled.

>#### PushStreamEnabled
>`bool | optional | false | true`. A boolean which indicates if the test should use a "push" stream to interact with the Speech SDK. The default value is `false`. This can be useful in making sure the tests mimic your application.

>#### AriaProjectKey
>`sring | optional | null | "0123456789abcdef0123456789abcdef-01234567-890a-bcde-f012-34567890abcd-ef01"`. An optional Aria project key. If given, dialog success and failure events will be sent to the Aria cloud. For more information on Aria, see [https://www.aria.ms/](https://www.aria.ms/).

>#### SetPropertyId
>`JSON string | optional | null | [{12345, "PropertyValue"}]`. A JSON string that is an array of pairs of integer and string values, used for custom settings of the client Speech SDK. Each pair results in a call to [DialogServiceConfig.SetProperty(PropertyId, string)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetProperty_Microsoft_CognitiveServices_Speech_PropertyId_System_String_) or the equivalent method on CustomCommandsConfig for custom command applications. For more detail, see the section [Custom Settings](#custom-settings)

>#### SetPropertyString 
>`JSON string | optional | null | [{"PropertyKey", "PropertyValue"}]`. A JSON string that is an array of pairs of two string values, used for custom settings of the client Speech SDK. Each pair results in a call to [DialogServiceConfig.SetProperty(string, string)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetProperty_System_String_System_String_) or the equivalent method on CustomCommandsConfig for custom command applications. For more detail, see the section [Custom Settings](#custom-settings)

>#### SetServiceProperty
>`JSON string | optional | null | [{"PropertyKey", "PropertyValue"}]`. A JSON string that is an array of pairs of two string values, used for custom settings of the Speech Service. Each pair results in a call to [DialogServiceConfig.SetServiceProperty(String, String, ServicePropertyChannel)](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconfig.setserviceproperty?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConfig_SetServiceProperty_System_String_System_String_Microsoft_CognitiveServices_Speech_ServicePropertyChannel_), where ServicePropertyChannel is set to ServicePropertyChannel.UriQueryParameter. Or the equivalent method on CustomCommandsConfig for custom command applications. For more detail, see the section [Custom Settings](#custom-settings)

>#### RealTimeAudio
>`bool | optional | false | true`. The default behavior of PushAudioInputStream is to send the first few seconds of audio as fast as possible to accommodate for short utterances. Following audio is throttled back to prevent client from spamming the service too fast but this throttled speed is faster than real-time microphone input. Set this optional flag to true to send audio at real-time (x1) speed. If this flag is set to true, the app config [Timeout](#Timeout) should be larger than the duration of the longest WAV file in the test. For more information see [Measuring User Perceived Latency](#Measuring-User-Perceived-Latency) section.

>#### Tests
>`JSON string | required | [{"FileName":"MyTestFile.json", "SingleConnection": true}]`. An array of JSON objects, each related to a single test configuration. Each of these JSON objects includes:
>
>>##### FileName
>>`string | required | "Test1\\MyTestFile.json"`. The test configuration file name. It can be a file name without path or with a relative path. See the section [Test configuration file](#test-configuration-file) for details on the JSON format of this file.
>>
>>#### IgnoreActivities
>>`JSON string | optional | null | [{"type": "typing","name": "trace"}, {"name": "QnAMaker"}]`. List of bot-response activities that are ignored by tool. For more information see the section [Ignoring certain bot response activities](#ignoring-certain-bot-response-activities)
>>
>>#### SingleConnection
>>`bool | optional | false | true`. If false, connection with the bot (or custom command application) will be re-established before each dialog is run, and it will close after the dialog is done. If true, the connection will be established before the first dialog in a test file, and it will remain open while executing all dialogs in that test file. Note that if your application configuration file includes multiple test configuration files and SingleConnection is true, a new connection will be established before running the first dialog in each test file.
>>
>>#### Skip
>>`bool | optional | false | true`. If true, the test file will be skipped while executing tests. This is useful when the application configuration file specifies multiple test files, but you only want to run one (or a few) of them. Use Skip to temporary disable tests.
>>
>>#### WavAndUtterancePairs
>>`bool | optional | false | true`. "If true, and a dialog has both [WavFile](#wavfile) and [Utternace](#utterance) fields populated, the dialog will be [run twice](#Running-tests-twice-once-with-text-input-and-once-with-audio), first with wav input, and then with text input. If false, a dialog will only be run once, either with wav input or text input.

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
>>`string | optional | null | "test1.WAV"`. Audio from this WAV file is streaming to Direct Line Speech as the input in the turn, by calling the [ListenOnceAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.listenonceasync?view=azure-dotnet) method (or [StartKeywordRecognitionAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.startkeywordrecognitionasync?view=azure-dotnet) method if [Keyword](#keyword) is true). This represents a user speaking to a microphone. It's good practice to have at least one second of silence (non speech) at the end of the WAV file to make sure the speech service properly detects end-of-speech, as it would with a live audio stream from a microphone. When this field is present, you can specify the expected recognition result in the [Utterance](#utterance) field.
To send WavFile's at real time (x1) speed, see the [Measuing User Perceived Latency](#Measuring-User-Perceived-Latency) and [RealTimeAudio](#RealTimeAudio) sections.
To generate WAV files using Speech CLI, see the [Generating WAV files using Speech CLI](#Generating-WAV-files-using-Speech-CLI) section.
>>
>>##### Activity
>>`JSON string | optional | null | "{\"type\": \"message\",\"text\":\"Test sending text via activity\"}"`. A bot-framework JSON activity string. If this field is specified, you cannot specify the [WavFile](#wavfile) or [Utterance](#utterance) fields. Use this to send any custom activity to your bot using the [SendActivityAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.sendactivityasync?view=azure-dotnet) method.
>>
>>##### Keyword
>>` bool | optional | false | true`. If true, the audio in the supplied [WavFile](#wavfile) starts with a keyword, and the [StartKeywordRecognitionAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.startkeywordrecognitionasync?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConnector_StartKeywordRecognitionAsync_Microsoft_CognitiveServices_Speech_KeywordRecognitionModel_) will be called to detect the keyword and stream audio to Direct Line Speech channel if the keyword was recognized. The field has meaning only if both [WavFile](#wavfile) and [KeywordRecognitionModel](#KeywordRecognitionModel) were defined. For more info, see the section [Keyword Activation Tests](#keyword-activation-tests).
>>
>>##### ExpectedResponses 
>>`JSON string | optional`. An array of Bot-Framework JSON activities. These are the expected bot responses. You only need to specify the activity fields you care about. If the number of bot responses is less than what you specify here, the test will fail. If the number is the same, but some of the fields you specify do not match the fields in the bot reply, the test will fail. Note that the tool orders the bot responses based on their activity [Timestamp](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#timestamp), before comparing the actual bot response to the expected response. If the number of bot responses is greater than the expected number, and there is a match to all expected activities, the test will pass. If ExpectedResponses is null or missing, the turn will run in bootstrapping mode, which is useful to do when writing the tests for the first time. See the [bootstrapping mode](#bootstrapping-mode) section for more details.
>>
>>##### ExpectedTTSAudioResponseDurations 
>>`array of strings | optional | null | ["1500", "-1", "2000 || 1700"]`. Expected duration (in msec) of bot response TTS audio stream. This allows you to validate that the right audio stream duration was downloaded by the tool. Otherwise the test will fail. The length of this array (if exists) must match the length of the [ExpectedResponses](#expectedresponses) array. Not every bot-response will have a TTS audio stream associated with it. In that case, specify a value of -1 in the array cell. The expected duration does not have to exactly match the actual duration for the test to succeed. This is controlled by the field [TTSAudioDurationMargin](#ttsaudiodurationmargin). Only if the different between expected duration and actual duration is outside this margin, the test will fail. See the [Getting Started Guide](docs/GETTING-STARTED-GUIDE.md) for an example of using ExpectedTTSAudioResponseDurations.
>>
>>##### ExpectedUserPerceivedLatency 
>>`string | optional | null | "500", or "500,0" or "500,1"`. The expected time the tool should have received a particular bot-response activity. If the tool did not receive that activity by this latency value, the test will fail. There are two formats for the string. The first one just includes a positive integer. In this case the bot-response that is timed is the last one expected to arrive (based on the length of the [ExpectedResponses](#ExpectedResponses) array). So for example of the length of ExpectedResponses is 3, it means the tool will wait until it receives three bot response activities. If the 3rd one was not received by the time specified by ExpectedUserPerceivedLatency, the test will fail. The second format of the string is a positive integer (the duration), followed by a comma, followed by a zero-based index. The index specifies which of the bot-response activities should be time-measured, with 0 being the first one specified in the ExpectedResponses array. This second format allows you to put an upper limit on either one of the bot responses, not just the last one.

## Topics

### Bootstrapping mode

Creating the test configuration files for the first time is hard, if your intention is to author the expected bot responses manually ([ExpectedResponses](#ExpectedResponses) field). For that reason we created the bootstrapping mode. This is where you run the test tool such that a particular turn in a dialog is run for the purpose of discovering the current bot responses, not for the purpose of failing or passing the test based on some expected bot responses. After running in bootstrapping mode, you can look at the detailed test result and find the bot responses as a array of JSON activities. You can then copy the array, filter out what you don't need then paste them as the [ExpectedResponses](#ExpectedResponses) ready for the next time you run the tool.

Here is the recipe to creating a new regression test from scratch:

1. Do manual tests of your bot (or custom commands application) and make sure it behaves as expected. You can use the [Windows Voice Assistant Client](../../csharp-wpf/README.md) for this purpose. Your goal is now to write regression tests to make sure the bot behavior does not change.
1. Author your [application configuration file](#application-configuration-file). You can start [from the template](docs/json-templates/app-config-template.json), modify and delete what you don't need. Define one test configuration file. Here we will assume it is named TestConfig.json.
1. Create your [test configuration file](#test-configuration-file) named TestConfig.json with one dialog, one turn, by only specifying the required fields. For example, if your bot supports greeting, you do not need to specify any input in Turn 0, so this is all you need:
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
1. Run your test. As [ExpectedResponses](#ExpectedResponses) is not specified, in means Turn 0 will run in bootstrapping mode. The tool will wait for a period specified by  [Timeout](#timeout) to collect all bot-responses. Note the default value of Timeout. As long as connection to your bot succeeded, the test should be marked as passed after the Timeout duration.
1. Open the detailed test result file. If your test file is named TestConfig.json, the detailed test result will be named TestConfigOutput.json. 
1. Copy the ActualResponses field, and optionally the ActualTTSAudioResponseDuration and ActualResponseLatency fields, and paste them into TestConfig.json, in the Turn 0 scope.
1. Renamed those fields to ExpectedResponses, ExpectedTTSAudioResponseDurations and ExpectedUserPerceivedLatency.
1. Now edit ExpectedResponses and remove bot-framework activity fields, such that only a few are left -- the ones that you care about when it comes to evaluating bot response regressions. This includes removing fields such as [ID](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#id) and [Timesamp](#https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#timestamp) that are not fixed.
1. If you want your test to fail when the duration of the TTS audio changes (with [TTSAudioDurationMargin](#ttsaudiodurationmargin)), leave the ExpectedTTSAudioResponseDurations field. Otherwise don't include it. If you want the test to fail when the bot response latency exceeds a certain value, edit the ExpectedUserPerceivedLatency field to include the upper limit of the latency.
1. Run the test again and make sure it succeeds. 
1. If your bot supports additional turns, you can continue the process described above to populate the test for the next turn. That is, edit your TestConfig.json by adding { "TurnID": 1 } in your Turns array and run the test. This means Turn 0 runs normally, and Turn 1 is in bootstrapping mode.

### Ignoring certain bot-response activities

When a dialog turn is run, the test sends up information and waits for bot responses. It waits until the bot responds with a number of activities equal to the number of activities in the [ExpectedResponses](#expectedresponses) field, or until in the elapsed [Timeout](#timeout) has passed. If it timeout, the test fails. If it got the number of expected responses, it compares the actual responses with the expected responses. However, sometimes the bot sends responses that are not important for the purpose of determining test pass/fail. For example, if your bot is using [LUIS](https://www.luis.ai/) or [QnA Maker](https://www.qnamaker.ai/), the tool may get traces from these back-end services if your bot is configured to send them. Or, your bot may send a "typing" message, indicating it is working on a response.

The tool can be configured to ignore these or any other bot-response activity, when waiting for the expected number of bot responses. This is done by defining the [IgnoreActivities](#ignoreactivities) field in the application configuration tile. Create an array of JSON strings that specify the bot-framework activities you want to ignore. You only need to specify a few activity fields to filter out bot responses. For example, if you specify this:
```json
[{"type": "typing", "name": "trace"}, {"name": "QnAMaker"}]
```
All bot-response activities which are of [type](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#type) "typing" and [name](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#name) "trace" will be ignored. Also, all bot-response activities with name "QnAMaker" will be ignored.

### Testing bot-greetings

Bot-greeting is an automatic bot-response that happens when the client first connects to the bot, without the client sending anything up to the bot first. The application configuration file has an optional field called [BotGreeting](#botgreeting) that should be set to true when a bot has a greeting.

It's important to understand how to configure your test correctly to support bot-greeting, and how that depends on the [SingleConnection](#singleconnection) setting.

If SingleConnection is false, each dialog will start with a boot greeting. Therefore Turn 0 cannot include any of the fields that send information up to the bot ([Utterance](#utterance), [Activity](#activity) and [WavFile](#wafile)).

If SingleConnection is true, only the first dialog in every test file will see a bot greeting. Therefore Turn 0 of the first dialog in every test file should include a fields that sends information up to the bot.

### Testing response activities with random text and speak from a predefined set

In some cases, Bot or Custom Commands application can define a set of text or speech for responding, and randomly select to use one of them in actual responded activity. For example, a command of Custom Commands application defines 2 sentences asking for the temperature to change - (1) How much do you want to change the temperature by? (2) By how many degrees? This command will randomly use the first sentence in some responses and the second sentence in other responses. To deal with this case, you can use " || " in text, speak of ExpectedResponses field along with ExpectedTTSAudioResponseDurations field to specify the expected responses as below:
```json
    [
      {
        "DialogID": "0",
        "Turns": [
          {
            "TurnID": 0,
            "Utterance": "change temperature",
            "WavFile": "",
            "ExpectedResponses": [
              {
                "type": "message",
                "text": "How much do you want to change the temperature by? || By how many degrees?",
                "speak": "How much do you want to change the temperature by? || By how many degrees?",
              }
            ],
            "ExpectedTTSAudioResponseDurations": ["3000 || 2000"],
          }
        ]
      }
    ]
```
Actual response matches any of the expected responses will pass the test.

### Keyword activation tests

Speech SDK supports keyword activation using the [StartKeywordRecognitionAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.startkeywordrecognitionasync?view=azure-dotnet#Microsoft_CognitiveServices_Speech_Dialog_DialogServiceConnector_StartKeywordRecognitionAsync_Microsoft_CognitiveServices_Speech_KeywordRecognitionModel_) method. To test with keyword activation:
1. [Create a custom keyword using Speech Studio](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-create-kws) and save the model file
1. Populate the [KeywordRecognitionModel](#keywordrecognitionmodel) field in the application settings file to point to this model file
1. Record WAV files that include the keyword at the beginning, following by the sentence you want your bot to receive
1. For every [WavFile](#wavfile) field that points to a WAV file with keyword, add the [Keyword](#keyword) field with a value of "true". This will tell the test tool to call StartKeywordRecognitionAsync instead of ListenOnceAsync. 
1. Run your test

Therefore if [Keyword](#keyword) is true in a dialog turn, the test configuration file will be valid if:
* [WavFile](#wavfile) is defined for the same turn where [Keyword](#keyword) is defined and set to true
* [KeywordRecognitionModel](#keywordrecognitionmodel) is defined in the application configuration.

If the keyword has been recognized successfully, the identified keyword will be listed in the string field named "KeywordVerified" in the output JSON test result file.

Note that due to a bug in the way Speech SDK consumes audio from an input stream, keyword activation is limited to the first turn of the dialog. Therefore [Keyword](#keyword) can only be set to true when [TurnID](#turnid) is 0.

### Measuring User Perceived Latency

We define User Perceived Latency (UPL) as the duration between the time the user stops speaking (or submitting text input) and the time the uses sees an action taken by the Voice Assistant as a response. This test tool is generic and does not execute any real actions other than TTS playback. Therefore we define the "action" to be receiving the the first TTS audio butter (if there is a TTS response), or receiving the bot reply activity (if it does not include a TTS response). So it's easy to know when to stop the timer when measuring UPL.

It's harder to know when to start the timer. For that we need to estimate the time speech has stopped. Here of course we use WAV files, but we can't simply measure the duration of the WAV file because speech may have stopped before the WAV file has ended. In fact there should be some short silence at the end of speech in the WAV file to make sure the speech recognition engine detects end-of-speech as if it was audio coming from a live microphone. To solve this we rely on an argument in the Speech recognition event called [RecognitionResult.Duration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.recognitionresult.duration?view=azure-dotnet#Microsoft_CognitiveServices_Speech_RecognitionResult_Duration). This gives us the duration of the speech excluding any pre or post silence/noise. Assuming the WAV file is authored to make sure there is no silence at the beginning, we can use this value to estimate the duration of the speech portion in the WAV file. We therefore start the timer in the [SessionStarted](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog.dialogserviceconnector.sessionstarted?view=azure-dotnet) event. We stop the timer when the bot reply activity was received (or first TTS buffer received). But then we adust the elapsed time by subtracting the duration of the speech in the WAV file.

The above will only work if Speech SDK is consuming input audio at real-time, as if it was a live microphone input. Turns out that by default this is not the case. When processing audio from an input stream, as done in this test tool, Speech SDK by default sends the first few seconds of audio as fast as it can, then throttles down the speed to a constant rate to prevent the client from flooding the service. The throttled speed is still faster than real-time microphone speed. This was done to help speed up speech recognition in mass batch-processing of WAV files for transcription.

In our case we need to set the input stream consumption rate to real-time. For that we added a boolean application configuration file called [RealTimeAudio](#RealTimeAudio). It is off by default. Setting it to true will result in these to Speech SDK calls:
```csharp
config.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", "100")
config.SetProperty("SPEECH-TransmitLengthBeforeThrottleMs", "0");
```
The first property fixes the transmit speed at real time and the second removes the burst behavior at the start of speech recognition. Together they allow simulating speech recognition at real-time from an audio stream, as if it was live microphone input.

Since now audio is consumed at real-time instead of faster than real time, you may need to adjust the [Timeout](#Timeout) duration. Make sure that this time is larger than the longest duration WAV file. 

In summary, to accurately measure UPL do these three things:
1. Make sure your WAV files do not have silence at the beginning. Speech should start right at the beginning of the WAV file
1. Set the value of [RealTimeAudio](#RealTimeAudio) to true in your application configuration file
1. Make sure the value for [Timeout](#Timeout) in your application configuration file is set to a value higher than the longest duration WAV file

Note: This has not been tested when keyword activation is used. At this time we do not recommend measuring UPL using the above method when [Keyword](#Keyword) is set to true.

### Running tests twice once with text input and once with audio

The [WavAndUtterancePairs](#WavAndUtterancePairs) flag, if set to true will run the same test twice once with audio and again with text. A turn with audio input may fail due to incorrect speech recognition result but may pass with text input indicating an audio or SR issue. The test tool's UtteranceMatch compares the SR result with the Utterance. In order to run the test twice, the WavFile and Utterance must be present for the respective turn. Some reasons why a WavFile could return incorrect SR could be because there is not ending silence for proper segmentation, WavFile audio is not long enough, or incorrect format. We recommend setting this flag to true that an audio input to make sure the test is also run with text input.

### Running tests in an Azure DevOps pipeline

The Visual Studio Project is configured to published a self-contained Windows console executable that includes the .NET Core run-time and all NuGet dependencies. This means you only need to copy one file, VoiceAssistantText.exe, to the target Windows 10 PC or VM, together with your test configuration files and WAV files to run your test. The publishing is done by these lines at the end of [VoiceAssistantTest.csproj](tool/VoiceAssistantTest.csproj):
```xml
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="dotnet publish -c $(Configuration) -r win-x64 /p:PublishSingleFile=true /p:NoBuild=true" />
  </Target>
```
For example, for the DEBUG build, the self-contained executable will be found under:
\bin\Debug\netcoreapp3.1\win-x64\publish\VoiceAssistantTest.exe.

To deploy the test as an Azure DevOps (ADO) Task, simply create a [Command Line Task](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/utility/command-line?view=azure-devops&tabs=yaml) in your ADO pipeline.

Here is an example YAML script:
```yml
TBD (+ show populating the artifacts folder)
```

<!---
TBD

### Custom commands

TBD

### Custom speech recognition

TBD

### Custom TTS voices

TBD

### Other custom settings
-->


### Generating WAV files using Speech CLI

When you write tests with voice input, instead of recording WAV files, you can create them by using Microsoft Text-to-Speech service. The Speech CLI is a command line tool for using the Speech service without writing any code. With your Speech subscription key and region information (ex. eastus, westus) ready, within minutes you can run text-to-speech to generate WAV files for testing on a single string directly from the command line or a collection of strings from a file. [Learn the basics of the Speech CLI](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/spx-basics?tabs=windowsinstall) shows how to download and install Speech CLI, and how to run commands with SPX.

To get started, run following commands to setup and generate a Hello world WAV file on the fly.
```
spx config @key --set YOUR-SUBSCRIPTION-KEY
spx config @region --set YOUR-REGION-ID
spx synthesize --text "Hello world! " --audio output hello.wav
```
The above commands generate WAV file in default en-US standard voice. The text-to-speech service provides many options for synthesized voices, under [text-to-speech language support](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#text-to-speech). Following commands show how to list supported voices, synthesize with a specific voice in English, and synthesize in German.
```
spx synthesize --voices
spx synthesize --voice en-US-GuyNeural --text "Hello world!"
spx synthesize --voice de-DE-KatjaNeural --text "Hallo Welt!"
```
To learn how you can configure and adjust neural voices, see [Speech synthesis markup language](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup#adjust-speaking-styles). 
