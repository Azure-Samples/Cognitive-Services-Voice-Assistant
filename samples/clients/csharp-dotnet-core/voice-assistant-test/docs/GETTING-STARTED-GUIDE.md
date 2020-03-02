# Getting Started Guide

## Overview
This is a step-by-step guide for first time users of the tool, showing how to write configuration files for the most common end-to-end tests, and how to run the tool. The guide uses Bot-Framework's "core bot" as the example bot to be tested.

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

The build will create a self-contained .NET core executable VoiceAssistantTest.exe in a "publish" folder that you can copy to your working folder, where you will run your tests. It's best not to run the test under your repo folder. Create a test folder (e.g. "c:\test" in this example), copy the executable to this folder and run it without arguments:

```cmd
cd /d c:\test
copy c:\repo\Cognitive-Services-Voice-Assistant\samples\clients\csharp-dotnet-core\voice-assistant-test\tool\bin\Debug\netcoreapp3.1\win-x64\publish\VoiceAssistnatText.exe
VoiceAssistantText.exe
```
You will get the following error:
```
VoiceAssistantTest Error: 0 : System.ArgumentException: Configuration file is not specified. Please pass a valid configuration file as an argument.
```
This is good. The tool works. You are now ready to author your first application and test configuration files. The application configuration file is needed as the single run-time argument when running the tool. But before we write the test, we need to make sure we have a bot web service hosted to test against.

## Step 2: Deploy the Core Bot

In order to write and execute your first test, you will need to deploy Bot Framework's "core bot" into your own Azure subscription. The core bot has to then be voiced enabled and registered with Direct Line Speech channel.

Before you do that, it is recommended you take the time and do the tutorial called ["Voice-enable your bot using the Speech SDK"](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk). This tutorial shows how to deploy the simpler "echo bot" and configure it to work with Direct Line Speech channel. Once you've deployed the echo bot, you will need to do a couple of additional easy steps to replace it with core bot.

The C# version of the core bot can be [found here](https://github.com/microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/13.core-bot). The same repo include javascript and python versions, if you prefer those. Clone the repo, build and publish the core bot to your Azure subscription. Note that this will require you to Create a LUIS Application, per [the instruction here](https://github.com/microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/13.core-bot#create-a-luis-application-to-enable-language-understanding).

When you are done you will have the following:
* A Cognitive Services speech key. 
* An Azure region associated with the speech key.
* A core bot hosted in your Azure subscription, registered with Direct Line Speech channel, and verified to be working end-to-end with voice input using the [Windows Voice Assistant Client](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/samples/clients/csharp-wpf).

## Step 3: Write your first test!

As you have noticed when you tested the core bot with [Windows Voice Assistant Client](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/samples/clients/csharp-wpf), it supports a "greeting". This is when the bot automatically sends a message (or messages) to the client application as soon as a connection is made with the bot. Let's write a simple test to verify that the bot sends the correct greeting message.

First, we will write the Application configuration. Copy and paste the following to your text editor: 
```json
{
  "Tests": [
    {
      "FileName": "TestConfig.json",
    }
  ],
  "SubscriptionKey": "0123456789abcdef0123456789abcdef",
  "Region": "westus2",
  "BotGreeting": true,
}
```
Replace "SubscriptionKey" and "Region" fields with your own speech key and region, and save it as file "[AppConfig.json](core-bot-examples/greeting/AppConfig.json)" in your test folder (e.g. c:\test).

The application configuration file instructs the tool to execute the dialogs listed in a single test file "GreetingTest.json". We will create that file shortly. 

All input test files (or just one in this case -- "GreetingTest.json") and output files will be read written to the current folder, where you run the executable. You can change these defaults by adding the fields "InputFolder" and "OutputFolder" (not shown here).

"BotGreeting" field needs to be specified and set to true for echo bot tests, since by default it is false. A "true" value instructs the tool to verify that the test configuration is written correctly and the test is executed expecting a bot greeting after connection is established with the bot.

Now, write the test configuration. Copy and paste the following to your text editor and save the file as "[TestConfig.json](core-bot-examples/greeting/TestConfig.json)" in your test folder (e.g. c:\test):

```json
[
  {
    "DialogID": "0",
    "Description": "Testing core bot greeting",
    "Turns": [
      {
        "TurnID": 0,
        "ExpectedResponses": [
          {
            "type": "message",
            "speak": "Welcome to Bot Framework!",
            "inputHint": "acceptingInput",
            "attachments": [
              {
                "contentType": "application/vnd.microsoft.card.adaptive"
              }
            ]
          },
          {
            "type": "message",
            "text": "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"",
            "speak": "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"",
            "inputHint": "expectingInput"
          }
        ],
        "ExpectedTTSAudioResponseDuration": [2300, 8200],
        "ExpectedResponseLatency": 3000
      }
    ]
  }
]
```

The test includes one dialog to verify the bot's greeting. It has the following fields:
* DialogId - A unique identifier string for the dialog. You can use an integer counter (as we do here, starting with the value "0"), a random GUID or any unique string. The test logs will use this identifier.
* Description - A free-form string describing what this dialog does.
* Turns - A dialog with the bot may contain several turns (user request followed by bot reply). Here we list one turn, and it is a greeting turn in the sense that we only specify the expect bot reply. We do not specify a preceding user request.
    * TurnId - A non-negative integer that enumerates the turns, starting from 0.
    * ExpectedResponses - This is an array that lists the bot reply activities in the order you expect the client to receive them. Each activity is JSON string that follows the [Bot-Framework Activity schema](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md). You only need to list the activity fields that you care about. In the example above, we expect the bot greeting to include two activities of type "message". We list values for "text", "speak", "inputHint" and "attachments". If the actual activities received have different values for these fields, the test will fail. Note that the tool first orders the bot responses based on the [activity timestamp](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#timestamp) field, before comparing to the expected bot responses.
    * ExpectedTTSAudioResponseDuration - An optional array of integers, specifying the expected duration in msec of the resulting Text-to-Speech (TTS) audio stream associated with each bot-reply activity. The length of this array must equal the length of the ExpectedResponses array. If there is no TTS audio associated with any of the bot-reply activities, you can enter a value of -1. The duration does not need to be exact. When the tool compares expected duration to actual duration, there is a tolerance defined by the application configuration setting TTSAudioDurationMargin. Its default value is 200 msec.
    * ExpectedResponseLatency - Specifies the maximum expected duration to receive all the bot responses, in msec. If the actual duration is larger than the value specified here, the test will fail.

## Step 4: Run your test

Now that you created both the AppConfig.json and the TestConfig.json files, it's time to run the tool. Do this:

```cmd
VoiceAssistantTest.exe AppConfig.json
```

If your core bot is reachable, the test should pass and you should logs on the console similar to the following:

```cmd
VoiceAssistantTest Information: 0 : Parsing AppConfig.json
VoiceAssistantTest Information: 0 : Validating file TestConfig.json
VoiceAssistantTest Information: 0 : Processing file TestConfig.json
    VoiceAssistantTest Information: 0 : [6:56:37 PM] Running DialogId 0, description "Testing core bot greeting"
        VoiceAssistantTest Information: 0 : [6:56:37 PM] Running Turn 0
            VoiceAssistantTest Information: 0 : Task status RanToCompletion. Received 2 activities, as expected (configured to wait for 2):
            VoiceAssistantTest Information: 0 : [0]: Latency 570 msec
            VoiceAssistantTest Information: 0 : [1]: Latency 604 msec
            VoiceAssistantTest Information: 0 : Turn passed (DialogId 0, TurnID 0)
    VoiceAssistantTest Information: 0 : DialogId 0 passed
VoiceAssistantTest Information: 0 : ********** TEST PASS **********
```
Notice that these files and folder were crated by the tool:
* [VoiceAssistantTest.log](core-bot-examples/greeting/VoiceAssistantTest.log) - A capture of the same logs you see on the console
* [VoiceAssistantTestReport.json](core-bot-examples/greeting/VoiceAssistantTestReport.json) - A short summary report of the test. For each one of the test files specified in AppConfig.json (here we only have one: TestConfig.json) it lists the pass rate (how many dialogs succeeded), and enumerates all the dialogs with their pass/fail result.
* [TestConfigOutput\TestConfigOutput.json](core-bot-examples/greeting/TestConfigOutput/TestConfigOutput.json) - A detailed result of executing the dialogs specified in the TestConfig.txt file (here we only have on bot-greeting dialog). It lists all the expected values and the actual values observed when executing the dialog. At the end it also lists the pass/fail result for each validation step:
    * ResponseMatch - True if ActualResponses matched ExpectedResponses
    * UtteranceMatch - True if expected speech recognition matched actual speech recognition result. This is only relevant for turns with WAV file input (to be discussed further down)
    * TTSAudioResponseDurationMatch - True if ActualTTSAudioResponseDuration values are all within the tolerance range of ExpectedTTSAudioResponseDuration.
    * ResponseLatencyMatch - True if ActualResponseLatency is less or equal ExpectedResponseLatency.
    * Pass - True if all of the above are true.
* [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-0-0.WAV](core-bot-examples/greeting/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-0-0.WAV) - This is the TTS audio stream associated with the first bot response ("welcome to Bot Framework"). The WAV file name is a concatenation of 
    - test configuration name ("TestConfig" here), followed by 
    - the fixed string "BotResponse", then
    - "Dialog ID" string ("0" here), then
    - Turn ID ("0" here), then
    - The bot reply index 
* [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-0-1.WAV](core-bot-examples/greeting/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-0-1.WAV) - TTS audio stream associates with the second bot response ("What can I help you with today? ...")

## Step 5: Make your test fail

Experiment with changing test configurations to see an example of test failure and the corresponding error message. Here are some suggestions:
* **Missing required field**. Remove the "Region" field in AppConfig.json, or the "DialogID" in TestConfig.json, re-run the test and see the "required property not found" failure message.
* **Bot-response activity mismatch**. Change the "speak" field of the first expected bot-response in TestConfig.json from "Welcome to Bot Framework!" to just "Welcome". Re-run the test and see the "activity field mismatching values" failure message.
* **Latency failure**. Update the "ExpectedResponseLatency" field in TestConfig.json to a very small, unrealistic value (e.g. 10 msec), re-run the test and see the "latency mismatch" failure message.

## Step 6: Add turns with WAV input

Now that you've verified the expected bot greeting, update your test configuration file to specify a particular dialog you want to have with the bot following the greeting. Open your TestConfig.json file, replace its entire content with the one given below and save the result:
```json
[
  {
    "DialogID": "0",
    "Description": "Testing core bot greeting",
    "Turns": [
      {
        "TurnID": 0,
        "ExpectedResponses": [
          {
            "type": "message",
            "speak": "Welcome to Bot Framework!",
            "inputHint": "acceptingInput",
            "attachments": [
              {
                "contentType": "application/vnd.microsoft.card.adaptive"
              }
            ]
          },
          {
            "type": "message",
            "text": "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"",
            "speak": "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"",
            "inputHint": "expectingInput"
          }
        ],
        "ExpectedTTSAudioResponseDuration": [2300, 8200],
        "ExpectedResponseLatency": 3000
      },
      {
        "TurnID": 1,
        "WavFile": "BookFlight.wav",
        "Utterance": "Book a flight from Seattle to New York on January 2nd, 2020",
        "ExpectedResponses": [
          {
            "type": "message",
            "text": "Please confirm, I have you traveling to: New York from: Seattle on: 2020-01-02. Is this correct? (1) Yes or (2) No",
            "speak": "Please confirm, I have you traveling to: New York from: Seattle on: 2020-01-02. Is this correct?",
            "inputHint": "expectingInput"
          }
        ],
        "ExpectedTTSAudioResponseDuration": [9221],
        "ExpectedResponseLatency": 1000,
      },
      {
        "TurnID": 2,
        "WavFile": "Yes.wav",
        "Utterance": "Yes",
        "ExpectedResponses": [
          {
            "type": "message",
            "text": "I have you booked to New York from Seattle on 2nd January 2020",
            "speak": "I have you booked to New York from Seattle on 2nd January 2020",
            "inputHint": "ignoringInput"
          },
          {
            "type": "message",
            "text": "What else can I do for you?",
            "speak": "What else can I do for you?",
            "inputHint": "expectingInput"
          }
        ],
        "ExpectedTTSAudioResponseDuration": [5100, 2200],
        "ExpectedResponseLatency": 1000,
      }
    ]
  }
]
```
The first turn (TurnID 0 -- bot greeting verification) has not changed. Turns 1 and 2 were added and in each one audio is streamed up to Direct Line Speech channel from pre-recorded WAV files on disk, followed by verification of the bot response. Turns 1 & 2 have these additional fields set, compared to turn 0:
* WavFile - The name of the WAV file. A single format is supported: Raw PCM, 16khz sample rate, mono, 16 bit per sample, signed integer
* Utterance - This is an optional field when WavFile is specified. It should include the expected speech recognition result on the audio stream in the WAV file. The test will fail if Direct Line Speech channel does not return this recognized text.

The two WAV files are provided in the repository, so you do not need to record them. Copy "BookFlight.wav" and "Yes.wav" from the docs\core-bot-examples\multi-turn folder into your test folder. Once you are done, you should have the following in your test folder:
* VoiceAssistantTest.exe
* [AppConfig.json](core-bot-examples/multi-turn/AppConfig.json)
* [TestConfig.json](core-bot-examples/multi-turn/TestConfig.json)
* [BookFlight.wav](core-bot-examples/multi-turn/BookFlight.wav) ("Book a flight from Seattle to New York on January 2nd, 2020")
* [Yes.wav](core-bot-examples/multi-turn/Yes.wav)

Run the tool:
```cmd
VoiceAssistantTest.exe AppConfig.json
```
If everything is configured correctly, the test should succeed and you should see logs on screen similar to the following:

```cmd
VoiceAssistantTest Information: 0 : Parsing AppConfig.json
VoiceAssistantTest Information: 0 : Validating file TestConfig.json
VoiceAssistantTest Information: 0 : Processing file TestConfig.json
    VoiceAssistantTest Information: 0 : [5:21:29 PM] Running DialogId 0, description "Testing core bot greeting"
        VoiceAssistantTest Information: 0 : [5:21:29 PM] Running Turn 0
            VoiceAssistantTest Information: 0 : Task status RanToCompletion. Received 2 activities, as expected (configured to wait for 2):
            VoiceAssistantTest Information: 0 : [0]: Latency 465 msec
            VoiceAssistantTest Information: 0 : [1]: Latency 465 msec
            VoiceAssistantTest Information: 0 : Turn passed (DialogId 0, TurnID 0)
        VoiceAssistantTest Information: 0 : [5:21:30 PM] Running Turn 1
            VoiceAssistantTest Information: 0 : [5:21:30 PM] Start listening
            VoiceAssistantTest Information: 0 : [5:21:30 PM] Session Started event received. SessionId = 164e9ccd-f4e8-422b-80c6-2ea8f28fc5e3
            VoiceAssistantTest Information: 0 : [5:21:33 PM] Recognized event received. SessionId = 6bd60a10e3cf499d8ce8a515ea24bdde
            VoiceAssistantTest Information: 0 : Task status RanToCompletion. Received 1 activities, as expected (configured to wait for 1):
            VoiceAssistantTest Information: 0 : [0]: Latency 199 msec
            VoiceAssistantTest Information: 0 : Turn passed (DialogId 0, TurnID 1)
        VoiceAssistantTest Information: 0 : [5:21:33 PM] Running Turn 2
            VoiceAssistantTest Information: 0 : [5:21:33 PM] Start listening
            VoiceAssistantTest Information: 0 : [5:21:33 PM] Session Stopped event received. SessionId = 6bd60a10e3cf499d8ce8a515ea24bdde
            VoiceAssistantTest Information: 0 : [5:21:33 PM] Session Started event received. SessionId = f1d06296-30fb-49fb-a338-1c41e322841e
            VoiceAssistantTest Information: 0 : [5:21:34 PM] Recognized event received. SessionId = 6bd60a10e3cf499d8ce8a515ea24bdde
            VoiceAssistantTest Information: 0 : Task status RanToCompletion. Received 2 activities, as expected (configured to wait for 2):
            VoiceAssistantTest Information: 0 : [0]: Latency 201 msec
            VoiceAssistantTest Information: 0 : [1]: Latency 686 msec
            VoiceAssistantTest Information: 0 : Turn passed (DialogId 0, TurnID 2)
    VoiceAssistantTest Information: 0 : DialogId 0 passed
VoiceAssistantTest Information: 0 : ********** TEST PASS **********
```
The following files wil be created, with results similar to the ones linked here:
* [VoiceAssistantTest.log](core-bot-examples/multi-turn/VoiceAssistantTest.log)
* [VoiceAssistantTestReport.json](core-bot-examples/multi-turn/VoiceAssistantTestReport.json)
* [TestConfigOutput\TestConfigOutput.json](core-bot-examples/multi-turn/TestConfigOutput/TestConfigOutput.json) 
* TTS audio streams from Turn 0 (bot greeting):
    * [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-0-0.WAV](core-bot-examples/multi-turn/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-0-0.WAV) (1st bot reply activity - "welcome to Bot Framework")
    * [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-0-1.WAV](core-bot-examples/multi-turn/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-0-1.WAV) (2nd bot reply activity - "What can I help you with today? ...")
* TTS audio stream from Turn 1:
    * [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-1-0.WAV](core-bot-examples/multi-turn/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-1-0.WAV) ("Please confirm, I have you traveling to...")
* TTS audio streams from Turn 2:
    * [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-2-0.WAV](core-bot-examples/multi-turn/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-2-0.WAV) ("I have you booked to New York from Seattle ...")
    * [TestConfigOutput\WAVFiles\TestConfig-BotResponse-0-2-1.WAV](core-bot-examples/multi-turn/TestConfigOutput/WAVFiles/TestConfig-BotResponse-0-2-1.WAV) ("What else can I do for you?")

## Step 7: Try text input instead of WAV input

There are three ways to specify user input in each turn:
1. Audio stream read from WAV file (as if the user was speaking to a microphone), per the example above. In this case you will need to populate these fields:
    * WavFile (required)
    * Utterance (optional) - To validate speech recognition result
    
    Specifying Activity input is now allowed in this case.
2. Text input (as if the user were typing in a chat box)
    * Utterance (required) - Text to be sent to the bot as a Bot-Framework activity of type "message".

    Specifying WavFile or Activity fields are not allowed in this case.
3. Sending a Bot-Framework Activity as a JSON string. This is used for send any other data to the bot.
    * Activity - A JSON string containing a valid Bot-Framework Activity. 

    Specifying WavFile or Utterance fields is not allowed in this case.

To test text input instead of WAV file input, delete the WavFile field in Turn 1 and/or Turn 2 in TestConfig.json. Leave the Utterance field as is, as it will now indicate the test to sent up to the bot. Rerun the test. Notice in the logs that instead of "Start listening" (associated with audio stream input) you now see "Activity sent to channel" (associated with the text input). Otherwise the test is identical.

## Step 7: Next steps
* Review all supported fields in the [application configuration file](../README.md#application-configuration-file), and the [application configuration template](json-templates/app-config-template.json)
* Review all supported fields in the [test configuration file](../README.md#test-configuration-file), and the [test configuration template](json-templates/test-config-template.json)
* Read the [topics section](../README.md#topics)