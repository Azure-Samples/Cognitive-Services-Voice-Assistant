# Listen for voice activation

Windows uses a lower-power, always-on keyword detector to detect when a user speaks a registered keyword. This stage of voice activation is referred to as keyword detection. These are the steps to for a voice assistant application to register a keyword with Windows and listen for keyword detection.

## 1. Ensure that the microphone is available and accessible, then monitor its state

Windows needs a microphone to be present and accessible to be able to detect a voice activation. [AudioCaptureControl](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AudioInput/AudioCaptureControl.cs) retrieves and monitors the state of audio input device, input volume, and whether the input is reported as muted. It also contains a reference to the [AppCapability](https://docs.microsoft.com/en-us/uwp/api/windows.security.authorization.appcapabilityaccess.appcapability?view=winrt-18362), which reflects whether the user has accepted microphone privacy settings and notifies when the user's preference changes. [Call RequestAccessAsync](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/MainPage.xaml.cs#L72) on the MicrophoneCapability to launch a prompt for microphone access. Note that you'll need to include "microphone" as a capability in the app manifest file if you're using AudioCaptureControl in a different project.

## 2. Register the application with the background service

In order for Windows to launch the application in the background, the application needs to be registered with the Background Service. This is implemented in [MVARegistrationHelper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/MVARegistrationHelpers.cs)'s IsBackgroundTaskRegistered property. Register your application by setting IsBackgroundTaskRegistered to true. This should be set on application start.

## 3. Unlock the Limited Access Feature

Use your Microsoft-provided Limited Access Feature key to unlock the voice assistant features on Windows. This is implemented in [MVARegistrationHelper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/MVARegistrationHelpers.cs)'s UnlockLimitedAccessFeature. Change the hardcoded values in the method to your own credentials. Note: the credentials in this repo will work for the UWP Voice Assistant sample, but won't work for an app with a different package identity.

## 4. Register the keyword for the application

The application needs to register itself, its keyword model, and its language with Windows to enable voice activation. Windows uses this information to listen for the keyword using the provided model and launch the correct application on detection. This is implemented in [KeywordRegistration](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/KeywordRegistration.cs). Register your application by [creating an instance of KeywordRegistration](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs#L48) with the proper inputs. You can update these values with the UpdateKeyword method. This should be executed on application start.

## 5. Verify that the voice activation setting is enabled

To use voice activation, a user needs to enable voice activation for their system and enable voice activation for their application. You can find the setting under "Voice activation privacy settings" in Windows settings. To check the status of the voice activation setting in your application, you must have an instance of the ActivationSignalDetectionConfiguration from the Windows SDK. In the sample app, the current ActivationSignalDetectionConfiguration is retrieved by calling GetOrCreateKeywordConfigurationAsync on the instance of [KeywordRegistration](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs#L48) from step 4. The [AvailabilityInfo](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/UIAudioStatus.cs#L128) field on the ActivationSignalDetectionConfiguration contains an enum value that describes the state of the voice activation setting. This state is processed and reflected in the UI in the [UIAudioStatus](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/UIAudioStatus.cs#L124) class.

## 6. Retrieve a ConversationalAgentSession to register the app with the MVA system

The ConversationalAgentSession is a class in the Windows SDK that allows your app to update the Windows with the app state (Idle, Detecting, Listening, Working, Speaking) and receive events, such as activation detection and system state changes such as the screen locking. Retrieving an instance of the AgentSession also serves to register the application with Windows as activatable by voice. It is best practice to maintain one reference to the ConversationalAgentSession. In the sample app, this instance is managed by the [AgentSessionManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionManager.cs) and can be retrieved using GetSessionAsync. This should be called on application start.

## 7. Listen to the two activation signals: the OnBackgroundActivated and OnSignalDetected

Windows will signal your app when it detects a keyword in one of two ways. If the app is not active (ie you do not have a reference to a non-disposed instance of ConversationalAgentSession), then it will launch your app and call the OnBackgroundActivated method in the [App.xaml.cs](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/App.xaml.cs) file of your application. If the BackgroundActivatedEventArgs' TaskInstance<span>.Task.Name matches "AgentBackgroundTrigger", it was triggered by keyword detection. The application needs to override this method and retrieve an instance of ConversationalAgentSession to signal to Windows that is now active. 

If the app is active (i.e. has a reference to a non-disposed instance of ConversationalAgentSession), then Windows will signal the app through the SignalDetected event in the ConversationalAgentSession. The app needs to add a handler to this event on startup for signals that occur while the app is already active. In the sample app, this event is wrapped in [AgentSessionWrapper](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionWrapper.cs#L38) and handled in [AgentSessionManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/AgentSessionManager.cs).

## Summary

On startup, a Windows voice agent should

- verify that an audio input device is available and accessible
- register itself with the Background Service
- register its keyword
- verify that voice activation is enabled
- start listening for an activation signal

## Next step: Keyword Verification

After completing this step, your application will be activated and receive a signal when its keyword is spoken. The next step is to verify that the signal that was detected was valid. In the sample app, each activation signal eventually leads to a call to [DialogManager](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/UWPVoiceAssistantSample/DialogManager.cs).HandleSignalDetected, which initiates keyword verification.