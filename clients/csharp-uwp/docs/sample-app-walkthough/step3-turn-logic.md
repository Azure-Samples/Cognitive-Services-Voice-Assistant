# Turn Logic

Once the activation signal has been verified, the next step is to begin a dialog between the voice agent and the user. A "turn" is a user input to the bot through voice or text and the corresponding response from the bot. Thus, a conversation between a user and a bot can be thought of as a sequence of turns. This document walks through how to complete a turn using Direct Line Speech and how to update the Windows Conversational Agent system with the state of the voice agent completing the turns.

## 12. Bot interaction
The IDialogBackend interface was designed to allow the use of any dialog service with the other components in the UWP Voice Assistant sample. It contains events for keyword signals, speech recognition output, and generic output in the form of the DialogResponse class. 

The sample app's implementation of this interface, DirectLineSpeechDialogBackend, uses the Speech Services SDK and Direct Line Speech which provide an easy way to build a dialog service and a straightforward library to interface with it.

The Speech Services SDK's DialogServiceConnector object invokes a sequence of events signaling each step of keyword verification, speech recognition, and agent responses. The events, the ResultReaon from the event arguments, and a short description are summarized in the following table.

| Step | `DialogServiceConnector` event | `ResultReason` | Description |
|---|---|---|---|
| 1 | `SpeechRecognizing` | `RecognizingKeyword` | Keyword confirmed locally |
| 2 | `SessionStarted` | | Audio begins flowing to the Speech Service
| 3 | `SpeechRecognized` | `RecognizedKeyword` | Keyword verified in the cloud |
| 4 | `SpeechRecognizing` | `RecognizingSpeech` | In-progress speech-to-text as the user is speaking |
| ... | * | * | Speech continues |
| 5 | `SpeechRecognized` | `RecognizedSpeech` | Final speech-to-text result that is also sent to the bot |

The following is a walkthrough of the code path to process an utterance from the user while the app is listening followed by a text and audio response from the bot, altogether referred to as a "turn".

### Turn during first activation
While completing 3rd stage keyword verification, Direct Line Speech will begin to convert the activation audio into text. This reduces latency between keyword confirmation and the bot response. In practice, this means that, after providing audio to the DialogServiceConnector, the expected result is a [DialogServiceConnector.SpeechRecognized event for the keyword confirmation](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L127) followed by a [DialogServiceConnector.SpeechRecognized event with the text version of what the user said when they activated the voice agent](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L131). Direct Line Speech also immediately sends this text to the configured bot and, upon receiving a response, converts the text response from the bot into audio before finally sending the full response back to the application through the [DialogServiceConnector.ActivityReceived](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L148) event.

All of these events are surfaced by the DirectLineSpeechDialogBackend and handled in DialogManager. For activities, DialogManager uses a queue, the [DialogResponseQueue](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogResponseQueue.cs), to make sure activities are executed sequentially. For example, if a bot responded with two activities, the second activity's text and audio would be queued, then dequeued after the first activity finishes playing its audio and displaying its text.

### Subsequent Turns
If a bot requests a response to the last turn (like if it asked a question and expects the user to answer) then the DialogResponse.FollowupTurnIndicated will be set to true. When this field is true, the app just needs to start listening for user input again as if it received an activation. 

DialogManager saves the value of the DialogResponse.FollowupTurnIndicated field during its OnActivityReceived event handler. When the DialogResponseQueue indicates that the last turn has finished executing, DialogManager calls FinishTurnAsync. If FollowupTurnIndicated is true, then it initiates listening with StartTurnAsync and starts providing audio to the DirectLineSpeechDialogBackend again.

## Reporting Agent State
Throughout activation, keyword verification, and the bot interaction, a UWP voice agent application needs to update the Windows Conversational Agent system with its state. The Conversational Agent System uses this information to manage multiple voice agents running at the same time, system state and lighting, and other important functions. There are 5 possible states:
- Inactive: The application is inactive
- Detecting: Keyword verification is in progress
- Listening: The voice agent is listening to the user
- Working: The agent is taking action based on what the user said
- Speaking: The voice agent is playing an audio response to the user

The application reports changes in state to the Windows Conversational Agent system through the ConversationalAgentSession.RequestAgentStateChangeAsync method. In the sample app, these state changes are executed in DialogManager through [DialogManager.ChangeAgentStateAsync](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogManager.cs#L358).

## Handling Above Lock Activation
To enable above lock activation, the application needs to close when the screen locks and adjust its UI when opened on the lock screen. The ConversationalAgentSession.IsUserAuthenticated field reflects whether the screen is unlocked. The sample application uses this field across the app. The ActivationSignalDetectionConfiguration is used to track the state of the lock screen. The SystemStateChanged event fires with specific event arguments when the lock screen changes state. In the sample app, the change in lock screen state is handled in [AgentSessionManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionManager.cs#L48).