#include <cstring>
#include <chrono>
#include <condition_variable>
#include <string>
#include <thread>
#include <mutex>
#include <alsa/asoundlib.h>
#include "LinuxAudioPlayer.h"

using namespace AudioPlayer;

void LinuxAudioPlayer::PlayerThreadMain(){
    while(m_canceled == false){
        std::unique_lock<std::mutex> lk{ m_threadMutex };
        m_conditionVariable.wait(lk);
        
        //fprintf(stdout, "checking for audio\n");
        unsigned int playBufferSize = GetBufferSize();
        while(m_audioQueue.size() > 0){
            m_isPlaying = true;
            AudioPlayerEntry entry = m_audioQueue.front();
            int bufferLeft = entry.m_size;
            std::unique_ptr<unsigned char []> playBuffer = std::make_unique<unsigned char[]>(playBufferSize);
            while(bufferLeft > 0){
                if(bufferLeft >= playBufferSize){
                    memcpy(playBuffer.get(), &entry.m_data[entry.m_size - bufferLeft], playBufferSize);
                    bufferLeft -= playBufferSize;
                }else { //there is a smaller amount to play so we will pad with silence
                    memcpy(playBuffer.get(), &entry.m_data[entry.m_size - bufferLeft], bufferLeft);
                    memset(playBuffer.get() + bufferLeft, 0, playBufferSize - bufferLeft);
                    bufferLeft = 0;
                }
                WriteToPCM(playBuffer.get());
            }
            
            m_queueMutex.lock();
            m_audioQueue.pop_front();
            m_queueMutex.unlock();
        }
        m_isPlaying = false;
        m_canceled = false;
        
    }
    
}

LinuxAudioPlayer::LinuxAudioPlayer(){
    
    fprintf(stdout, "creating thread\n");
    Open();
    m_playerThread = std::thread(&LinuxAudioPlayer::PlayerThreadMain, this);
    
}

int LinuxAudioPlayer::Open(){
    int rc;
    rc = Open("default", AudioPlayerFormat::Mono16khz16bit);
    return rc;
}

int LinuxAudioPlayer::Open(const std::string& device, AudioPlayerFormat format){
    //PCM variables
    int rc;
    int err;
    unsigned int val;
    int dir;

    //begin PCM setup
    
    /* Open PCM device for playback. */
    if ((err = snd_pcm_open(&m_playback_handle, device.c_str(), SND_PCM_STREAM_PLAYBACK, 0)) < 0) {
    fprintf(stderr, "cannot open output audio device %s: %s\n", device.c_str(), snd_strerror(err));
    exit(1);
    }
    
    /* Allocate a hardware parameters object. */
    snd_pcm_hw_params_alloca(&m_params);

    /* Fill it in with default values. */
    snd_pcm_hw_params_any(m_playback_handle, m_params);

    /* Set the desired hardware parameters. */

    /* Interleaved mode */
    snd_pcm_hw_params_set_access(m_playback_handle, m_params,
                        SND_PCM_ACCESS_RW_INTERLEAVED);

    switch(format){
        case AudioPlayerFormat::Mono16khz16bit:
        default:
            /* Signed 16-bit little-endian format */
            fprintf(stdout, "Format = Mono16khz16bit\n");
            m_numChannels = 1;
            m_bytesPerSample = 2;
            m_bitsPerSecond = 16000;
            snd_pcm_hw_params_set_format(m_playback_handle, m_params,
                                SND_PCM_FORMAT_S16_LE);
    }

    /* set number of Channels */
    snd_pcm_hw_params_set_channels(m_playback_handle, m_params, m_numChannels);

    /* set bits/second sampling rate */
    snd_pcm_hw_params_set_rate_near(m_playback_handle, m_params,
                                    &m_bitsPerSecond, &dir);

    /* Set period size to 32 frames. */
    m_frames = 32;
    snd_pcm_hw_params_set_period_size_near(m_playback_handle,
                                m_params, &m_frames, &dir);

    /* Write the parameters to the driver */
    rc = snd_pcm_hw_params(m_playback_handle, m_params);
    if (rc < 0) {
    fprintf(stderr,
            "unable to set hw parameters: %s\n",
            snd_strerror(rc));
    exit(1);
    }
    
    //end PCM setup
    m_opened = true;
    return rc;
}

int LinuxAudioPlayer::GetBufferSize(){
    int dir;
    
    /* Use a buffer large enough to hold one period */
    snd_pcm_hw_params_get_period_size(m_params, &m_frames,
                                    &dir);
    int size = m_frames * m_bytesPerSample * m_numChannels; 
    return size;
}

int LinuxAudioPlayer::Play(uint8_t* buffer, size_t bufferSize){
    int rc = 0;
    AudioPlayerEntry entry(buffer, bufferSize);
    
    m_queueMutex.lock();
    m_audioQueue.push_back(entry);
    m_queueMutex.unlock();
    
    if(!m_isPlaying){
        m_conditionVariable.notify_one();
    }
    // rc = snd_pcm_writei(m_playback_handle, buffer, m_frames);
    // if (rc == -EPIPE) {
    //     /* EPIPE means underrun */
    //     fprintf(stderr, "underrun occurred\n");
    //     snd_pcm_prepare(m_playback_handle);
    // } else if (rc < 0) {
    //     fprintf(stderr,
    //             "error from writei: %s\n",
    //             snd_strerror(rc));
    // }  else if (rc != (int)m_frames) {
    //     fprintf(stderr, "short write, write %d frames\n", rc);
    // }
    
    return rc;
}

int LinuxAudioPlayer::WriteToPCM(uint8_t* buffer){
    int rc = 0;
    
    rc = snd_pcm_writei(m_playback_handle, buffer, m_frames);
    if (rc == -EPIPE) {
        /* EPIPE means underrun */
        fprintf(stderr, "underrun occurred\n");
        snd_pcm_prepare(m_playback_handle);
    } else if (rc < 0) {
        fprintf(stderr,
                "error from writei: %s\n",
                snd_strerror(rc));
    }  else if (rc != (int)m_frames) {
        fprintf(stderr, "short write, write %d frames\n", rc);
    }
    
    return rc;
}

int LinuxAudioPlayer::Play(uint8_t* buffer, size_t bufferSize, AudioPlayerFormat format){
    return 0;
}

int LinuxAudioPlayer::Close(){
    snd_pcm_drain(m_playback_handle);
    snd_pcm_close(m_playback_handle);
    
    return 0;
}