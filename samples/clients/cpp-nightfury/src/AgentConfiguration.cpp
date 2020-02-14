// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AgentConfiguration.h"
#include <cstdio>
#include <fstream>
#include "json.hpp"

using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace std;

namespace FieldNames
{
    constexpr auto KeywordModel = "keyword_model";
    constexpr auto CommandsAppId = "commands_app_id";
    constexpr auto SpeechSubscriptionKey = "speech_subscription_key";
    constexpr auto SpeechRegion = "speech_region";
    constexpr auto CustomVoiceDeploymentIds = "custom_voice_deployment_ids";
    constexpr auto CustomSpeechDeploymentId = "custom_speech_deployment_id";
    constexpr auto CustomEndpoint = "custom_endpoint";
    constexpr auto KeywordDisplay = "keyword_display";
    constexpr auto Volume = "volume";
}

AgentConfiguration::AgentConfiguration() : _loadResult(AgentConfigurationLoadResult::Undefined)
{}

shared_ptr<AgentConfiguration> AgentConfiguration::LoadFromFile(const string& path)
{
    auto config = make_shared<AgentConfiguration>();

    if (path.length() == 0)
    {
        config->_loadResult = AgentConfigurationLoadResult::FileNotFound;
        return config;
    }

    // Used as an alias
    auto&& j = config->_configJson;

    ifstream inputStream{ path.data() };
    inputStream >> j;

    config->_commandsAppId = j.value(FieldNames::CommandsAppId, "");
    config->_speechKey = j.value(FieldNames::SpeechSubscriptionKey, "");
    config->_speechRegion = j.value(FieldNames::SpeechRegion, "");
    config->_customEndpoint = j.value(FieldNames::CustomEndpoint, "");
    config->_customVoiceIds = j.value(FieldNames::CustomVoiceDeploymentIds, "");
    config->_customSpeechId = j.value(FieldNames::CustomSpeechDeploymentId, "");
    config->_keywordModelPath = j.value(FieldNames::KeywordModel, "");
    config->_keywordDisplayName = j.value(FieldNames::KeywordDisplay, "");
    config->_volume = j.value(FieldNames::Volume, "");

    if (config->_speechKey.length() == 0)
    {
        config->_loadResult = AgentConfigurationLoadResult::BadSpeechKey;
        return config;
    }

    if (config->_customEndpoint.length() > 0 && config->_speechRegion.length() > 0)
    {
        config->_loadResult = AgentConfigurationLoadResult::RegionWithCustom;
        return config;
    }

    if (config->_customEndpoint.empty() && config->_speechRegion.empty())
    {
        config->_loadResult = AgentConfigurationLoadResult::MissingRegion;
        return config;
    }

    config->_loadResult = AgentConfigurationLoadResult::Success;
    config->_dialogServiceConfig = config->CreateDialogServiceConfig();

    return config;
}

shared_ptr<DialogServiceConfig> AgentConfiguration::AsDialogServiceConfig()
{
    if (_loadResult != AgentConfigurationLoadResult::Success)
    {
        return nullptr;
    }

    return _dialogServiceConfig;
}

shared_ptr<DialogServiceConfig> AgentConfiguration::CreateDialogServiceConfig()
{
    auto config = _commandsAppId.length() > 0
        ? dynamic_pointer_cast<DialogServiceConfig>(CustomCommandsConfig::FromSubscription(_commandsAppId, _speechKey, _speechRegion))
        : dynamic_pointer_cast<DialogServiceConfig>(BotFrameworkConfig::FromSubscription(_speechKey, _speechRegion));

    if (_customEndpoint.length() > 0)
    {
        config->SetProperty(PropertyId::SpeechServiceConnection_Endpoint, _customEndpoint);
    }

    if (_customVoiceIds.length() > 0)
    {
        config->SetProperty(PropertyId::Conversation_Custom_Voice_Deployment_Ids, _customVoiceIds);
    }

    if (_customSpeechId.length() > 0)
    {
        config->SetServiceProperty("cid", _customSpeechId, ServicePropertyChannel::UriQueryParameter);
    }

    return config;
}