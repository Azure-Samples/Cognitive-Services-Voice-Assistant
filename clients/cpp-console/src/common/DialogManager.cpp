// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <thread>
#include "log.h"
#include "DialogManager.h"

using namespace std;
using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Audio;
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace AudioPlayer;
using namespace MicMuter;

DialogManager::DialogManager(shared_ptr<AgentConfiguration> agentConfig)
{
    _agentConfig = agentConfig;

    SetDeviceStatus(DeviceStatus::Initializing);

    InitializeDialogServiceConnectorFromMicrophone();
    InitializePlayer();
    InitializeMuter();
    AttachHandlers();
    InitializeConnection();

    // Activate keyword listening on start up if keyword model file exists
    if (_agentConfig->KeywordRecognitionModel().length() > 0)
    {
        StartKws();
    }
    else
    {
        SetKeywordActivationState(KeywordActivationState::NotSupported);
    }

    SetDeviceStatus(DeviceStatus::Ready);
}

DialogManager::DialogManager(shared_ptr<AgentConfiguration> agentConfig, string audioFilePath)
{
    _agentConfig = agentConfig;
    _audioFilePath = audioFilePath;

    SetDeviceStatus(DeviceStatus::Initializing);

    InitializeDialogServiceConnectorFromFile();
    InitializePlayer();
    InitializeMuter();
    AttachHandlers();
    InitializeConnection();

    SetDeviceStatus(DeviceStatus::Ready);
}

void DialogManager::InitializeDialogServiceConnectorFromMicrophone()
{
    log_t("Configuration loaded. Creating connector...");

    // MAS stands for Microsoft Audio Stack
#ifdef MAS
    auto config = _agentConfig->AsDialogServiceConfig();
    auto audioConfig = AudioConfig::FromMicrophoneInput(_agentConfig->_linuxCaptureDeviceName);
    config->SetProperty("MicArrayGeometryConfigFile", _agentConfig->_customMicConfigPath);
    _dialogServiceConnector = DialogServiceConnector::FromConfig(config, audioConfig);
#endif
#ifndef MAS
    _dialogServiceConnector = DialogServiceConnector::FromConfig(_agentConfig->AsDialogServiceConfig());
#endif
    log_t("Connector created");
}

void DialogManager::InitializePlayer()
{
    if (_agentConfig->_barge_in_supported == "true")
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

void DialogManager::InitializeMuter()
{
#ifdef LINUX
    _muter = make_shared<LinuxMicMuter>();
#endif
#ifdef WINDOWS
    _muter = make_shared<WindowsMicMuter>();
#endif

    string result = (_muter->Initialize() == 0) ? "succeeded." : "failed.";
    log_t("Initializing Microphone Muter " + result);
}

void DialogManager::SetDeviceStatus(const DeviceStatus status)
{
    _deviceStatus = status;
    DeviceStatusIndicators::SetStatus(_deviceStatus, IsMuted());
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
        SetDeviceStatus(DeviceStatus::Detecting);
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
        SetDeviceStatus(newStatus);
    };

    // Signal for events relating to the cancellation of an interaction. The event indicates if the reason is a direct cancellation or an error.
    _dialogServiceConnector->Canceled += [&](const SpeechRecognitionCanceledEventArgs& event)
    {

        printf("CANCELED: Reason=%d\n", (int)event.Reason);
        SetDeviceStatus(DeviceStatus::Idle);
        if (event.Reason == CancellationReason::Error)
        {
            printf("CANCELED: ErrorDetails=%s\n", event.ErrorDetails.c_str());
            printf("CANCELED: Did you update the subscription info?\n");
            ResumeKws();
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

            if (!_bargeInSupported)
            {
                log_t("Pausing KWS during TTS playback");
                PauseKws();
            }

            auto audio = event.GetAudio();
            int play_result = 0;

            uint32_t total_bytes_read = 0;
            if (_volumeOn && _player != nullptr)
            {

                // If we are expecting more input and have audio to play, we will want to wait till all audio is done playing before
                // before listening again. We can read from the stream here to accomplish this.
                if (continue_multiturn)
                {
                    uint32_t playBufferSize = 1024;
                    unsigned int bytesRead = 0;
                    std::unique_ptr<unsigned char[]> playBuffer = std::make_unique<unsigned char[]>(playBufferSize);
                    do
                    {
                        bytesRead = audio->Read(playBuffer.get(), playBufferSize);
                        _player->Play(playBuffer.get(), bytesRead);
                        total_bytes_read += bytesRead;
                    } while (bytesRead > 0);

                    SetDeviceStatus(DeviceStatus::Speaking);

                    // We don't want to timeout while tts is playing so start 1 second before it is done
                    int secondsOfAudio = total_bytes_read / 32000;
                    std::this_thread::sleep_for(std::chrono::milliseconds((secondsOfAudio - 1) * 1000));
                }
                else
                {
                    std::shared_ptr<IAudioPlayerStream> playerStream = std::make_shared<AudioPlayerStreamImpl>(audio);
                    play_result = _player->Play(playerStream);
                }
            }

            if (!continue_multiturn)
            {
                SetDeviceStatus(DeviceStatus::Idle);
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
            if (!_bargeInSupported)
            {
                ResumeKws();
            }
        }
    };
}

void DialogManager::StartKws()
{
    log_t("Enter StartKws (state = ", uint32_t(_keywordActivationState), ")");

    auto modelPath = _agentConfig->KeywordRecognitionModel();
    log_t("Initializing keyword recognition with: ", modelPath);
    auto model = KeywordRecognitionModel::FromFile(modelPath);
    auto _ = _dialogServiceConnector->StartKeywordRecognitionAsync(model);
    _keywordActivationState = KeywordActivationState::Listening;
    log_t("KWS initialized");

    log_t("Exit StartKws (state = ", uint32_t(_keywordActivationState), ")");
}

void DialogManager::ResumeKws()
{
    if (_keywordActivationState == KeywordActivationState::Paused)
    {
        StartKws();
    }
}

void DialogManager::StartListening()
{
    _player->Stop();

    ContinueListening();
}

void DialogManager::ContinueListening()
{
    log_t("Now listening...");
    SetDeviceStatus(DeviceStatus::Listening);
    auto future = _dialogServiceConnector->ListenOnceAsync();
};

void DialogManager::Stop()
{
    log_t("Now stopping...");

    if (_player != nullptr)
    {
        _player->Stop();
    }
    auto future = _dialogServiceConnector->DisconnectAsync();
    InitializeConnection();
    if (_keywordActivationState != KeywordActivationState::NotSupported)
    {
        StartKws();
    }
    SetDeviceStatus(DeviceStatus::Ready);
}

void DialogManager::MuteUnMute()
{
    if (_muter->MuteUnmute() == 0)
    {
        string result = _muter->IsMuted() ? "muted." : "unmuted.";
        log_t("Microphone is " + result);
    }
    else
    {
        log_t("Mute/UnMute microphone failed.");
    }
}

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

fstream DialogManager::OpenFile(const string& audioFilePath)
{
    if (audioFilePath.empty())
    {
        throw invalid_argument("Audio filename is empty");
    }

    fstream fs;
    fs.open(audioFilePath, ios_base::binary | ios_base::in);
    if (!fs.good())
    {
        throw invalid_argument("Failed to open the specified audio file.");
    }

    return fs;
}

int DialogManager::ReadBuffer(fstream& fs, uint8_t* dataBuffer, uint32_t size)
{
    if (fs.eof())
    {
        // returns 0 to indicate that the stream reaches end.
        return 0;
    }

    fs.read((char*)dataBuffer, size);

    if (!fs.eof() && !fs.good())
    {
        // returns 0 to close the stream on read error.
        return 0;
    }
    else
    {
        // returns the number of bytes that have been read.
        return (int)fs.gcount();
    }
}

void DialogManager::PushData(const string& audioFilePath)
{
    fstream fs;
    try
    {
        fs = OpenFile(audioFilePath);
        //skip the wave header
        fs.seekg(44);
    }
    catch (const exception& e)
    {
        cerr << "Error: exception in pushData, %s." << e.what() << endl;
        cerr << "  can't open " << audioFilePath << endl;
        throw e;
        return;
    }

    std::array<uint8_t, 1000> buffer;
    while (1)
    {
        auto readSamples = ReadBuffer(fs, buffer.data(), (uint32_t)buffer.size());
        if (readSamples == 0)
        {
            break;
        }
        _pushStream.get()->Write(buffer.data(), readSamples);
    }
    fs.close();
    _pushStream.get()->Close();
}

void DialogManager::ListenFromFile()
{
    PushData(_audioFilePath);
    _dialogServiceConnector->ListenOnceAsync();
}

void DialogManager::InitializeDialogServiceConnectorFromFile()
{
    log_t("Configuration loaded. Creating connector...");
    shared_ptr<DialogServiceConfig> config = _agentConfig->CreateDialogServiceConfig();
    _pushStream = AudioInputStream::CreatePushStream();
    auto audioConfig = AudioConfig::FromStreamInput(_pushStream);

    _dialogServiceConnector = DialogServiceConnector::FromConfig(config, audioConfig);
    log_t("Connector created");
}

void DialogManager::InitializeConnection()
{
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
