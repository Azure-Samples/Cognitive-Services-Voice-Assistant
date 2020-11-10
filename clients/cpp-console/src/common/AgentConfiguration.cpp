// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define _SILENCE_EXPERIMENTAL_FILESYSTEM_DEPRECATION_WARNING
#include "AgentConfiguration.h"
#include <stdlib.h>
#include <cstdio>
#include <fstream>
#include <experimental/filesystem>
//the pragma here suppresses warnings from the 3rd party header
#pragma warning(push, 0)
#pragma warning (disable : 26451)
#pragma warning (disable : 26444)
#pragma warning (disable : 28020)
#pragma warning (disable : 26495)
#include "json.hpp"
#pragma warning(pop)

using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace std;

namespace FieldNames
{
    constexpr auto KeywordRecognitionModel = "KeywordRecognitionModel";
    constexpr auto CustomCommandsAppId = "CustomCommandsAppId";
    constexpr auto SRLanguage = "SRLanguage";
    constexpr auto SpeechSubscriptionKey = "SpeechSubscriptionKey";
    constexpr auto SpeechRegion = "SpeechRegion";
    constexpr auto CustomVoiceDeploymentIds = "CustomVoiceDeploymentIds";
    constexpr auto CustomSREndpointId = "CustomSREndpointId";
    constexpr auto UrlOverride = "UrlOverride";
    constexpr auto KeywordDisplay = "Keyword";
    constexpr auto Volume = "Volume";
    constexpr auto BargeInSupported = "TTSBargeInSupported";
    constexpr auto LogFilePath = "SpeechSDKLogFile";
    constexpr auto CustomMicConfigPath = "CustomMicConfigPath";
    constexpr auto LinuxCaptureDeviceName = "LinuxCaptureDeviceName";
}

AgentConfiguration::AgentConfiguration() : _loadResult(AgentConfigurationLoadResult::Undefined)
{
}

shared_ptr<AgentConfiguration> AgentConfiguration::LoadFromFile(const string& path)
{
    auto config = make_shared<AgentConfiguration>();

    if (path.length() == 0 || !std::experimental::filesystem::exists(path))
    {
        config->_loadResult = AgentConfigurationLoadResult::ConfigFileNotFound;
        return config;
    }

    // Used as an alias
    auto&& j = config->_configJson;

    ifstream inputStream{ path.data() };
    inputStream >> j;

    config->_customCommandsAppId = j.value(FieldNames::CustomCommandsAppId, "");
    config->_speechKey = j.value(FieldNames::SpeechSubscriptionKey, "");
    config->_srLanguage = j.value(FieldNames::SRLanguage, "");
    config->_speechRegion = j.value(FieldNames::SpeechRegion, "");
    config->_urlOverride = j.value(FieldNames::UrlOverride, "");
    config->_customVoiceIds = j.value(FieldNames::CustomVoiceDeploymentIds, "");
    config->_customSREndpointId = j.value(FieldNames::CustomSREndpointId, "");
    config->_keywordRecognitionModel = j.value(FieldNames::KeywordRecognitionModel, "");
    config->_keywordDisplayName = j.value(FieldNames::KeywordDisplay, "");
    config->_logFilePath = j.value(FieldNames::LogFilePath, "");
    config->_customMicConfigPath = j.value(FieldNames::CustomMicConfigPath, "");
    config->_linuxCaptureDeviceName = j.value(FieldNames::LinuxCaptureDeviceName, "");
    config->_volume = atoi(j.value(FieldNames::Volume, "").c_str());
    config->_barge_in_supported = j.value(FieldNames::BargeInSupported, "");

    if (config->_keywordRecognitionModel.length() > 0)
    {
        if (config->_keywordRecognitionModel.find("~") != string::npos)
        {
            // ifdef is used because variations of getenv functions with _s suffixes are not supported in GNU C Library
            // Microsoft implements an approximation to the C11 standard hence _dupenv_s is used for _WIN32_
            // and secure_getenv is used __linux__
            // both _dupenv_s and secure_getenv are 'safe' versions of getenv
            #ifdef _WIN32

            char* pValue = nullptr;
            size_t len = 0;
            errno_t err = _dupenv_s(&pValue, &len, "USERPROFILE");
            if (pValue != 0) 
            {
                config->_keywordRecognitionModel.replace(0, 1, pValue);
            }
            free(pValue);

            #elif __linux__

            string home = secure_getenv("HOME");
            config->_keywordRecognitionModel.replace(0, 1, home);
            
            #endif
        }

        if (!std::experimental::filesystem::exists(config->_keywordRecognitionModel))
        {
            printf("%s is not a valid file path\n", config->_keywordRecognitionModel.c_str());
            config->_loadResult = AgentConfigurationLoadResult::KWFileNotFound;
            return config;
        }

        // this check should be removed once the SDK properly validates KWS model files
        std::experimental::filesystem::path pathObj(config->_keywordRecognitionModel);
        if (!pathObj.has_extension() || pathObj.extension().string() != ".table")
        {
            config->_loadResult = AgentConfigurationLoadResult::KWFileWrongExtension;
            return config;
        }
    }

    if (config->_speechKey.length() == 0)
    {
        config->_loadResult = AgentConfigurationLoadResult::BadSpeechKey;
        return config;
    }

    if (config->_urlOverride.length() > 0 && config->_speechRegion.length() > 0)
    {
        config->_loadResult = AgentConfigurationLoadResult::RegionWithCustom;
        return config;
    }

    if (config->_urlOverride.empty() && config->_speechRegion.empty())
    {
        config->_loadResult = AgentConfigurationLoadResult::MissingRegion;
        return config;
    }

    config->_loadResult = AgentConfigurationLoadResult::Success;
    config->_dialogServiceConfig = config->CreateDialogServiceConfig();

    return config;
}

std::string AgentConfiguration::LoadMessage()
{
    switch (_loadResult)
    {
    case AgentConfigurationLoadResult::Success:
        return "Success";
    case AgentConfigurationLoadResult::ConfigFileNotFound:
        return "Config file is not found.";
    case AgentConfigurationLoadResult::ConfigFileNotParsed:
        return "Config file can not be parsed.";
    case AgentConfigurationLoadResult::KWFileNotFound:
        return "Keyword file is not found.";
    case AgentConfigurationLoadResult::KWFileWrongExtension:
        return "Keyword file extension is not \".table\".";
    case AgentConfigurationLoadResult::BadSpeechKey:
        return "Speech key is missing.";
    case AgentConfigurationLoadResult::MissingRegion:
        return "Region is missing.";
    case AgentConfigurationLoadResult::RegionWithCustom:
        return "Region with custom is found.";
    case AgentConfigurationLoadResult::Undefined:
    default:
        return "Unknown Failure";
    }
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
    auto config = _customCommandsAppId.length() > 0
        ? dynamic_pointer_cast<DialogServiceConfig>(CustomCommandsConfig::FromSubscription(_customCommandsAppId, _speechKey, _speechRegion))
        : dynamic_pointer_cast<DialogServiceConfig>(BotFrameworkConfig::FromSubscription(_speechKey, _speechRegion, ""));

    if (_srLanguage.length() > 0)
    {
        // Set the speech recognition language. If not set, the default is "en-us".
        config->SetLanguage(_srLanguage);
    }
    if (_urlOverride.length() > 0)
    {
        config->SetProperty(PropertyId::SpeechServiceConnection_Endpoint, _urlOverride);
    }

    if (_customVoiceIds.length() > 0)
    {
        config->SetProperty(PropertyId::Conversation_Custom_Voice_Deployment_Ids, _customVoiceIds);
    }

    if (_logFilePath.length() > 0)
    {
        config->SetProperty(PropertyId::Speech_LogFilename, _logFilePath);
    }

    if (_customSREndpointId.length() > 0)
    {
        config->SetServiceProperty("cid", _customSREndpointId, ServicePropertyChannel::UriQueryParameter);
    }

    return config;
}