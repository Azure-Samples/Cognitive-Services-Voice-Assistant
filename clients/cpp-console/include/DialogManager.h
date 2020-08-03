// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AgentConfiguration.h"
#include "DeviceStatusIndicators.h"
#include "speechapi_cxx.h"
#include <fstream>
#include "AudioPlayerStreamImpl.h"

#ifdef LINUX
#include "LinuxAudioPlayer.h"
#include "LinuxMicMuter.h"
#endif

#ifdef WINDOWS
#include <Windows.h>
#include "WindowsAudioPlayer.h"
#include "WindowsMicMuter.h"
#endif

using namespace std;
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace Microsoft::CognitiveServices::Speech::Audio;

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

class DialogManager
{
public:
    DialogManager(shared_ptr<AgentConfiguration> agentConfig);
    DialogManager(shared_ptr<AgentConfiguration> agentConfig, string audioFilePath);
    const DeviceStatus GetDeviceStatus() { return _deviceStatus; };
    const KeywordActivationState GetKeywordActivationState() { return _keywordActivationState; }
    // Start a listening session that will terminate after the first utterance.
    void StartListening();
    // Stop audio player, re-initialize back end connection, and restart Keyword spotting if it is supported.
    void Stop();
    // Mute and unmute default microphone.
    void MuteUnMute();
    // Initiate keyword recognition.
    void StartKws();
    // Stop keyword recognition.
    void StopKws();
    // Start a listening session that read audio stream from a wav file.
    void ListenFromFile();
    // Get mute state of the default microphone.
    bool IsMuted() { return _muter ? _muter->IsMuted() : false; };

private:
    bool _volumeOn = false;
    bool _bargeInSupported = false;
    string _audioFilePath = "";
    DeviceStatus _deviceStatus = DeviceStatus::Initializing;
    KeywordActivationState _keywordActivationState = KeywordActivationState::Undefined;
    IAudioPlayer* _player;
    shared_ptr <IMicMuter> _muter;
    shared_ptr<AgentConfiguration> _agentConfig;
    shared_ptr<DialogServiceConnector> _dialogServiceConnector;
    shared_ptr<PushAudioInputStream> _pushStream;
    void InitializeDialogServiceConnectorFromMicrophone();
    void InitializeDialogServiceConnectorFromFile();
    void InitializePlayer();
    void InitializeMuter();
    void AttachHandlers();
    void InitializeConnection();
    void SetDeviceStatus(const DeviceStatus status);
    void SetKeywordActivationState(const KeywordActivationState& state) { _keywordActivationState = state; }
    void ContinueListening();
    void ResumeKws();
    void PauseKws();
    fstream OpenFile(const string& audioFilePath);
    int ReadBuffer(fstream& fs, uint8_t* dataBuffer, uint32_t size);
    void PushData(const string& audioFilePath);
};
