// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AgentConfiguration.h"
#include "DeviceStatusIndicators.h"
#include "speechapi_cxx.h"
#include <fstream>

#ifdef LINUX
#include "LinuxAudioPlayer.h"
#endif

#ifdef WINDOWS
#include <Windows.h>
#include "WindowsAudioPlayer.h"
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
    const KeywordActivationState GetKeywordActivationState() { return _keywordActivationState; }
    void SetKeywordActivationState(const KeywordActivationState& state) { _keywordActivationState = state; }
    void PauseKws();
    void StartKws();
    void StartListening();
    void StopListening();
    void MuteListening();
    void UnmuteListening();
    void ContinueListening();
    void StopKws();
    void ListenFromFile();

private:
    bool _volumeOn = false;
    bool _bargeInSupported = false;
    string _audioFilePath = "";
    KeywordActivationState _keywordActivationState = KeywordActivationState::Undefined;
    IAudioPlayer* _player;
    shared_ptr<AgentConfiguration> _agentConfig;
    shared_ptr<DialogServiceConnector> _dialogServiceConnector;
    shared_ptr<PushAudioInputStream> _pushStream;
    void InitializeDialogServiceConnectorFromMicrophone();
    void InitializeDialogServiceConnectorFromFile();
    void InitializePlayer();
    void AttachHandlers();
    void InitializeConnection();
    fstream OpenFile(const string& audioFilePath);
    int ReadBuffer(fstream& fs, uint8_t* dataBuffer, uint32_t size);
    void PushData(const string& audioFilePath);
};
