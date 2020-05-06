// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <thread>
#include "log.h"
#include "DialogManager.h"

using namespace std;
using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace AudioPlayer;

DialogManager::DialogManager(shared_ptr<AgentConfiguration> agentConfig)
{
    _agentConfig = agentConfig;

    InitializeDialogServiceConnector();
    InitializePlayer();
    AttachHandlers();
}

void DialogManager::InitializeDialogServiceConnector()
{
    log_t("Configuration loaded. Creating connector...");
    _dialogServiceConnector = DialogServiceConnector::FromConfig(_agentConfig->AsDialogServiceConfig());
    log_t("Connector created");
    auto future = _dialogServiceConnector->ConnectAsync();

    log_t("Creating prime activity");
    nlohmann::json keywordPrimingActivity =
    {
        { "type", "event" },
        { "name", "KeywordPrefix" },
        { "value", _agentConfig->KeywordDisplayName() }
    };
    auto keywordPrimingActivityText = keywordPrimingActivity.dump();
    log_t("Sending inform-of-keyword activity: ", keywordPrimingActivityText);
    auto stringFuture = _dialogServiceConnector->SendActivityAsync(keywordPrimingActivityText);

    log_t("Connector successfully initialized!");
}

void DialogManager::InitializePlayer()
{
    if(_agentConfig->_barge_in_supported == "true")
    {
        _bargeInSupported = true;
    }
    
    if (_agentConfig->_volume > 0)
    {
        _volumeOn = true;
#ifdef LINUX
        _player = new LinuxAudioPlayer();
#endif
#ifdef WINDOWS
        _player = new WindowsAudioPlayer();
#endif
        log_t("Initializing Audio Player...");
        _player->Initialize();
        _player->SetVolume(_agentConfig->_volume);
    }

}

void DialogManager::AttachHandlers()
{
    // Signals that indicates the start of a listening session.
    _dialogServiceConnector->SessionStarted += [&](const SessionEventArgs& event)
    {
        printf("SESSION STARTED: %s ...\n", event.SessionId.c_str());
    };

    // Signals that indicates the end of a listening session.
    _dialogServiceConnector->SessionStopped += [&](const SessionEventArgs& event)
    {
        printf("SESSION STOPPED: %s ...\n", event.SessionId.c_str());
    };

    // Signal for events containing intermediate recognition results.
    _dialogServiceConnector->Recognizing += [&](const SpeechRecognitionEventArgs& event)
    {
        printf("INTERMEDIATE: %s ...\n", event.Result->Text.c_str());
        DeviceStatusIndicators::SetStatus(DeviceStatus::Detecting);
    };

    // Signal for events containing speech recognition results.
    _dialogServiceConnector->Recognized += [&](const SpeechRecognitionEventArgs& event)
    {
        printf("FINAL RESULT: '%s'\n", event.Result->Text.c_str());
        auto&& reason = event.Result->Reason;

        DeviceStatus newStatus;

        switch (reason)
        {
        case ResultReason::RecognizedKeyword:
            newStatus = DeviceStatus::Listening;
            _player->Stop();
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
    _dialogServiceConnector->Canceled += [&](const SpeechRecognitionCanceledEventArgs& event)
    {

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
    _dialogServiceConnector->ActivityReceived += [&](const ActivityReceivedEventArgs& event)
    {
        auto activity = nlohmann::json::parse(event.GetActivity());

        // Let's log the type and whether we have audio. Note this is how you access a property in the json. Here we are
        // reading the "type" value and defaulting to "" if it doesn't exist.
        log_t("ActivityReceived, type=", activity.value("type", ""), ", audio=", event.HasAudio() ? "true" : "false");

        if (activity.contains("text"))
        {
            log_t("activity[\"text\"]: ", activity["text"].get<string>());
        }

        auto continue_multiturn = activity.value<string>("inputHint", "") == "expectingInput";

        if (event.HasAudio())
        {
            log_t("Activity has audio, playing asynchronously.");

            if(!_bargeInSupported)
            {
                log_t("Pausing KWS during TTS playback");
                PauseKws();
            }

            auto audio = event.GetAudio();
            int play_result = 0;
    
            uint32_t total_bytes_read = 0;
            if(_volumeOn && _player != nullptr)
            {
                
                // If we are expecting more input and have audio to play, we will want to wait till all audio is done playing before
                // before listening again. We can read from the stream here to accomplish this.
                 if(continue_multiturn)
                 {
                    uint32_t playBufferSize = 1024;
                    unsigned int bytesRead = 0;
                    std::unique_ptr<unsigned char []> playBuffer = std::make_unique<unsigned char[]>(playBufferSize);
                    do{
                        bytesRead = audio->Read(playBuffer.get(), playBufferSize);
                        _player->Play(playBuffer.get(), bytesRead);
                        total_bytes_read += bytesRead;
                    }while(bytesRead > 0);
                    
                    DeviceStatusIndicators::SetStatus(DeviceStatus::Speaking);
                    
                    // We don't want to timeout while tts is playing so start 1 second before it is done
                    int secondsOfAudio = total_bytes_read / 32000;
                    std::this_thread::sleep_for(std::chrono::milliseconds((secondsOfAudio-1)*1000));
                }
                else
                {
                    play_result = _player->Play(audio);
                }
            }

            if (!continue_multiturn)
            {
                DeviceStatusIndicators::SetStatus(DeviceStatus::Idle);
            }
            
        }
        
        if (continue_multiturn)
        {
            log_t("Activity requested a continuation (ExpectingInput) -- listening again");
            // There may be an issue where the listening times out while the Audio is playing.
            ContinueListening();
        }
        else
        {
            if(!_bargeInSupported)
            {
                StartKws();
            }
        }
    };
}

void DialogManager::PauseKws()
{
    log_t("Enter PauseKws (state = ", uint32_t(_keywordActivationState), ")");

    if (_keywordActivationState == KeywordActivationState::Listening)
    {
        log_t("Stopping keyword recognition");
        auto future = _dialogServiceConnector->StopKeywordRecognitionAsync();
        _keywordActivationState = KeywordActivationState::Paused;
    }

    log_t("Exit PauseKws (state = ", uint32_t(_keywordActivationState), ")");
}

void DialogManager::StartKws()
{
    log_t("Enter StartKws (state = ", uint32_t(_keywordActivationState), ")");

    if (_keywordActivationState == KeywordActivationState::Paused)
    {
        auto modelPath = _agentConfig->KeywordModel();
        log_t("Initializing keyword recognition with: ", modelPath);
        auto model = KeywordRecognitionModel::FromFile(modelPath);
        auto _ = _dialogServiceConnector->StartKeywordRecognitionAsync(model);
        _keywordActivationState = KeywordActivationState::Listening;
        log_t("KWS initialized");
    }

    log_t("Exit StartKws (state = ", uint32_t(_keywordActivationState), ")");
}

void DialogManager::StartListening()
{
    log_t("Now listening...");
    if(_bargeInSupported)
    {
        _player->Stop();
    }
    DeviceStatusIndicators::SetStatus(DeviceStatus::Listening);
    auto future = _dialogServiceConnector->ListenOnceAsync();
}

void DialogManager::ContinueListening()
{
    log_t("Now listening...");
    DeviceStatusIndicators::SetStatus(DeviceStatus::Listening);
    auto future = _dialogServiceConnector->ListenOnceAsync();
};

void DialogManager::StopKws()
{
    log_t("Enter StopKws (state = ", uint32_t(_keywordActivationState), ")");

    if (_keywordActivationState == KeywordActivationState::Listening ||
        _keywordActivationState == KeywordActivationState::Paused)
    {
        if (_keywordActivationState == KeywordActivationState::Listening)
        {
            log_t("Stopping keyword recognition");
            auto future = _dialogServiceConnector->StopKeywordRecognitionAsync();
        }

        _keywordActivationState = KeywordActivationState::NotListening;
    }

    log_t("Exit StopKws (state = ", uint32_t(_keywordActivationState), ")");
}