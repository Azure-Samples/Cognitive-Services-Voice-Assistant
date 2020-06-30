
# Voice Assistants Cloud Connection Logic

This article discusses options for when to connect your client application to [Direct Line Speech](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/direct-line-speech) (DLS) channel and your bot, and how long to keep that connection open. It also discusses policies for Cognitive Services Speech token refresh, as it is recommended that client applications never handle speech keys.

The text below assumes you are deploying a bot. However, similar considerations apply if you host your dialog using [Custom Commands](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands).

The text below and C# pseudo code assumes your client application uses keyword activation. The considerations with regards to when to open connection and how long to keep it open are similar if your application uses microphone button ("push to talk") activation only.

## Connection options

### Option 1 - Always connected

A web-socket between the client application, DLS channel and your bot is established when the client application opens. Web-socket connections remains open until the client application closes. 

Pros
* Lowest latency - There is no need to create the web-socket connection at the moment the user says the keyword or presses the microphone button.
* State of conversation can remain in memory in the bot instance between dialogs

Cons
* Always-on connection with an instance of your bot is present even for a client application that is not active. This prevents scaling up and down resouces based on actual usage.

#### On client start up
Set up a web-socket connection with DLS and bot using DialogServiceConnector. Start listening for keyword. Audio will not stream to the cloud until keyword is detected:
```csharp
// Set up cloud connection, reading audio from default microphone
var botConfig = BotFrameworkConfig.FromSubscription("YourSpeechSubscriptionKey", "YourServiceRegion");
botConfig.Language = "en-US";
connector = new DialogServiceConnector(botConfig);
// Implement event handlers
connector.ActivityReceived += (sender, activityReceivedEventArgs) => { ... };
...
// Start listening for keyword
var model = KeywordRecognitionModel.FromFile("YourKeywordModelFileName");
connector.StartKeywordRecognitionAsync(model);
```
#### On keyword activation

Audio automatically starts streaming to the cloud. Bot gets the recognized text and replies with one or more Bot-Framework activities. Client application receives and processes the activities, including playback of attached TTS audio stream. Client application may initiate a second turn by calling ListenOnceAsync() based on bot hint.

#### On client shutdown
Close the web socket connection with DLS and bot
```csharp
// Unregister event handlers
connector.ActivityReceived -= ...
// Close cloud connection
connector.Dispose();
```

### Option 2 - On-demand connection for the current dialog

A web-socket between the client application, DLS and your bot is established when on-device keyword detection fires, or on button press ("push to talk"). It closes after the current dialog is done.

Pros
* Higher latency - Connection needs to be established after keyword activation or microphone button press

Cons
* Bot instances are only spun up to handle active clients
* If state preservation is needed between dialogs with a client, state needs to be stored at the end of a dialog and fetched at the beginning of the next dialog.

#### On client start up
Set up on-device keyword recognizer, without cloud connection:
```csharp
var micAudioConfig = AudioConfig.FromDefaultMicrophoneInput();
var keywordRecognizer = new KeywordRecognizer(micAudioConfig);
// Implement event handlers
keywordRecognizer.Recognized += (sender, keywordRecognitionEventArgs) => { ... };
...
 // Start listening for keyword (no cloud connection needed!)
var model = KeywordRecognitionModel.FromFile("YourKeywordModelFileName");
keywordRecognizer.RecognizeOnceAsync(model)
```
#### On keyword activation
Set up a web-socket connection with DLS and bot using DialogServiceConnector, and stream audio to the cloud starting from the keyword. Do dialog turns. Close web-socket at the end of the dialog:
```csharp
// In the Recognized event handler:
var result = keywordRecognitionEventArgs.Result;
if (result.Reason == ResultReason.RecognizedKeyword)
{
    // Get an audio data stream which starts from right before the keyword
    var audioDataStream  = AudioDataStream.FromResult(result);
    // Create a dialog connector using audio input stream
    var audioInputStream = PushAudioInputStream.create();
    var streamAudioConfig = AudioConfig.fromStreamInput(audioInputStream);
    var botConfig = BotFrameworkConfig.FromSubscription("YourSpeechSubscriptionKey", "YourServiceRegion");
    var connector = new DialogServiceConnector(botConfig, streamAudioConfig);
    // Implement event handlers
    connector.ActivityReceived += (sender, activityReceivedEventArgs) => { ... };
    // Start a thread here to pump audio from audioDataStream to audioInputStream... This will simplified in future version of Speech SDK
    ...
    // Do dialog turns
    connector.ListenOnceAsync()
    ...
    // When the dialog is done release dialog resources and close cloud connection
    audioDataStream.detachInput();
    audioDataStream.Dispose();
    connector.ActivityReceived -= ...
    connector.Dispose()
    streamAudioConfig.Dispose();
    audioInputStream.Dispose();
}
```
#### On client shutdown
Stop listening for keyword:
```csharp
keywordRecognizer.Recognized -= ...
keywordRecognizer.Dispose();
```

### Option 3 - On-demand connection, stays open for a pre-defined amount of time

A web-socket between the client application, DLS  and your bot is established when on-device keyword detection fires, or on button press ("push to talk"). It remains open for a pre-determined amount of time beyond the end of the current dialog.

This is a compromise between Options 1 and 2. If your scenario typically calls for a user to initiate several dialogs with your bot in a short period of time, this may be the best solution.

## Using speech tokens

TBD
