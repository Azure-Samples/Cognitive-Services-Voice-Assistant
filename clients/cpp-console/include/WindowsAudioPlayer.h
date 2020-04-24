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
            /// Default constructor for the WindowsAudioPlayer.
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
            virtual int Open() final;
            
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
            virtual int Open(const std::string& device, AudioPlayerFormat format) final;
            
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
            virtual int Play(uint8_t* buffer, size_t bufferSize) final;
            
            /// <summary>
            /// This method is used to actually play the audio. The PullAudioOutputStream
            /// passed in should be taken from the GetAudio() call on the activity received event.
            /// </summary>
            /// <param name="pStream">A shared pointer to the PullAudioOutputStream</param>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// ... 
            ///
            /// //In the Activity received callback
            /// if (event.HasAudio()){
            ///     std::shared_ptr<Audio::PullAudioOutputStream> stream = event.GetAudio();
            ///     audioPLayer->Play(stream);
            /// }
            /// </code>
            /// </example>
            /// <remarks>
            /// Here we use the WindowsAudioPlayer as an example. This is preferred to the Byte array if possible
            /// since this will not cause copies of the buffer to be stored at runtime.
            /// In our implementation we assume Open is called before playing.
            /// </remarks>
            virtual int Play(std::shared_ptr<Microsoft::CognitiveServices::Speech::Audio::PullAudioOutputStream> pStream) final;
            
            /// <summary>
            /// This method is used to stop all playback. This will clear any queued audio meaning that any audio yet to play will be lost.
            /// </summary>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Play(...);
            /// audioPlayer->StopAllPlayback();
            /// </example>
            /// <remarks>
            /// In our implementation we assume Open is called before playing.
            /// </remarks>
            virtual int StopAllPlayback() final;
            
            /// <summary>
            /// This function is a no-op
            /// </summary>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// audioPlayer->SetVolume(50);
            /// </code>
            /// </example>
            /// <remarks>
            /// In this case volume is handled by windows and not set here
            /// </remarks>
            virtual int SetVolume(unsigned int percent) final;
            
            /// <summary>
            /// This function is used to retrieve the current state of the player.
            /// </summary>
            /// <returns>An AudioPlayerState Enum</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer *audioPlayer = new WindowsAudioPlayer();
            /// audioPlayer->Open();
            /// audioPlayer->GetState();
            /// </code>
            /// </example>
            /// <remarks>
            /// States are defined in AudioPlayerState.h
            /// </remarks>
            virtual AudioPlayerState GetState() final;
            
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
            virtual int Close() final;
        
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

            static inline bool IsValidHandle(const HANDLE& h) {
                return ((h != INVALID_HANDLE_VALUE) && (h != 0));
            }

            std::thread m_playerThread;
            void PlayerThreadMain();
            void PlayByteBuffer(std::shared_ptr<AudioPlayerEntry> pEntry);
            void PlayPullAudioOutputStream(std::shared_ptr<AudioPlayerEntry> pEntry);
    };
}