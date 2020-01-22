#include <string>
#include <alsa/asoundlib.h>
#include "LinuxAudioPlayer.h"

using namespace AudioPlayer;

LinuxAudioPlayer::LinuxAudioPlayer(){}

int LinuxAudioPlayer::Open(){
    int rc;
    rc = Open("default", AudioPlayerFormat::Mono16khz16bit);
    return rc;
}

int LinuxAudioPlayer::Open(const std::string& device, AudioPlayerFormat format){
    //PCM variables
    int rc;
    int err;
    snd_pcm_hw_params_t *params;
    unsigned int val;
    int dir;

    //begin PCM setup
    
    /* Open PCM device for playback. */
    if ((err = snd_pcm_open(&playback_handle, device.c_str(), SND_PCM_STREAM_PLAYBACK, 0)) < 0) {
    fprintf(stderr, "cannot open output audio device %s: %s\n", device.c_str(), snd_strerror(err));
    exit(1);
    }
    
    /* Allocate a hardware parameters object. */
    snd_pcm_hw_params_alloca(&params);

    /* Fill it in with default values. */
    snd_pcm_hw_params_any(playback_handle, params);

    /* Set the desired hardware parameters. */

    /* Interleaved mode */
    snd_pcm_hw_params_set_access(playback_handle, params,
                        SND_PCM_ACCESS_RW_INTERLEAVED);

    switch(format){
        case AudioPlayerFormat::Mono16khz16bit:
        default:
            /* Signed 16-bit little-endian format */
            fprintf(stdout, "Format = Mono16khz16bit\n");
            channels = 1;
            bytesPerSample = 2;
            bitsPerSecond = 16000;
            snd_pcm_hw_params_set_format(playback_handle, params,
                                SND_PCM_FORMAT_S16_LE);
    }

    /* set number of Channels */
    snd_pcm_hw_params_set_channels(playback_handle, params, channels);

    /* set bits/second sampling rate */
    snd_pcm_hw_params_set_rate_near(playback_handle, params,
                                    bitsPerSecond, &dir);

    /* Set period size to 32 frames. */
    frames = 32;
    snd_pcm_hw_params_set_period_size_near(playback_handle,
                                params, &frames, &dir);

    /* Write the parameters to the driver */
    rc = snd_pcm_hw_params(playback_handle, params);
    if (rc < 0) {
    fprintf(stderr,
            "unable to set hw parameters: %s\n",
            snd_strerror(rc));
    exit(1);
    }
    
    //end PCM setup
    return rc;
}

int LinuxAudioPlayer::GetBufferSize(){
    
    /* Use a buffer large enough to hold one period */
    snd_pcm_hw_params_get_period_size(params, &frames,
                                    &dir);
    int size = frames * bytesPerSample * channels; 
    return size;
}

int LinuxAudioPlayer::Play(uint8_t* buffer, size_t bufferSize){
    int rc = 0;
    
    rc = snd_pcm_writei(playback_handle, buffer, frames);
    if (rc == -EPIPE) {
        /* EPIPE means underrun */
        fprintf(stderr, "underrun occurred\n");
        snd_pcm_prepare(playback_handle);
    } else if (rc < 0) {
        fprintf(stderr,
                "error from writei: %s\n",
                snd_strerror(rc));
    }  else if (rc != (int)frames) {
        fprintf(stderr, "short write, write %d frames\n", rc);
    }
    
    return rc;
}

int LinuxAudioPlayer::Play(uint8_t* buffer, size_t bufferSize, AudioPlayerFormat format){
    return 0;
}

int LinuxAudioPlayer::Close(){
    snd_pcm_drain(playback_handle);
    snd_pcm_close(playback_handle);
    
    return 0;
}