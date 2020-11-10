# Sample Voice Assistant Clients

## Overview

This folder includes sample Voice Assistant client application
* Voice Assistants clients use Microsoft's [Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) to manage microphone input and audio streaming to [Direct Line Speech Channel](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/direct-line-speech) service and connection to your [Bot-Framework](https://dev.botframework.com/) bot. 
* Speech SDK is used to manage connection to your [Custom Commands](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands) voice application.
* All client samples support multi-turn dialogs, managing microphone state [based on hints](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#input-hint) from the bot or your Custom Command application
* All client samples can be configured for [keyword activation](https://speech.microsoft.com/customkeyword) using [keyword model files in this repository](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/keyword-models) or new ones you create. 
* All client samples support playback of the Voice Assistant's text-to-speech response. 

For more details on each sample, see their individual folders.

## Client configuration

All sample clients read a JSON configuration file named ```config.json``` at runtime. This file defines your bot or Custom Command connection settings. JSON fields common to most samples are listed below. Different sample clients will have additional configuration settings depending on their unique functionality, the target platform and if they have GUI. Those will be detailed in the individual samples' documentation.

This is an example config.json file that shows all the common properties. If you emit an optional property, the default value shown in the table below will be used.

```json
{
  "SpeechSubscriptionKey": "b587d36063dd458daea151a1b969720a",
  "SpeechRegion": "westus",
  "SRLanguage": "en-US",  
  "CustomCommandsAppId": "32d06e92-1bd0-4f3f-2c3b-8cf036d0518f",
  "CustomSREndpointId": "c31ad51b-efef-7ec6-b262-a4be7cd251f2",
  "CustomVoiceDeploymentIds": "53bcf7be-80a2-47dc-2bd3-ba6f62eccfe3",
  "UrlOverride": "",
  "TTSBargeInSupported": true,
  "SpeechSDKLogEnabled": false
}
```

| JSON field name          | Type   | Required? | Default value | Description |
|--------------------------|--------|:---------:|:-------------:|-------------|
| SpeechSubscriptionKey    | String | Yes | - | Cognitive Services [speech subscription key](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started). Example of hows this string should look like: ```"b587d36063dd458daea151a1b969720a"```
| SpeechRegion             | String | Yes | - | The Azure region of your speech subscription. Limited to the [Voice Assistant supported regions](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/regions#voice-assistants). If you are using free-trial Azure subscription, it is further limited to ```westus``` and ```northeurope```.
| SRLanguage               | String | No  | ```"en-US"``` | The [locale code](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#speech-to-text) specifying the spoken language to be recognized. 
| CustomCommandsAppId      | String | No  | Empty | The id uniquely identifying your [Custom Command](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands) application/project. For example ```"32d06e92-1bd0-4f3f-2c3b-8cf036d0518f"```. Not required if you are connecting to a Bot-Framework bot.
| CustomVoiceDeploymentIds | String | No  | Empty | For Custom Command usage only. Specify this if you created your own [custom text-to-speech voice](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-custom-voice). For example "53bcf7be-80a2-47dc-2bd3-ba6f62eccfe3". Not required if you are using one of the pre-built voice. Note that if you are using a Bot-Framework bot, the preferred place to specify Custom Voice Deployment IDs is in the Direct Line Speech channel registration portal on Azure, not as a client property
| CustomSREndpointId       | String | No  | Empty | For Custom Command usage only. Specify this if you want to use your own [custom speech recognition model](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-custom-speech). For example ```"c31ad51b-efef-7ec6-b262-a4be7cd251f2"```. Not required if you are using the standard model. Note that if you are using a Bot-Framework bot, the preferred place to specify Custom SE Endpoint ID is in the Direct Line Speech channel registration portal on Azure, not as a client property
| UrlOverride              | String | No  | Empty | This is mostly used to test out new service features in preview, and will be provided by Microsoft to selected customers. Safe to ignore.
| TTSBargeInSupported      | Bool   | No  | false | See the [What does barge-in mean?](#what-does-barge-in-mean?)
| SpeechSDKLogEnabled      | Bool   | No  | false | Speech SDK has a verbose text logging option that you can turn on, if you need to report a bug to Microsoft. The file name will be SpeechSDK.log. A retail deployment of the client should always have this logging disabled (the default)

## What does barge-in mean?

All sample clients have the option to always-listen for an activation keyword of your choice. When barge-in is supported, the sample client will be listening at the same time as it is playing the text-to-speech reply from your Voice Assistant. This is only recommended on devices that have audio processing that includes good acoustic echo cancellation, such as provided by the Microsoft Audio Stack (MAS). Otherwise the Voice Assistant's voice may accidentally trigger a keyword activation.  

Therefore when barge-in is supported and the device is configured to listen to a keyword:
* Assume the Voice Assistant is speaking to you, as a respond to a previous request
* While the Voice Assistant is speaking, the user says the keyword
* The keyword is identified by Speech SDK (on device processing), then confirmed by the cloud Keyword Verification Service. The client receives an event indicating the keyword as been verified
* As a result of this event, the client immediately stops playback of the Voice Assistant prompt and waits for the bot to process the new utterance

If barge-in is NOT supported (the default option):
* Assume the Voice Assistant is speaking to you, as a respond to a previous request
* While the Voice Assistant is speaking, the client temporarily turns off keyword activation. If the users says the keyword during this time, it will be ignored. 
* Once the Voice Assistant finishes talking, the device will resume listening for a keyword

Note that in both cases, the user can always press a mic button to trigger a ListenOnceAsync call, which will immediately stop TTS playback and the client will wait for the bot to process the new utterance.

