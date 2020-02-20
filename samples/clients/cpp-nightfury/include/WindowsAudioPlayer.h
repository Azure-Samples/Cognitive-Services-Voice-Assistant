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
    /// Windows
    /// </summary>
    /// <remarks>
    /// </remarks>
    class WindowsAudioPlayer :public IAudioPlayer{
        public:
        
            /// <summary>
            /// Default constructor for the WindowsAudioPlayer. Here we will start a thread
            /// to play audio and open the default audio device in Mono16khz16bit
            /// </summary>
            /// <returns>a WindowsAudioPlayer object</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer * audioPlayer = new WindowsAudioPlayer();
            /// </code>
            /// </example>
            /// <remarks>
            /// </remarks>
            WindowsAudioPlayer();
            
            /// <summary>
            /// Default destructor for the WindowsAudioPlayer. Closes the audio player and cleans up the thread
            /// </summary>
            /// <returns>a WindowsAudioPlayer object</returns>
            /// <example>
            /// <code>
            /// </code>
            /// </example>
            /// <remarks>
            /// </remarks>
            ~WindowsAudioPlayer();
            
            /// <summary>
            /// Open the default audio device for ALSA and uses the Mono16khz16bit format.
            /// </summary>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer * audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// </code>
            /// </example>
            /// <remarks>
            /// </remarks>
            int Open();
            
            /// <summary>
            /// Open will initialize the audio player with any specific OS dependent 
            /// settings. This implementation takes an ALSA device name and an AudioPlayFormat 
            /// enum to be used in setting up the AudioPlayer.
            /// </summary>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open("default",IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
            /// </code>
            /// </example>
            /// <remarks>
            /// This will force the audio device to be closed and reopened to ensure the specified format.
            /// </remarks>
            int Open(const std::string& device, AudioPlayerFormat format);
            
            
            /// <summary>
            /// ALSA expects audio to be sent in periods defined by frames. This function will compute the
            /// buffer size based on the channels, bytes per sample, and frames per period.
            /// </summary>
            /// <returns>An integer representing the expected buffer size in bytes</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// int bufferSize = audioPlayer->GetBufferSize();
            /// </code>
            /// </example>
            /// <remarks>
            /// </remarks>
            int GetBufferSize();
            
            /// <summary>
            /// This method is used to actually play the audio. The buffer passed in 
            /// should contain the raw audio bytes.
            /// </summary>
            /// <param name="buffer">A point to the buffer containing the audio bytes</param>
            /// <param name="bufferSize">The size in bytes of the buffer being passed in.</param>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// int bufferSize = audioPlayer->GetBufferSize();
            /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
            /// // fill buffer with audio from somewhere
            /// audioPlayer->Play(buffer, bufferSize);
            /// </code>
            /// </example>
            /// <remarks>
            /// The method returns the number of frames written to ALSA.
            /// We assume Open has already been called.
            /// </remarks>
            int Play(uint8_t* buffer, size_t bufferSize);
            
            /// <summary>
            /// This method is used to actually play the audio. The buffer passed in 
            /// should contain the raw audio bytes. The AudioPlayerFormat is used to determine how to play it.
            /// </summary>
            /// <param name="buffer">A point to the buffer containing the audio bytes</param>
            /// <param name="bufferSize">The size in bytes of the buffer being passed in.</param>
            /// <param name="format">The AudioPlayerFormat enum to define the settings for the audio player</param>
            /// <returns>A return code with < 0 as an error and any other int as success. 
            /// Non-errors are the number of frames written.</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// int bufferSize = audioPlayer->GetBufferSize();
            /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
            /// // fill buffer with audio from somewhere
            /// audioPLayer->Play(buffer, bufferSize, IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
            /// </code>
            /// </example>
            /// <remarks>
            /// The method returns the number of frames written to ALSA.
            /// In our implementation we assume Open is called before playing.
            /// </remarks>
            int Play(uint8_t* buffer, size_t bufferSize, AudioPlayerFormat format);
            
            /// <summary>
            /// This function is used to clean up the audio players resources.
            /// </summary>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// int bufferSize = audioPlayer->GetBufferSize();
            /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
            /// // fill buffer with audio from somewhere
            /// audioPLayer->Play(buffer, bufferSize, IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
            /// audioPlayer->Close();
            /// </code>
            /// </example>
            /// <remarks>
            /// </remarks>
            int Close();
        
        private:
            bool                    m_isPlaying = false;
            bool                    m_canceled = false;
            bool                    m_opened = false;
            std::string             m_device;
            std::mutex              m_queueMutex;
            std::mutex              m_threadMutex;
            std::condition_variable m_conditionVariable;
            
            std::list<AudioPlayerEntry> m_audioQueue;

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

            /// <summary>
            /// Call this to stop playback in progress. You can call this method anytime.
            /// </summary>
            HRESULT Stop();

            HRESULT Initialize(
                _Inout_opt_ HANDLE hRenderingDoneEvent,
                _In_ WAVEFORMATEX* pwf,
                _In_count_(renderBufferSizeInSamples) INT16* pRenderBuffer,
                _In_ DWORD renderBufferSizeInSamples,
                _In_ bool loopOnFlag);

            HRESULT GetFramesRendered(
                _Inout_opt_ DWORD* pFrames,
                _Inout_opt_ DWORD* pTotalFrames);

            static DWORD WINAPI DoRenderThread(_In_  LPVOID lpParameter);
            HRESULT DoRenderThread();

            static inline bool IsValidHandle(const HANDLE& h) {
                return ((h != INVALID_HANDLE_VALUE) && (h != 0));
            }

            std::thread m_playerThread;
            void PlayerThreadMain();
            int WriteToDriver(uint8_t* buffer, size_t bufferSize);
    };
}