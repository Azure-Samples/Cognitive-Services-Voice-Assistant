# Understanding the sample app
This is a detailed breakdown of the steps to build a Windows voice assistant along with an explanation of how the UWP Voice Assistant sample implements each step. Please read the [readme](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/README.md) for this sample for more information on MVA, DLS, and the UWP Voice Assistant Sample.

## Listen for voice activation
MVA uses a lower-power, always-on keyword detector to detect when a user speaks a registered keyword. This is referred to as "1st stage keyword detection". These are the steps to for a voice assistant application to register a keyword with MVA and listen for 1st stage keyword detection.

### 1. Ensure that the microphone is available and accessible, then monitor its state
MVA needs a microphone to be present and accessible to be able to detect a voice activation. [AudioCaptureControl](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AudioInput/AudioCaptureControl.cs) retrieves and monitors the state of audio input device, input volume, and whether the input is reported as muted. It also contains a reference to the [AppCapability](https://docs.microsoft.com/en-us/uwp/api/windows.security.authorization.appcapabilityaccess.appcapability?view=winrt-18362), which reflects whether the user has accepted microphone privacy settings and notifies when the user's preference changes. [Call RequestAccessAsync](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/MainPage.xaml.cs#L72) on the MicrophoneCapability to launch a prompt for microphone access. Note that you'll need to include "microphone" as a capability in the app manifest file if you're using AudioCaptureControl in a different project.

### 2. Register the application with the background service
In order for MVA to launch the application in the background, the application needs to be registered with the Background Service. This is implemented in [MVARegistrationHelper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/MVARegistrationHelpers.cs)'s IsBackgroundTaskRegistered property. Register your application by setting IsBackgroundTaskRegistered to true. This should be set on application start.

### 3. Unlock the Limited Access Feature
Use your Microsoft-provided Limited Access Feature key to unlock the MVA feature. This is implemented in [MVARegistrationHelper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/MVARegistrationHelpers.cs)'s UnlockLimitedAccessFeature. Change the hardcoded values in the method to your own credentials. Note: the credentials in this repo will work for the UWP Voice Assistant sample, but won't work for an app with a different package identity.

### 4. Register the keyword for the application
The application needs to register itself, its keyword model, and its language with MVA. MVA uses this information to listen for the keyword using the provided model and launch the correct application on detection. This is implemented in [KeywordRegistration](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/KeywordRegistration.cs). Register your application by [creating an instance of KeywordRegistration](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs#L48) with the proper inputs. You can update these values with the UpdateKeyword method. This should be executed on application start.

### 5. Verify that the voice activation setting is enabled
To use voice activation, a user needs to enable voice activation for their system and enable voice activation for their application. You can find the setting under "Voice activation privacy settings" in Windows settings. To check the status of the voice activation setting in your application, you must have an instance of the ActivationSignalDetectionConfiguration from the Windows SDK. In the sample app, retrieve the current ActivationSignalDetectionConfiguration by calling GetOrCreateKeywordConfigurationAsync on the instance of [KeywordRegistration](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs#L48) from step 4. The [AvailabilityInfo](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/UIAudioStatus.cs#L128) field on the ActivationSignalDetectionConfiguration contains an enum value that describes the state of the voice activation setting.

### 6. Retrieve a ConversationalAgentSession to register the app with the MVA system
The ConversationalAgentSession is a class in the Windows SDK that allows your app to update the MVA system with the app state (Idle, Detecting, Listening, Speaking) and receive events, such as activation detection and system state changes such as the screen locking. Retrieving an instance of the AgentSession also serves to register the application with MVA as activatable by voice. It is best practice to maintain one reference to the ConversationalAgentSession. In the sample app, this instance is managed by the [AgentSessionManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionManager.cs) and can be retrieved using GetSessionAsync. This should be called on application start.

### 7. Listen to the two activation signals: the OnBackgroundActivated and OnSignalDetected
MVA will signal your app when it detects a keyword in one of two ways. If the app is not active (ie you do not have a reference to a non-disposed instance of ConversationalAgentSession), then it will launch your app and call the OnBackgroundActivated method in the [App.xaml.cs](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs) file of your application. If the BackgroundActivatedEventArgs' TaskInstance<span>.Task.Name matches "AgentBackgroundTrigger", it was triggered by an MVA activation. The application needs to override this method and retrieve an instance of ConversationalAgentSession to signal to MVA that is now active. 

If the app is active (i.e. has a reference to a non-disposed instance of ConversationalAgentSession), then MVA will signal the app through the SignalDetected event in the ConversationalAgentSession. The app needs to add a handler to this event on startup for signals that occur while the app is already active. In the sample app, this event is warpped in [AgentSessionWrapper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionWrapper.cs#L38) and handled in [AgentSessionManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionManager.cs).

In both cases, the activation eventually leads to a call to [DialogManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogManager.cs).HandleSignalDetected, which executes the code to complete keyword verification.

## Keyword Verification
Once a voice agent application receives a signal from MVA, the next step is to verify that the keyword detection was valid. The model used in 1st stage keyword detection is low-power, but as a result, it has a high false accept rate. For the best experience, the audio from the 1st stage detection needs to be verified. In the sample application, this is done in two stages using DLS: 2nd stage detection, which uses a higher-power, more accurate keyword detector (running locally), followed by 3rd stage detection, which sends the signal to the cloud to be verified by an even more accurate and resource-intensive keyword detection model. Fortunately, most of this complexity is encompassed by the Speech SDK and only requires proper setup.

The following steps describe how to complete keyword verification with DLS through the Speech SDK and how to interact with MVA during verification.

### 8. Retrieve activation audio
Create an [AudioGraph](https://docs.microsoft.com/en-us/uwp/api/windows.media.audio.audiograph) and pass it to the CreateAudioDeviceInputNodeAsync of the ConversationalAgentSession. This will load the graph's audio buffer with the audio *starting approximately 3 seconds before MVA detected a keyword*. In the sample app, [AgentAudioInputProvider](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AudioInput/AgentAudioInputProvider.cs)'s InitializeFromAgentSessionAsync method retrieves the audio from the ConversationalAgentSession. AgentAudioInputProvider fires an event, DataAvailable, when the audio is ready as a stream of bytes. 

Note: In the sample app, the first 2 seconds of the audio are [trimmed](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogManager.cs#L308) before being sent to Speech Services. This is because the keyword verification may fail if the full 3 seconds from pre-detection are included in the audio.

### 9. Verify the keyword
Using Cognitive Speech Services or your own system, process the activation audio to verify whether it contains a keyword. The sample app uses the Speech SDK to interface with Speech Services. To use a different keyword verification system, direct the audio from the AgentAudioInputProvider's DataAvailable event to the alternative service and trigger step 10 on keyword confirmation. 

The following goes into further detail on how the sample application uses Direct Line Speech for keyword verification.
#### Initiate conversation start on signal from MVA
The DialogManager class manages the lifecycle of a bot interaction, accepting commands to start keyword verification and speech recognition and emitting events with recognition results and bot responses. In step 7, after receiving a signal from MVA, the sample app calls [DialogManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogManager.cs).HandleSignalDetection, which initiates keyword verification. 
#### Pass input audio to Direct Line Speech
DialogManager completes step 8 by initializing the AgentAudioInputProvider and passing it to an instance of [DirectLineSpeechDialogBackend](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs) through DirectLineSpeechDialogBackend.SetAudioSource. The DirectLineSpeechDialogBackend class manages the Speech SDK classes used to interface with Direct Line Speech. 
#### Initiate keyword verification
When the AgentAudioInputProvider fires the DataAvailable event, the bytes from the activation audio are [passed to the Speech SDK](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L192) and analyzed by the local 2nd stage keyword detector.
#### Handle 2nd stage keyword verification
When the 2nd stage keyword detector detects a keyword, the DialogServiceConnector object in DirectLineSpeechDialogBackend will fire the SpeechRecognizing event with the result reason ResultReason.RecognizingKeyword. Note: there is no "keyword rejected" signal from the DialogServiceConnector for 2nd stage verification, so there must be a manually implemented timeout to cancel verification in case of failure. In the sample application, this "rejection timer" is implemented in [SignalDetectionHelper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/SignalDetectionHelper.cs).
#### Handle 3rd stage keyword verification
On successful 2nd stage keyword verification, the DialogServiceConnector automatically sends the audio to the cloud for 3rd stage keyword verification. When it verifies the keyword with cloud verification, the DialogServiceConnector object will fire the SpeechRecognized event with the result reason ResultReason.RecognizedKeyword. If cloud verification fails, the result reason will be ResultReason.NoMatch. In either case, the DialogManager surfaces these events as SignalConfirmed and SignalRejected.

### 10. If the keyword is verified, continue in the background or move to the foreground to display UI
The app is running in the background after it is activated by MVA, so it cannot display UI. For an audio-only app interaction, your application can simply stay in the background, play audio output, and listen to audio input. To display UI, call ConversationalAgentSession.RequestForegroundActivationAsync. In the sample app, this is called in [App.xaml.cs](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs)'s OnSignalConfirmed which is triggered by the SignalConfirmed event in DialogManager.

### Summary of activation in the UWP Voice Assistant Sample
When the app is started, the sample app 
- verifies that an audio input device is available and accessible
- registers itself with the Background Service
- registers its keyword
- verifies the voice activation is enabled

When the sample app receives an activation from MVA, the sample app
- retrieves an audio buffer that contains the "activation audio", including audio since 3 seconds before the 1st stage keyword detection
- provides the audio buffer as an input to the Speech SDK for 2nd stage keyword verification (and starting a timer in case of keyword rejection)
- reacts to keyword verification succeeding or failing by surfacing corresponding events.

Code path in the sample app:
- On first run MainPage.xaml.cs completes keyword registration, Background Service registration, mic verification, and voice activation setting verification.
- On 1st stage detection, DialogManager.HandleSignalDetected is called, either through App.xaml.cs's OnBackgroundActivated or the ConversationalAgentSession.SignalDetected event handler in AgentSessionManager
- HandleSignalDetected initializes the AgentAudioInputProvider with audio from the ConversationalAgentSession, creates a DirectLineSpeechDialogBackend instance, and passes the AgentAudioInputProvider through SetAudioSource
- DialogManager listens for speech recognition events from the DirectLineSpeechDialogBackend or a timeout event from SignalDetectionHelper and surfaces the events to the UI
- On keyword confirmation, the app uses the ConversationalAgentSession to request foreground activation and display UI

## Bot interaction
The IDialogBackend interface was designed to allow the use of any dialog service with the other components in the UWP Voice Assistant sample. It contains events for keyword signals, speech recognition output, and generic output in the form of the DialogResponse class. 

The sample app's implementation of this interface, DirectLineSpeechDialogBackend, uses the Speech SDK and Direct Line Speech which provide an easy way to build a dialog service and a straightforward library to interface with it.

The following is a walkthrough of the code path to process an utterance from the user while the app is listening followed by a text and audio response from the bot, altogether referred to as a "turn".

### Turn during first activation
While completing 3rd stage keyword verification, Direct Line Speech will begin to convert the activation audio into text. This reduces latency between keyword confirmation and the bot response. In practice, this means that, after providing audio to the DialogServiceConnector, the expected result is a [DialogServiceConnector.SpeechRecognized event for the keyword confirmation](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L127) followed by a [DialogServiceConnector.SpeechRecognized event with the text version of what the user said when they activated the voice agent](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L131). Direct Line Speech also immediately sends this text to the configured bot and, upon receiving a response, converts the text response from the bot into audio before finally sending the full response back to the application through the [DialogServiceConnector.ActivityReceived](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L148) event.

All of these events are surfaced by the DirectLineSpeechDialogBackend and handled in DialogManager. For activities, DialogManager uses a queue, the [DialogResponseQueue](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogResponseQueue.cs), to make sure activities are executed sequentially. For example, if a bot responded with two activities, the second activity's text and audio would be queued while the audio from the first activity is played then dequeued when the first activity finishes.

### Subsequent Turns
If a bot requests a response to the last turn (like if it asked a question and expects the user to answer) then the DialogResponse.FollowupTurnIndicated will be set to true. When this field is true, the app just needs to start listening for user input again as if it received an activation. 

DialogManager saves the value of the DialogResponse.FollowupTurnIndicated field during its OnActivityReceived event handler. When the DialogResponseQueue indicates that the last turn has finished executing, DialogManager calls FinishTurnAsync. If FollowupTurnIndicated is true, then it initiates listening with StartTurnAsync and starts providing audio to the DirectLineSpeechDialogBackend again.

## Reporting Agent State
Throughout activation, keyword verification, and the bot interaction, a UWP voice agent application needs to update MVA with its state. MVA uses this information to manage mutliple voice agents running at the same time, system state and lighting, and other important functions. There are 4 states to report to MVA.
- Inactive: The application is inactive
- Detecting: Keyword verification is in progress
- Listening: The voice agent is listening to the user
- Speaking: The voice agent is playing an audio response to the user

State changes are reported through the ConversationalAgentSession.RequestAgentStateChangeAsync method. In the sample app, these state changes are executed in DialogManager through [DialogManager.ChangeAgentStateAsync](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogManager.cs#L358).


