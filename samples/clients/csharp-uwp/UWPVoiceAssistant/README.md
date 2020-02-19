[Welcome to UWP Voice Assistant](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant)
===

Overview
===
This solution contains implementation for MVA (Multi Voice Assistant) and DLS (Direct Line Speech) in a C# UWP Application. If you are new to UWP visit [Getting Started with UWP](https://docs.microsoft.com/en-us/windows/uwp/get-started/ "Getting Started with UWP"). If you are new to Azure Cognitive Services visit [Getting Started with Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/ "Azure Cognitive Services").
<br>
 This sample demonstrates how to use Keyword Spotting and Custom Wake Words to enable Multi Voice Assistants with Microsoft's Azure Cognitive Services Speech and Speech SDK. These samples should be used as a guiding tool for developers to implement their own solutions using the Speech SDK or their own Speech SDK.

Prerequisites
===
* A subscription key for the Speech service. See [Try the speech service for free](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started).
* A pre-configured bot created using Bot Framework version 4.2 or above. See [here for steps on how to create a bot](https://blog.botframework.com/2018/05/07/build-a-microsoft-bot-framework-bot-with-the-bot-builder-sdk-v4/). The bot would need to subscribe to [Direct Line Speech](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) to send and receive voice inputs. 
* A Windows PC with Windows 10 or later, with a working microphone.
* [Microsoft Visual Studio 2017](https://visualstudio.microsoft.com/), Community Edition or higher.
* The **Universal Windows Platform development** workload in Visual Studio. See [Get set up](https://docs.microsoft.com/en-us/windows/uwp/get-started/get-set-up) to get your machine ready for developing UWP Applications.


Samples List
===
Get started with UWPVoiceAssistant Application using Azure Cognitive Services. To use the sample provided, clone this GitHub repository using Git.

```
git clone https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant.git
cd Cognitive-Services-Voice-Assistant/samples/clients/csharp-uwp/UWPVoiceAssistant
UWPVoiceAssitant.sln
```

Getting Started with Sample
===
## Detailed Readme coming soon.

References
===

* This is the initial landing page for the Azure Cognitive Services Speech Documentation. It is recommended for beginners to read through the sections of interest to understand the capabilites of Azure Cognitive Services Speech Service `=>` [Speech Services Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/)

* [Speech SDK API reference for C#](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech?view=azure-dotnet)
* To voice enable your bot using Azure Direct Line Speech Channel `=>` [Voice-enable your bot using the Speed SDK Tutorial](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk)

* Voice-first Virtual Assistants [FAQ's](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/faq-voice-first-virtual-assistants)

* This E-book is a great resource to fill any knowledge gaps as well as build additional knowledge in building Azure Cognitive Services Solutions `=>` [Learning Azure Cognitive Services E-book](https://azure.microsoft.com/en-us/resources/learning-azure-cognitive-services/ "Azure Cognitive Services E-book")

* Speech SDK documenation landing page `=>` [Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk)


* Windows.Media.Audio namespace allows us to access, modify and process audio `=>` [UWP Audio Graphs API](https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/audio-graphs)

* The Speech.Audio namespace allows the application to access and output audio streams for processing `=>` [Microsoft.Cognitive.Services.Speech.Audio Namespace](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.audio?view=azure-dotnet)

* Microsoft.CognitiveServices.Speech.Dialog allows us to connect the Direct Line Speech enabled bot to connect to our UWP Application. View the DialogServiceConfig class for implementation methods `=>` [Microsoft.Cognitive.Services.Speech.Dialog Namespace](https://docs.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.dialog?view=azure-dotnet)

* [What is UWP?](https://docs.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide)


Copyright (c) Microsoft Corporation. All rights reserved.