# Microsoft Cognitive Services - Voice Assistant C++ Console Sample

## Overview

This sample is intended to be a starting point for any application coded in c++. It has some generic files that implement an IAudioPlayer interface for audio playback. This interface will be specific to the target OS and/or device. A sample player for linux devices is included.

## Prerequisites and Setup
* You will need a speech service subscription key and region. Instructions for creating one can be found on this [page](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) under "Create a resource group" and "Create resources"

* The [Microsoft Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) will need to be downloaded.

For Windows you should use the nuget package.

For Linux or ARM devices the build scripts will download and copy the SDK's newest version and the native binaries will be copied to the lib folder in this repo so that you have a structure like this (for ARM32): ./lib/arm32. 

Headers and their folders will be copied into the include folder so that you have a structure like this: ./include/cxx_api and ./include/c_api

Replace the text in the configs/config.json file with your subscription key and key region. If you are using a Custom Commands application or custom speech font you can insert those GUID's there as well.

## Build directly on Linux arm32

Check out the [RaspberryPi.md](docs/RaspberryPi.md) for detailed instructions.

## Building for Linux Arm32 with Docker

Check out the [GGECSpeaker.md](docs/GGECSpeaker.md) for detailed instructions.

## Building for Linux on a Windows machine

Check out how to [compile this C++ console sample for Linux on a Windows machine](docs/BuildForLinuxOnWindows.md)

## Building for Windows

Check out the [Windows.md](docs/Windows.md) for detailed instructions.

## Quickstart

1. Follow the instructions listed above to setup the building and running environment. Besides, get subscription key and key region ready at hand, along with app id if you are using a Custom Commands application.

2. Build the executable from source code:
* First clone the repository:
```cmd
git clone https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistants.git
```
* Then change directories if you are doing for Windows:
```cmd
cd Cognitive-Services-Voice-Assistants\clients\cpp-console\src\windows
```
* Launch Visual Studio 2017 or newer by opening the solution cppSample.sln. Build the solution (the default build flavor is Debug x64).

3. Config and run the executable.
* For Debug x64 build, go to the directory:
```cmd
cd Cognitive-Services-Voice-Assistants\clients\cpp-console\src\windows\ x64\Debug
```
* Open config.json (if it is not there, copy and paste from Cognitive-Services-Voice-Assistant\clients\cpp-console\configs), replace YOUR_SUBSCRIPTION_KEY, YOUR_SUBSCRIPTION_REGION, and YOUR_CUSTOM_COMMANDS_APP_ID with actual values. The KeywordRecognitionModel, Keyword, Volume, TTSBargeInSupported, CustomMicConfigPath, LinuxCaptureDeviceName below show you how these values could be.
```json
{
  "KeywordRecognitionModel": "/data/cppSample/computer.table",
  "Keyword": "Computer",
  "SpeechSubscriptionKey": "YOUR_SUBSCRIPTION_KEY",
  "SpeechRegion": "YOUR_SUBSCRIPTION_REGION",
  "Volume": "25",
  "CustomCommandsAppId": " YOUR_CUSTOM_COMMANDS_APP_ID",
  "CustomVoiceDeploymentIds": "",
  "SpeechSDKLogFile": "",
  "TTSBargeInSupported": "true",
  "CustomMicConfigPath" :"/home/ubuntu/cpp-console/configs/micConfig.json",
  "LinuxCaptureDeviceName": "hw:1,0"
}
```
* Run the executable cppSample.exe.
![Console](docs/Console.png)
Currently, the CPP console application supports 6 user inputs:
1. listen once – Enter 1 to start listening. The listening session will stop when detecting starts.
2. stop – Enter 2 to stop speaking.
3. mute/unmute – Enter 3 to mute/unmute when listening. This functionality is not implemented yet.
4. start keyword listening – Though keyword recognition starts automatically if a valid keyword is specified, enter 4 to start keyword listening if it is stopped later on.
5. stop keyword listening – Enter 5 to stop keyword listening.
6. exit – Enter x to exit this console application.

