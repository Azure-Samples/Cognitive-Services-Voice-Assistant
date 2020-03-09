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
    cout << chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch()).count() % 10000000 << "  ";
    log(v, args...);
}

int main(int argc, char** argv)
{
    
    if(argc < 2){
        log("Usage:\n", argv[0] ," [config file path]\n");
        return 0;
    }
    
    int bufferSize = 1024;
    unsigned char * buffer = (unsigned char *)malloc(bufferSize);
    string configFilePath = argv[1];
    string s;
    const char * device = "default";
    bool keywordListeningEnabled = false;
    bool volumeOn = false;
    
    IAudioPlayer* player;
    shared_ptr<AgentConfiguration> agentConfig;
    shared_ptr<DialogServiceConnector> dialogServiceConnector;
    
    auto startKwsIfApplicable = [&]()
    {
        log_t("startKWS called");
        if (keywordListeningEnabled)
        {
            auto modelPath = agentConfig->KeywordModel();
            log_t("Initializing keyword recognition with: ", modelPath);
            auto model = KeywordRecognitionModel::FromFile(modelPath);
            auto _ = dialogServiceConnector->StartKeywordRecognitionAsync(model);
            log_t("KWS initialized");
        }
        else {
            log_t("no model file specified. Cannot start keyword listening");
        }
    };
    
    DeviceStatusIndicators::SetStatus(DeviceStatus::Initializing);
    log_t("Loading configuration from file: ", configFilePath);
    
    agentConfig = AgentConfiguration::LoadFromFile(configFilePath);
    
    if (agentConfig->LoadResult() != AgentConfigurationLoadResult::Success)
    {
        log_t("Unable to load config file.");
        return (int)agentConfig->LoadResult();
    }
    
    if(agentConfig->_volume > 0){
        volumeOn = true;
#ifdef LINUX
        player = new LinuxAudioPlayer();
#endif
#ifdef WINDOWS
        player = new WindowsAudioPlayer();
#endif
        player->SetVolume(agentConfig->_volume);
    }
    
    log_t("Configuration loaded. Creating connector...");
    
    shared_ptr<DialogServiceConfig> config = agentConfig->CreateDialogServiceConfig();

    dialogServiceConnector = DialogServiceConnector::FromConfig(config);
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
    
    if(agentConfig->KeywordModel().length() > 0){
        keywordListeningEnabled = true;
    }
    
    // Signals that indicates the start of a listening session.
    dialogServiceConnector->SessionStarted += [&](const SessionEventArgs& event) {
        printf("SESSION STARTED: %s ...\n", event.SessionId.c_str());
    };

    // Signals that indicates the end of a listening session.
    dialogServiceConnector->SessionStopped += [&](const SessionEventArgs& event) {
        printf("SESSION STOPPED: %s ...\n", event.SessionId.c_str());
        printf("Press ENTER to acknowledge...\n");
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
        auto newStatus = reason == ResultReason::RecognizedKeyword
                ? DeviceStatus::Listening
                : reason == ResultReason::RecognizedSpeech
                ? DeviceStatus::Thinking
                : DeviceStatus::Idle;
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
            startKwsIfApplicable();
        }
    };

    // Signals that an activity was received from the service
    dialogServiceConnector->ActivityReceived += [&](const ActivityReceivedEventArgs& event) {
        auto activity = nlohmann::json::parse(event.GetActivity());

            log_t("ActivityReceived, type=", activity.value("type", ""), ", audio=", event.HasAudio() ? "true" : "false");

            if (activity.contains("text"))
            {
                log_t("activity[\"text\"]: ", activity["text"].get<string>());
            }

            auto continue_multiturn = activity.value<string>("inputHint", "") == "expectingInput";

            uint32_t total_bytes_read = 0;
            if (event.HasAudio())
            {
                log_t("Activity has audio, playing synchronously");

                // TODO: AEC + Barge-in
                // For now: no KWS during playback
                log_t("stopping KWS for playback");
                auto future = dialogServiceConnector->StopKeywordRecognitionAsync();
                log_t("KWS stopped");

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

            if (continue_multiturn)
            {
                log_t("Activity requested a continuation (ExpectingInput) -- listening again");
                DeviceStatusIndicators::SetStatus(DeviceStatus::Listening);
                auto future = dialogServiceConnector->ListenOnceAsync();
            }
            else
            {
                //TODO remove once we have echo cancellation
                int secondsOfAudio = total_bytes_read / 32000;
                std::this_thread::sleep_for(std::chrono::milliseconds(secondsOfAudio*1000));
                startKwsIfApplicable();
            }
        };
    
    startKwsIfApplicable();
    DeviceStatusIndicators::SetStatus(DeviceStatus::Ready);
    
    cout << "Commands:" << endl;
    cout << "1 [listen once]" << endl;
    cout << "2 [start keyword listening]" << endl;
    cout << "3 [stop keyword listening]" << endl;
    cout << "x [exit]" << endl;
    
    s = "";
    while(s != "x")
    {
        cin >> s;
        if(s == "1"){
            log_t("Now listening...");
            auto future = dialogServiceConnector->ListenOnceAsync();
        }
        if(s == "2"){
            startKwsIfApplicable();
        }
        if(s == "3"){
            if(keywordListeningEnabled){
                log_t("Stopping keyword recognition");
                auto future = dialogServiceConnector->StopKeywordRecognitionAsync();
            }
            else{
                cout << "No model path specified. Cannot stop keyword listening.\n";
            }
        }
        cout << "Commands:" << endl;
        cout << "1 [listen once]" << endl;
        cout << "2 [start keyword listening]" << endl;
        cout << "3 [stop keyword listening]" << endl;
        cout << "x [exit]" << endl;
    }
    cout << "Closing down and freeing variables" << endl;
    
    if(volumeOn){
        player->Close();
    }
    free(buffer);
    
    return 0;
}
