// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <iostream>
#include <string> 
#include <fstream>
#include "log.h"
#include "AgentConfiguration.h"
#include "DialogManager.h"
#include "DeviceStatusIndicators.h"
#include "speechapi_cxx.h"

//the pragma here suppresses warnings from the 3rd party header
#pragma warning(push, 0)
#pragma warning (disable : 26451)
#pragma warning (disable : 26444)
#pragma warning (disable : 28020)
#pragma warning (disable : 26495)
#include "json.hpp"
#pragma warning(pop)

using namespace std;
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Audio;

void DisplayKeystrokeOptions(DialogManager&);
void HandleKeystrokeOptions(DialogManager&, string);

int main(int argc, char** argv)
{
    if (argc < 2)
    {
        log("Usage with Microphone Input:\n", argv[0], " config_file_path\n");
        log("Usage with Audio File Input:\n", argv[0], " config_file_path audio_file_path\n");
        return 0;
    }

    string configFilePath = argv[1];
    string wavFilePath = "";

    if (argc >= 3)
    {
        wavFilePath = argv[2];
    }

    DeviceStatusIndicators::SetStatus(DeviceStatus::Initializing);

    log_t("Loading configuration from file: ", configFilePath);

    shared_ptr<AgentConfiguration> agentConfig = AgentConfiguration::LoadFromFile(configFilePath);
    if (agentConfig->LoadResult() != AgentConfigurationLoadResult::Success)
    {
        log_t(agentConfig->LoadMessage());
        return (int)agentConfig->LoadResult();
    }

    // Wavfile path to send to Speech Service
    if (wavFilePath != "")
    {
        DialogManager dialogManager(agentConfig, wavFilePath);
        dialogManager.ListenFromFile();
        log_t("Initialized with audio file. Enter 'x' to exit.");
        string keystroke = "";
        cin >> keystroke;
        HandleKeystrokeOptions(dialogManager, keystroke);
    }
    else 
    {
        DialogManager dialogManager(agentConfig);
    
        // Activate keyword listening on start up if keyword model file exists
        if (agentConfig->KeywordRecognitionModel().length() > 0)
        {
            dialogManager.SetKeywordActivationState(KeywordActivationState::Paused);
            dialogManager.StartKws();
        }
        else
        {
            dialogManager.SetKeywordActivationState(KeywordActivationState::NotSupported);
        }
    
        DeviceStatusIndicators::SetStatus(DeviceStatus::Ready);

        DisplayKeystrokeOptions(dialogManager);
        log_t("Initialized with audio file. Enter 'x' to exit.");
        string keystroke = "";
        cin >> keystroke;
        HandleKeystrokeOptions(dialogManager, keystroke);
    }

    fprintf(stdout, "Closing down and freeing variables.\n");

    return 0;
}

void DisplayKeystrokeOptions(DialogManager& dialogManager)
{
    fprintf(stdout, "Commands:\n");
    fprintf(stdout, "1 [listen once]\n");
    if (dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
    {
        fprintf(stdout, "2 [start keyword listening]\n");
        fprintf(stdout, "3 [stop keyword listening]\n");
    }
    fprintf(stdout, "x [exit]\n");
};

void HandleKeystrokeOptions(DialogManager& dialogManager, string keystroke)
{
    while (keystroke != "x")
    {
        cin >> keystroke;
        if (keystroke == "1")
        {
            dialogManager.StartListening();
        }
        if (keystroke == "2" && dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
        {
            dialogManager.SetKeywordActivationState(KeywordActivationState::Paused);
            dialogManager.StartKws();
        }
        if (keystroke == "3" && dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
        {
            dialogManager.StopKws();
        }
        DisplayKeystrokeOptions(dialogManager);
    }
}