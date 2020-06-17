// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <condition_variable>
#include <list>
#include <thread>
#include <mutex>
#include <atlcore.h>
#include <mmdeviceapi.h>
#include <Audioclient.h>
#include <Windows.h>
#include "AudioPlayer.h"
#include "AudioPlayerEntry.h"

// REFERENCE_TIME time units per second and per millisecond
#define REFTIMES_PER_SEC  10000000
#define REFTIMES_PER_MILLISEC  10000

#define EXIT_ON_ERROR(hres)  \
              if (FAILED(hres)) { goto Exit; }
#define SAFE_RELEASE(punk)  \
              if ((punk) != NULL)  \
                { (punk)->Release(); (punk) = NULL; }

namespace AudioPlayer
{

    /// <summary>
    /// This object implemented the IAudioPlayer interface and handles the audio Playback for 
    /// Windows. See AudioPlayer.h for full documentation.
    /// </summary>
    /// <remarks>
    /// </remarks>
    class WindowsAudioPlayer :public IAudioPlayer
    {
    public:

        WindowsAudioPlayer();

        ~WindowsAudioPlayer();

        virtual int Initialize() final;

        virtual int Initialize(const std::string& device, AudioPlayerFormat format) final;

        virtual int Play(uint8_t* buffer, size_t bufferSize) final;

        virtual int Play(std::shared_ptr<IAudioPlayerStream> pStream) final;

        virtual int Stop() final;

        /// <summary>
        /// not implemented currently
        /// </summary
        virtual int Pause() final;

        /// <summary>
        /// not implemented currently
        /// </summary
        virtual int Resume() final;

        virtual int SetVolume(unsigned int percent) final;

        virtual AudioPlayerState GetState() final;

    private:
        bool                    m_canceled = false;
        bool                    m_shuttingDown = false;
        std::string             m_device;
        std::mutex              m_queueMutex;
        std::mutex              m_threadMutex;
        std::condition_variable m_conditionVariable;

        std::list<AudioPlayerEntry> m_audioQueue;

        AudioPlayerState m_state = AudioPlayerState::UNINITIALIZED;

        ATL::CComAutoCriticalSection m_cs;

        // Do not use CComPtr< > for these two because we need to control the order in which these interfaces are released
        IAudioClient* m_pAudioClient;
        IAudioRenderClient* m_pRenderClient;

        HANDLE m_hAudioClientEvent;  // WASAPI signals more data is needed for playback
        HANDLE m_hRenderThread; // Worker thread
        HANDLE m_hStartEvent; // Set by Start() to unblock worker thread
        HANDLE m_hStopEvent; // Set by Stop() to kill worker thread
        HANDLE m_hRenderingDoneEvent; // To signal the caller that rendering is done

        WAVEFORMATEX m_pwf; // Format of audio buffer

        INT16* m_renderBuffer;  // Points to audio buffer holding the calibration playback tone
        DWORD m_renderBufferOffsetInFrames; // Points to the next frame that has not yet been read
        DWORD m_renderBufferSizeInFrames; // Total number of frames in the render buffer

        BOOL m_loopRenderBufferFlag; // Playback data keeps looping same buffer when on

        DWORD m_muteChannelMask; // By defualt 0 (do not mute any channels), unless otherwise set by MuteChannels()

        static inline bool IsValidHandle(const HANDLE& h)
        {
            return ((h != INVALID_HANDLE_VALUE) && (h != 0));
        }

        std::thread m_playerThread;
        void PlayerThreadMain();
        void PlayByteBuffer(std::shared_ptr<AudioPlayerEntry> pEntry);
        void PlayAudioPlayerStream(std::shared_ptr<AudioPlayerEntry> pEntry);
        int Close();
    };
}