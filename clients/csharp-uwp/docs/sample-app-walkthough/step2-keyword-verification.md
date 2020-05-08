# Keyword Verification

Once a voice agent application receives a signal from Windows, the next step is to verify that the keyword detection was valid. The keyword model used by the Windows requires less power to run, but as a result, it has a high false accept rate. For the best user experience, the audio from the initial keyword detection needs to be verified.

In the sample application, this is done in two stages using Direct Line Speech (DLS):

- Local keyword verification, or 2nd stage verification, which uses a higher-power, more accurate keyword model running locally, followed by...
- Cloud keyword verification, or 3rd stage verification, which sends the signal to the cloud to be verified by an even more accurate and resource-intensive keyword verification model.

Fortunately, most of this complexity is encompassed by the Speech SDK and only requires proper setup. The following steps describe how to complete keyword verification with DLS through the Speech SDK and how to interact with Windows during verification.

## 8. Retrieve activation audio

Create an [AudioGraph](https://docs.microsoft.com/en-us/uwp/api/windows.media.audio.audiograph) and pass it to the CreateAudioDeviceInputNodeAsync of the ConversationalAgentSession. This will load the graph's audio buffer with the audio *starting approximately 3 seconds before Windows detected a keyword*. This additional leading audio is included to accommodate a wide range of keyword lengths and speaker speeds. In the sample app, [AgentAudioInputProvider](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AudioInput/AgentAudioInputProvider.cs)'s InitializeFromAgentSessionAsync method retrieves the audio from the ConversationalAgentSession. AgentAudioInputProvider fires an event, DataAvailable, when the audio is ready as a stream of bytes. 

Note: The leading audio included in the audio buffer can cause keyword verification to fail. To fix this issue, it is recommended to trim the beginning of the audio buffer before sending the audio for keyword verification. This initial trim should be tailored to each assistant. In the sample app, the first 2 seconds of the audio are trimmed in DialogManager before being sent to Speech Services.

## 9. Pass input audio to Direct Line Speech

DialogManager initializes the AgentAudioInputProvider and passes it to an instance of [DirectLineSpeechDialogBackend](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs) through DirectLineSpeechDialogBackend.SetAudioSource. The DirectLineSpeechDialogBackend class manages the Speech SDK classes used to interface with Direct Line Speech.

When the AgentAudioInputProvider fires the DataAvailable event, the bytes from the activation audio are [passed to the Speech SDK](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DirectLineSpeechDialogBackend.cs#L192) and analyzed by the local 2nd stage keyword detector.

## 10. Complete local keyword verification

When the local keyword verifier verifies the keyword is present in the activation audio, the DialogServiceConnector object in DirectLineSpeechDialogBackend will fire the SpeechRecognizing event with the result reason ResultReason.RecognizingKeyword. Note: there is no "keyword rejected" signal from the DialogServiceConnector for 2nd stage verification, so there must be a manually implemented timeout to cancel verification in case of failure. In the sample application, this "rejection timer" is implemented in [SignalDetectionHelper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/SignalDetectionHelper.cs).

## 11. Complete cloud keyword verification

On successful local keyword verification, the DialogServiceConnector automatically sends the audio to the cloud for 3rd stage keyword verification. When it verifies the keyword with cloud verification, the DialogServiceConnector object will fire the SpeechRecognized event with the result reason ResultReason.RecognizedKeyword. If cloud verification fails, the result reason will be ResultReason.NoMatch. In either case, the DialogManager surfaces these events as SignalConfirmed and SignalRejected.

## 12. If the keyword is verified, display UI

If the app is running in the background after it is activated by the Windows Conversational Agent System, it must move to the foreground in order to display UI. To move to the foreground, call ConversationalAgentSession.RequestForegroundActivationAsync. In the sample app, this is called in [App.xaml.cs](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs)'s OnSignalConfirmed which is triggered by the SignalConfirmed event in DialogManager.

## Overall code path in the sample app

- DialogManager.HandleSignalDetected initializes the AgentAudioInputProvider with audio from the ConversationalAgentSession, creates a DirectLineSpeechDialogBackend instance, and passes the AgentAudioInputProvider through SetAudioSource
- DialogManager listens for speech recognition events from the DirectLineSpeechDialogBackend or a timeout event from SignalDetectionHelper and surfaces the events to the UI
- On keyword confirmation, the app uses the ConversationalAgentSession to request foreground activation and display UI

## Summary

When a Windows voice agent receives an activation signal, it should

- retrieve an audio buffer that contains the "activation audio", including audio since 3 seconds before the 1st stage keyword detection
- provide the audio buffer as an input to the Speech SDK for local and cloud keyword verification (and starting a timer in case of keyword rejection)
- on successful verification, move the application to the foreground to display UI

## Next step: Turn logic

After this step, the voice agent application has verified that the activation signal is valid and can proceed with the voice agent interaction. The next step is to process input audio with a dialog service and output a response from a bot as audio, all while keeping the Windows Conversational Agent system updated with the state of the voice agent.