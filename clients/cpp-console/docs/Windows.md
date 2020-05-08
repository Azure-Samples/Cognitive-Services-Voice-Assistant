# Microsoft Cognitive Services - Voice Assistant C++ Console Sample - Windows Setup

## Overview

This readme describes how to build and run the C++ sample code on your Windows machine

## Requirements

You will need a Windows 10 PC with Visual Studio 2017 or higher.

## Build the code

Open the Visual Studio solution **clients\cpp-console\src\windows\cppSample.sln** and compile the project.

Open a console Window in the project output folder, e.g. **clients\cpp-console\src\windows\x64\Debug** (for x64 debug build) and see the resulting executable **cppSample.exe** in that folder.

## Configure your client

Copy the example configuration file **clients\configs\config.json** into your project output folder and update it as needed. Fill in your subscription key and key region. If you are using a Custom Commands application or a Custom Voice insert those GUID's as well. The keyword_model should point to the Custom Keyword (.table file) being used. You can delete fields that are not required for your setup. Only the speech_subscription_key and speech_region are required.

## Run the code

To run, type
```cmd
cppSample.exe config.json
```

