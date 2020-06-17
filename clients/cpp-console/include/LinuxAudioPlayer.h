// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <alsa/asoundlib.h>
#include <condition_variable>
#include <list>
#include <thread>
#include <mutex>
#include "AudioPlayer.h"
#include "AudioPlayerEntry.h"
#include "speechapi_cxx.h"

namespace AudioPlayer
{

    /// <summary>
    /// This object implemented the IAudioPlayer interface and handles the audio Playback for 
    /// Linux using the ALSA library. See AudioPlayer.h for full documentation.
    /// </summary>
    /// <remarks>
    /// </remarks>
    class LinuxAudioPlayer :public IAudioPlayer
    {
    public:
        LinuxAudioPlayer();

        ~LinuxAudioPlayer();

        virtual int Initialize() final;

        virtual int Initialize(const std::string& device, AudioPlayerFormat format) final;

        virtual int GetBufferSize() final;

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

        snd_pcm_t* m_playback_handle;
        snd_pcm_uframes_t       m_frames;
        snd_pcm_hw_params_t* m_params;
        unsigned int            m_numChannels;
        unsigned int            m_bytesPerSample;
        unsigned int            m_bitsPerSecond;
        bool                    m_canceled = false;
        bool                    m_shuttingDown;
        std::string             m_device;
        std::mutex              m_queueMutex;
        std::mutex              m_threadMutex;
        std::condition_variable m_conditionVariable;

        AudioPlayerState m_state = AudioPlayerState::UNINITIALIZED;

        std::list<AudioPlayerEntry> m_audioQueue;

        std::thread m_playerThread;
        void PlayerThreadMain();
        void PlayByteBuffer(std::shared_ptr<AudioPlayerEntry> pEntry);
        void PlayAudioPlayerStream(std::shared_ptr<AudioPlayerEntry> pEntry);
        int WriteToALSA(uint8_t* buffer);
        void SetAlsaMasterVolume(long volume);
        int Close();
    };
}