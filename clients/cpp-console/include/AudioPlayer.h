
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "speechapi_cxx.h"

/// <summary>
/// Abstract object used to define the interface to an AudioPlayer
/// </summary>
/// <remarks>
/// </remarks>
class IAudioPlayer{
    public:
    
        /// <summary>
        /// AudioPlayerFormat is an enum that can be used in the AudioPlayer
        /// implementation. It is passed in as a parameter to the Open and Play functions.
        /// </summary>
        /// <example>
        /// <code>
        /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
        /// audioPlayer->Open("default",IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
        /// audioPlayer->Play("default",IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
        /// </code>
        /// </example>
        /// <remarks>
        /// </remarks>
        enum class AudioPlayerFormat
        {
            Mono16khz16bit,
            Stereo48khz16bit
        };
        
        /// <summary>
        /// Open will initialize the audio player with any specific OS dependent 
        /// settings. If called without parameters it should assume some appropriate
        /// defaults.
        /// </summary>
        /// <returns>A return code with < 0 as an error and any other int as success</returns>
        /// <example>
        /// <code>
        /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
        /// audioPlayer->Open();
        /// </code>
        /// </example>
        /// <remarks>
        /// Here we use the LinuxAudioPlayer as an example
        /// </remarks>
        virtual int Open() = 0;
        
        /// <summary>
        /// Open will initialize the audio player with any specific OS dependent 
        /// settings. This implementation should take a device name if necessary 
        /// and an AudioPlayFormat enum to be used in setting up the AudioPlayer
        /// </summary>
        /// <param name="device">The string name of the device to open</param>
        /// <param name="format">The AudioPlayerFormat enum</param>
        /// <returns>A return code with < 0 as an error and any other int as success</returns>
        /// <example>
        /// <code>
        /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
        /// audioPlayer->Open("default",IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
        /// </code>
        /// </example>
        /// <remarks>
        /// Here we use the LinuxAudioPlayer as an example
        /// </remarks>
        virtual int Open(const std::string& device, AudioPlayerFormat format) = 0;

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
        /// int bufferSize = 1024;
        /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
        /// // fill buffer with audio from somewhere
        /// audioPLayer->Play(buffer, bufferSize);
        /// </code>
        /// </example>
        /// <remarks>
        /// Here we use the LinuxAudioPlayer as an example.
        /// In our implementation we assume Open is called before playing.
        /// </remarks>
        virtual int Play(uint8_t* buffer, size_t bufferSize) = 0;

        /// <summary>
        /// This method is used to actually play the audio. The PullAudioOutputStream
        /// passed in should be taken from the GetAudio() call on the activity received event.
        /// </summary>
        /// <param name="pStream">A shared pointer to the PullAudioOutputStream</param>
        /// <returns>A return code with < 0 as an error and any other int as success</returns>
        /// <example>
        /// <code>
        /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
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
        /// Here we use the LinuxAudioPlayer as an example. This is preferred to the Byte array if possible
        /// since this will not cause copies of the buffer to be stored at runtime.
        /// In our implementation we assume Open is called before playing.
        /// </remarks>
        virtual int Play(std::shared_ptr<Microsoft::CognitiveServices::Speech::Audio::PullAudioOutputStream> pStream) = 0;
        
        /// <summary>
        /// This function is used to programmatically set the volume of the audio player
        /// </summary>
        /// <returns>A return code with < 0 as an error and any other int as success</returns>
        /// <example>
        /// <code>
        /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
        /// audioPlayer->Open();
        /// audioPlayer->SetVolume(50);
        /// </code>
        /// </example>
        /// <remarks>
        /// Here we use the LinuxAudioPlayer as an example. Though not all players will support this. See the implementation file for details.
        /// </remarks>
        virtual int SetVolume(unsigned int percent) = 0;
        
        /// <summary>
        /// This function is used to clean up the audio players resources.
        /// </summary>
        /// <returns>A return code with < 0 as an error and any other int as success</returns>
        /// <example>
        /// <code>
        /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
        /// audioPlayer->Open();
        /// int bufferSize = 1024;
        /// unsigned char * buffer = (unsigned char *)malloc(bufferSize);
        /// // fill buffer with audio from somewhere
        /// audioPLayer->Play(buffer, bufferSize, IAudioPlayer::AudioPlayerFormat::Mono16khz16bit);
        /// audioPlayer->Close();
        /// </code>
        /// </example>
        /// <remarks>
        /// Here we use the LinuxAudioPlayer as an example.
        /// </remarks>
        virtual int Close() = 0;
};