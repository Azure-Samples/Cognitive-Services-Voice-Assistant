// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <iostream>
#include <string> 
#include <fstream>
#include <thread>
#include <chrono>
#include "AgentConfiguration.h"
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

#ifdef LINUX
#include "LinuxAudioPlayer.h"
#endif

#ifdef WINDOWS
#include <Windows.h>
#include "WindowsAudioPlayer.h"
#endif

using namespace std; 
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Audio;
using namespace AudioPlayer;

enum class KeywordActivationState
{
    // Initial value, before reading the input configuration file.
    Undefined = 0, 

    // Configuration file did not specify a keyword mode. Keyword activation not possible on this device.
    NotSupported = 1, 

    // Keyword model exists on the device, user selected keyword activation, but the device is currently not listening for the keyword (e.g. since TTS playback is in progress and barge-in is not supported).
    Paused = 2,

    // Keyword model exists on the device, user selected keyword activation and the device is currently listening for the keyword.
    Listening = 3,  

    // Keyword model exists on the device but the user has selected not to listen for keyword.
    NotListening = 4 
};

void log()
{
    cout << endl;
}

template<typename T, typename... Args>
void log(T v, Args... args)
{
    cout << v << flush;
    log(args...);
}

template<typename T, typename... Args>
void log_t(T v, Args... args)
{
    char buff[9];
    std::chrono::system_clock::time_point now = chrono::system_clock::now();
    std::time_t now_c = std::chrono::system_clock::to_time_t(now);
    std::tm now_tm;
#ifdef LINUX
    localtime_r(&now_c, &now_tm);
#endif
#ifdef WINDOWS
    localtime_s(&now_tm, &now_c);
#endif
    strftime(buff, sizeof buff, "%H:%M:%S", &now_tm);

    cout << buff << "." << chrono::duration_cast<chrono::milliseconds>(now.time_since_epoch()).count() % 1000 << "  ";
    log(v, args...);
}

int main(int argc, char** argv)
{
    
    if(argc < 2){
        log("Usage:\n", argv[0] ," [config file path]\n");
        return 0;
    }
    
    string configFilePath = argv[1];
    string s;
    const char * device = "default";
    KeywordActivationState keywordActivationState = KeywordActivationState::Undefined;
    bool volumeOn = false;
    bool bargeInSupported = false;
    
    IAudioPlayer* player;
    shared_ptr<AgentConfiguration> agentConfig;
    shared_ptr<DialogServiceConnector> dialogServiceConnector;
    
    auto StartListening = [&]()
    {
        log_t("Now listening...");
        if(bargeInSupported)
        {
            player->Stop();
        }
        DeviceStatusIndicators::SetStatus(DeviceStatus::Listening);
        auto future = dialogServiceConnector->ListenOnceAsync();
    };
    
    auto StartKws = [&]()
    {
        log_t("Enter StartKws (state = ", uint32_t(keywordActivationState), ")");

        if (KeywordActivationState::Paused == keywordActivationState)
        {
            auto modelPath = agentConfig->KeywordModel();
            log_t("Initializing keyword recognition with: ", modelPath);
            auto model = KeywordRecognitionModel::FromFile(modelPath);
            auto _ = dialogServiceConnector->StartKeywordRecognitionAsync(model);
            keywordActivationState = KeywordActivationState::Listening;
            log_t("KWS initialized");
        }

        log_t("Exit StartKws (state = ", uint32_t(keywordActivationState), ")");
    };

    auto PauseKws = [&]()
    {
        log_t("Enter PauseKws (state = ", uint32_t(keywordActivationState), ")");

        if (KeywordActivationState::Listening == keywordActivationState)
        {
            log_t("Stopping keyword recognition");
            auto future = dialogServiceConnector->StopKeywordRecognitionAsync();
            keywordActivationState = KeywordActivationState::Paused;
        }

        log_t("Exit PauseKws (state = ", uint32_t(keywordActivationState), ")");
    };

    auto StopKws = [&]()
    {
        log_t("Enter StopKws (state = ", uint32_t(keywordActivationState), ")");

        if (KeywordActivationState::Listening == keywordActivationState ||
            KeywordActivationState::Paused == keywordActivationState)
        {
            if (KeywordActivationState::Listening == keywordActivationState)
            {
                log_t("Stopping keyword recognition");
                auto future = dialogServiceConnector->StopKeywordRecognitionAsync();
            }

            keywordActivationState = KeywordActivationState::NotListening;
        }

        log_t("Exit StopKws (state = ", uint32_t(keywordActivationState), ")");
    };
    
    DeviceStatusIndicators::SetStatus(DeviceStatus::Initializing);
    log_t("Loading configuration from file: ", configFilePath);
    
    agentConfig = AgentConfiguration::LoadFromFile(configFilePath);
    
    if (agentConfig->LoadResult() != AgentConfigurationLoadResult::Success)
    {
        log_t(agentConfig->LoadMessage());
        return (int)agentConfig->LoadResult();
    }
    
    if(agentConfig->_barge_in_supported == "true")
    {
        bargeInSupported = true;
    }
    
    if(agentConfig->_volume > 0){
        volumeOn = true;
#ifdef LINUX
        player = new LinuxAudioPlayer();
#endif
#ifdef WINDOWS
        player = new WindowsAudioPlayer();
#endif
        log_t("Initializing Audio Player...");
        player->Initialize();
        player->SetVolume(agentConfig->_volume);
    }
    
    log_t("Configuration loaded. Creating connector...");
    dialogServiceConnector = DialogServiceConnector::FromConfig(agentConfig->AsDialogServiceConfig());
    log_t("Connector created");
    auto future = dialogServiceConnector->ConnectAsync();
    
    log_t("Creating prime activity");
    nlohmann::json keywordPrimingActivity =
    {
        { "type", "event" },
        { "name", "KeywordPrefix" },
        { "value", agentConfig->KeywordDisplayName() }
    };
    auto keywordPrimingActivityText = keywordPrimingActivity.dump();
    log_t("Sending inform-of-keyword activity: ", keywordPrimingActivityText);
    auto stringFuture = dialogServiceConnector->SendActivityAsync(keywordPrimingActivityText);

    log_t("Connector successfully initialized!");

    // Signals that indicates the start of a listening session.
    dialogServiceConnector->SessionStarted += [&](const SessionEventArgs& event) {
        printf("SESSION STARTED: %s ...\n", event.SessionId.c_str());
    };

    // Signals that indicates the end of a listening session.
    dialogServiceConnector->SessionStopped += [&](const SessionEventArgs& event) {
        printf("SESSION STOPPED: %s ...\n", event.SessionId.c_str());
    };

    // Signal for events containing intermediate recognition results.
    dialogServiceConnector->Recognizing += [&](const SpeechRecognitionEventArgs& event) {
        printf("INTERMEDIATE: %s ...\n", event.Result->Text.c_str());
        DeviceStatusIndicators::SetStatus(DeviceStatus::Detecting);
    };

    // Signal for events containing speech recognition results.
    dialogServiceConnector->Recognized += [&](const SpeechRecognitionEventArgs& event) {
        printf("FINAL RESULT: '%s'\n", event.Result->Text.c_str());
        auto&& reason = event.Result->Reason;
        
        DeviceStatus newStatus;
        
        switch(reason){
            case ResultReason::RecognizedKeyword:
                newStatus = DeviceStatus::Listening;
                if(bargeInSupported)
                {
                    player->Stop();
                }
                break;
            case ResultReason::RecognizedSpeech:
                newStatus = DeviceStatus::Listening;
                break;
            default:
                newStatus = DeviceStatus::Idle;
        }
        
        //update the device status
        DeviceStatusIndicators::SetStatus(newStatus);
    };

    // Signal for events relating to the cancellation of an interaction. The event indicates if the reason is a direct cancellation or an error.
    dialogServiceConnector->Canceled += [&](const SpeechRecognitionCanceledEventArgs& event) {

        printf("CANCELED: Reason=%d\n", (int)event.Reason);
        DeviceStatusIndicators::SetStatus(DeviceStatus::Idle);
        if (event.Reason == CancellationReason::Error)
        {
            printf("CANCELED: ErrorDetails=%s\n", event.ErrorDetails.c_str());
            printf("CANCELED: Did you update the subscription info?\n");
            StartKws();
        }
    };

    // Signals that an activity was received from the service
    dialogServiceConnector->ActivityReceived += [&](const ActivityReceivedEventArgs& event) {
        auto activity = nlohmann::json::parse(event.GetActivity());

        // Let's log the type and whether we have audio. Note this is how you access a property in the json. Here we are
        // reading the "type" value and defaulting to "" if it doesn't exist.
        log_t("ActivityReceived, type=", activity.value("type", ""), ", audio=", event.HasAudio() ? "true" : "false");

        if (activity.contains("text"))
        {
            log_t("activity[\"text\"]: ", activity["text"].get<string>());
        }

        auto continue_multiturn = activity.value<string>("inputHint", "") == "expectingInput";

        uint32_t total_bytes_read = 0;
        if (event.HasAudio())
        {
            log_t("Activity has audio, playing synchronously.");

            if(!bargeInSupported)
            {
                log_t("Pausing KWS during TTS playback");
                PauseKws();
            }

            auto audio = event.GetAudio();
            int play_result = 0;

            if(volumeOn && player != nullptr){
                play_result = player->Play(audio);
            }

            cout << endl;
            log_t("Playback of ", total_bytes_read, " bytes complete.");

            if (!continue_multiturn)
            {
                DeviceStatusIndicators::SetStatus(DeviceStatus::Idle);
            }
        }

        //wait for audio to play. This is important even with Echo cancellation so the new listening doesn't time out while the audio is playing.
        int secondsOfAudio = total_bytes_read / 32000;
        std::this_thread::sleep_for(std::chrono::milliseconds(secondsOfAudio*1000));

        if (continue_multiturn)
        {
            log_t("Activity requested a continuation (ExpectingInput) -- listening again");
            StartListening();
        }
        else
        {
            if(!bargeInSupported)
            {
                StartKws();
            }
        }
    };

    // Activate keyword listening on start up if keyword model file exists
    if (agentConfig->KeywordModel().length() > 0)
    {
        keywordActivationState = KeywordActivationState::Paused;
        StartKws();
    }
    else
    {
        keywordActivationState = KeywordActivationState::NotSupported;
    }

    DeviceStatusIndicators::SetStatus(DeviceStatus::Ready);

    auto DisplayKeystrokeOptions = [&]()
    {
        cout << "Commands:" << endl;
        cout << "1 [listen once]" << endl;
        if (keywordActivationState != KeywordActivationState::NotSupported)
        {
            cout << "2 [start keyword listening]" << endl;
            cout << "3 [stop keyword listening]" << endl;
        }
        cout << "x [exit]" << endl;
    };

    DisplayKeystrokeOptions();

    s = "";
    while(s != "x")
    {
        cin >> s;
        if(s == "1"){
            StartListening();
        }
        if(s == "2" && keywordActivationState != KeywordActivationState::NotSupported){
            keywordActivationState = KeywordActivationState::Paused;
            StartKws();
        }
        if(s == "3" && keywordActivationState != KeywordActivationState::NotSupported){
            StopKws();
        }
        DisplayKeystrokeOptions();
    }

    cout << "Closing down and freeing variables." << endl;
    
    return 0;
}
