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

int main(int argc, char** argv)
{
    if (argc < 2)
    {
        log("Usage:\n", argv[0], " [config file path]\n");
        return 0;
    }

    string configFilePath = argv[1];

    DeviceStatusIndicators::SetStatus(DeviceStatus::Initializing);

    log_t("Loading configuration from file: ", configFilePath);
    shared_ptr<AgentConfiguration> agentConfig = AgentConfiguration::LoadFromFile(configFilePath);
    if (agentConfig->LoadResult() != AgentConfigurationLoadResult::Success)
    {
        log_t(agentConfig->LoadMessage());
        return (int)agentConfig->LoadResult();
    }

    DialogManager dialogManager(agentConfig);

    // Activate keyword listening on start up if keyword model file exists
    if (agentConfig->KeywordModel().length() > 0)
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

    string s = "";
    while (s != "x")
    {
        cin >> s;
        if (s == "1")
        {
            dialogManager.StartListening();
        }
        if (s == "2" && dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
        {
            dialogManager.SetKeywordActivationState(KeywordActivationState::Paused);
            dialogManager.StartKws();
        }
        if (s == "3" && dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
        {
            dialogManager.StopKws();
        }
        DisplayKeystrokeOptions(dialogManager);
    }

    cout << "Closing down and freeing variables." << endl;

    return 0;
}

void DisplayKeystrokeOptions(DialogManager& dialogManager)
{
    cout << "Commands:" << endl;
    cout << "1 [listen once]" << endl;
    if (dialogManager.GetKeywordActivationState() != KeywordActivationState::NotSupported)
    {
        cout << "2 [start keyword listening]" << endl;
        cout << "3 [stop keyword listening]" << endl;
    }
    cout << "x [exit]" << endl;
};