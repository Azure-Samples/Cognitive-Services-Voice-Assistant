
/// <summary>
/// Abstract object used to define the interface to an AudioPlayer
/// </summary>
/// <remarks>
/// </remarks>
class IAudioPlayer{
    public:
    
    /// <summary>
    /// AudioPlayerFormat is an enum that can be used in the AudioPlayer
    /// implementation. It is passed in as a parameter tot he Open and Play functions.
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
    /// Not all audio hardware or software devices are the same. This function 
    /// should be used to obtain the buffer size in bytes that the player 
    /// expects.
    /// </summary>
    /// <returns>An integer representing the expected buffer size in bytes</returns>
    /// <example>
    /// <code>
    /// IAudioPlayer *audioPlayer = new LinuxAudioPlayer();
    /// int bufferSize = audioPlayer->GetBufferSize();
    /// </code>
    /// </example>
    /// <remarks>
    /// Here we use the LinuxAudioPlayer as an example
    /// </remarks>
    virtual int GetBufferSize() = 0;
    
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
    /// Here we use the LinuxAudioPlayer as an example. It returns the number of frames written to ALSA.
    /// In our implementation we assume Open is called before playing.
    /// </remarks>
    virtual int Play(uint8_t* buffer, size_t bufferSize) = 0;
    
    /// <summary>
    /// This method is used to actually play the audio. The buffer passed in 
    /// should contain the raw audio bytes. The AudioPlayerFormat is used to determine how to play it.
    /// </summary>
    /// <param name="buffer">A point to the buffer containing the audio bytes</param>
    /// <param name="bufferSize">The size in bytes of the buffer being passed in.</param>
    /// <param name="format">The AudioPlayerFormat enum to define the settings for the audio player</param>
    /// <returns>A return code with < 0 as an error and any other int as success</returns>
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
    /// Here we use the LinuxAudioPlayer as an example. It returns the number of frames written to ALSA.
    /// In our implementation we assume Open is called before playing.
    /// </remarks>
    virtual int Play(uint8_t* buffer, size_t bufferSize, AudioPlayerFormat format) = 0;
    
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
    /// Here we use the LinuxAudioPlayer as an example.
    /// </remarks>
    virtual int Close() = 0;
};