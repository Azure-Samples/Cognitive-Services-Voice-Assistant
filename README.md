---
page_type: sample
description: "Sample code for Microsoft Cognitive Services Voice Assistant"
languages:
- csharp
- c++
products:
- dotnet-core
- azure-bot-service
- azure-cognitive-services
- azure-iot-edge
- azure-language-understanding
- azure-speech-text	
- azure-text-speech
- windows
- windows-iot
- windows-wpf
- windows-uwp
- Cognitive Services

---
[![Build Status](https://msasg.visualstudio.com/Skyman/_apis/build/status/Azure-Samples.Cognitive-Services-Voice-Assistant?branchName=master)](https://msasg.visualstudio.com/Skyman/_build/latest?definitionId=12256&branchName=master)

<!-- For above fields, see: https://review.docs.microsoft.com/en-us/help/contribute/samples/process/onboarding?branch=master#yaml-front-matter-structure  -->

# Microsoft Cognitive Services - Voice Assistant Sample Code

<!-- 
Guidelines on README format: https://review.docs.microsoft.com/help/onboard/admin/samples/concepts/readme-template?branch=master

Guidance on onboarding samples to docs.microsoft.com/samples: https://review.docs.microsoft.com/help/onboard/admin/samples/process/onboarding?branch=master

Taxonomies for products and languages: https://review.docs.microsoft.com/new-hope/information-architecture/metadata/taxonomies?branch=master
-->

## Overview

This repository includes samples of [Voice Assistant](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/voice-assistants) clients for different platforms. It also includes a client tool for end-to-end regression testing of a Voice Assistant system. Voice Assistant clients use Microsoft's [Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) to connect to [Direct Line Speech Channel](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/direct-line-speech) and your [Bot-Framework](https://dev.botframework.com/) bot. Alternatively, Voice Assistant clients can use Speech SDK to connect to your [Custom Commands](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands) voice application.

<!--
Sample code for building Voice Assistant clients, using Microsoft's Speech SDK and Direct Line Speech channel, including Custom Command
0-->

## Samples List

To build any of the samples below, clone this GitHub repository and look at the projects in the samples folder:

```bash
    git clone https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant.git
    cd samples
```

The following table describes the samples and root files in this repository:

| File/folder | Description | Language/Platform |
|-------------|-------------|-------------------|
| [`samples\clients\csharp-wpf`](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/samples/clients/csharp-wpf) |  Windows voice assistant client sample. Generic Windows tool to manually test your bot or Custom Commands application | C#, Windows Presentation Foundation (WPF) |
| [`samples\clients\csharp-dotnet-core\voice-assistant-test`](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/samples/clients/csharp-dotnet-core/voice-assistant-test) | Automated, multi-turn, end-to-end regression test for your bot or Custom Commands application. Supports WAV file input, text or Bot-Framework activities | C#, .NET Core |
| [`samples\clients\cpp-console`](.\samples\clients\cpp-console) | C plus plus client application configured via a json file. It supports microphone input and audio playback. | C++, Windows, Linux |
| `.gitignore`         | Define what to ignore at commit time
| `CODE_OF_CONDUCT.md` | Code of Conduct for all Microsoft repositories
| `CONTRIBUTING.md`    | Guidelines for contributing to these samples
| `README.md`          | This README file
| `LICENSE.md`         | The license for these samples
| `SECURITY.md`        | Information about reporting any security vulnerabilities to Microsoft
| `NOTICE.txt`         | License of third party software incorporated in these samples

<!--
## Prerequisites

Outline the required components and tools that a user might need to have on their machine in order to run the sample. This can be anything from frameworks, SDKs, OS versions or IDE releases.

## Setup

Explain how to prepare the sample once the user clones or downloads the repository. The section should outline every step necessary to install dependencies and set up any settings (for example, API keys and output folders).

## Runnning the sample

Outline step-by-step instructions to execute the sample and see its output. Include steps for executing the sample from the IDE, starting specific services in the Azure portal or anything related to the overall launch of the code.

## Key concepts

Provide users with more context on the tools and services used in the sample. Explain some of the code that is being used and how services interact with each other.
-->

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Reporting Security Issues
Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the [MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).

Copyright (c) Microsoft Corporation. All rights reserved.
