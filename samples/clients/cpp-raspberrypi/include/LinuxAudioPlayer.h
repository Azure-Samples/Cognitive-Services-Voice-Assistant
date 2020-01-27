#include <alsa/asoundlib.h>
#include <condition_variable>
#include <list>
#include <thread>
#include <mutex>
#include "AudioPlayer.h"
#include "AudioPlayerEntry.h"

namespace AudioPlayer
{
    
    /// <summary>
    /// This object implemented the IAudioPlayer interface and handles the audio Playback for 
    /// Linux using the ALSA library.
    /// </summary>
    /// <remarks>
    /// </remarks>
    class LinuxAudioPlayer :public IAudioPlayer{
        public:
        
            /// <summary>
            /// Default constructor for the LinuxAudioPlayer.
            /// </summary>
            /// <returns>a LinuxAudioPlayer object</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer * audioPlayer = new LinuxAudioPlayer();
            /// </code>
            /// </example>
            /// <remarks>
            /// </remarks>
            LinuxAudioPlayer();
            
            /// <summary>
            /// Open the default audio device for ALSA and uses the Mono16khz16bit format.
            /// </summary>
            /// <returns>A return code with < 0 as an error and any other int as success</returns>
            /// <example>
            /// <code>
            /// IAudioPlayer * audioPlayer = new LinuxAudioPlayer();
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
            /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
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
            /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
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
            /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
            /// audioPlayer->Open();
            /// int bufferSize = audioPlayer->GetBufferSize();
            /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
            /// // fill buffer with audio from somewhere
            /// audioPLayer->Play(buffer, bufferSize);
            /// </code>
            /// </example>
            /// <remarks>
            /// The method returns the number of frames written to ALSA.
            /// We assume Open has already been called.
            /// </remarks>
            int Play(uint8_t* buffer, size_t bufferSize);
            
            int WriteToPCM(uint8_t* buffer);
            
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
            /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
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
            /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
            /// audioPlayer->Open();
            /// int bufferSize = audioPlayer->GetBufferSize();
            /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
            /// // fill buffer with audio from somewhere
            /// audioPLayer->Play(buffer, bufferSize, IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
            /// audioPlayer->Close();
            /// </code>
            /// </example>
            /// <remarks>
            /// The ALSA library is drained and closed.
            /// </remarks>
            int Close();
        
        private:
            
            snd_pcm_t*              m_playback_handle;
            snd_pcm_uframes_t       m_frames;
            snd_pcm_hw_params_t*    m_params;
            unsigned int            m_numChannels;
            unsigned int            m_bytesPerSample;
            unsigned int            m_bitsPerSecond;
            bool                    m_isPlaying = false;
            bool                    m_canceled = false;
            bool                    m_opened = false;
            std::mutex              m_queueMutex;
            std::mutex              m_threadMutex;
            std::condition_variable m_conditionVariable;
            
            std::list<AudioPlayerEntry> m_audioQueue;

            std::thread m_playerThread;
            void PlayerThreadMain();
    };
}