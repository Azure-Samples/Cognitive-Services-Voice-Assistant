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

    log_t("Loading configuration from file: ", configFilePath);

    shared_ptr<AgentConfiguration> agentConfig = AgentConfiguration::LoadFromFile(configFilePath);
    if (agentConfig->LoadResult() != AgentConfigurationLoadResult::Success)
    {
        log_t(agentConfig->LoadMessage());
        return (int)agentConfig->LoadResult();
    }

    std::shared_ptr<DialogManager> dialogManager;
    string keystroke = "";

    // Wavfile path to send to Speech Service
    if (wavFilePath != "")
    {
        log_t("Initialized with audio WAV file. Enter 'x' to exit.");

        dialogManager = make_shared<DialogManager>(agentConfig, wavFilePath);
        dialogManager->ListenFromFile();
    }
    else
    {
        log_t("Initialized with live mic. Enter 'x' to exit.");

        dialogManager = make_shared<DialogManager>(agentConfig);

        DisplayKeystrokeOptions(*dialogManager);
    }

    cin >> keystroke;
    HandleKeystrokeOptions(*dialogManager, keystroke);

    fprintf(stdout, "Closing down and freeing variables.\n");

    return 0;
}

void DisplayKeystrokeOptions(DialogManager& dialogManager)
{
    fprintf(stdout, "Commands:\n");
    fprintf(stdout, "1 [listen once]\n");
    fprintf(stdout, "2 [stop]\n");
    fprintf(stdout, "3 [mute/unmute]\n");
    if (dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
    {
        fprintf(stdout, "4 [start keyword listening]\n");
        fprintf(stdout, "5 [stop keyword listening]\n");
    }
    fprintf(stdout, "x [exit]\n");
    if (dialogManager.IsMuted())
    {
        std::cout << ansi::foreground_red << "(Microphone is muted)" << ansi::reset << std::endl;
    }
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
        if (keystroke == "2")
        {
            dialogManager.Stop();
        }
        if (keystroke == "3")
        {
            dialogManager.MuteUnMute();
        }
        if (keystroke == "4" && dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
        {
            dialogManager.StartKws();
        }
        if (keystroke == "5" && dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
        {
            dialogManager.StopKws();
        }
        DisplayKeystrokeOptions(dialogManager);
    }
}