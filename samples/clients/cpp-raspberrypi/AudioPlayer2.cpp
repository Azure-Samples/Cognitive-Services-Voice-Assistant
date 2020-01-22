//
// Copyright (c) Microsoft Corporation. All rights reserved.
//

#include <tinyalsa/asoundlib.h>
#include <iostream>
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <string>
#include <memory>
#include <algorithm>

#include "AudioPlayer2.h"

static std::string GetDeviceIdentifier()
{
    FILE* fp = std::fopen("/proc/asound/cards", "r");
    if (fp == NULL)
    {
        fprintf(stderr, "Failed to read file /proc/asound/cards");
        return "";
    }

    char line[128];
    char* ptr = std::fgets(line, sizeof(line), fp);
    fclose(fp);

    if (ptr == NULL)
    {
        fprintf(stderr, "Failed to read file /proc/asound/cards");
        return "";
    }
    if (strstr(line, "qcs405csra1sndc") != NULL)
    {
        return "GGEC";
    }
    if (strstr(line, "qcs405wsasndcar") != NULL)
    {
        return "SSRD";
    }
    fprintf(stderr, "Unknown device detected");
    return "";
}

int AudioPlayer2::Open()
{
    return Open(GetDeviceIdentifier(), AudioPlayer2Format::Mono16khz16bit);
}

int AudioPlayer2::Open(const std::string& device, AudioPlayer2Format format)
{
    std::cout << "AudioPlayer2::Open: " << device << std::endl;

    if (!!_pcmHandle)
    {
        std::cout << "AudioPlayer2::Open calling Close() for existing handle" << std::endl;
        Close();
    }

    if (device.empty())
    {
        return -1;
    }

    // Edit mixer controls
    if (device == "GGEC")
    {
        system("tinymix set 'PRI_MI2S_RX Audio Mixer MultiMedia1' 1"); 
        system("tinymix set 'PRIM_MI2S_RX Channels' 'Two'");
    }
    else
    {
        system("tinymix set 'WSA_CDC_DMA_RX_0 Audio Mixer MultiMedia1' '1'");
        system("tinymix set 'WSA_CDC_DMA_RX_0 Channels' 'Two'");
        system("tinymix set 'WSA RX0 MUX' 'AIF1_PB'");
        system("tinymix set 'WSA RX1 MUX' 'AIF1_PB'");
        system("tinymix set 'WSA_RX0 INP0' 'RX0'");
        system("tinymix set 'WSA_RX1 INP0' 'RX1'");
        system("tinymix set 'WSA_COMP1 Switch' '1'");
        system("tinymix set 'WSA_COMP2 Switch' '1'");
        system("tinymix set 'SpkrLeft COMP Switch' '1'");
        system("tinymix set 'SpkrLeft BOOST Switch' '1'");
        system("tinymix set 'SpkrLeft VISENSE Switch' '1'");
        system("tinymix set 'SpkrLeft SWR DAC_Port Switch' '1'");
        system("tinymix set 'SpkrRight COMP Switch' '1'");
        system("tinymix set 'SpkrRight BOOST Switch' '1'");
        system("tinymix set 'SpkrRight VISENSE Switch' '1'");
        system("tinymix set 'SpkrRight SWR DAC_Port Switch' '1'");
    }

    _format = format;

    // struct pcm_config config;
    switch (format)
    {
    case AudioPlayer2Format::Mono16khz16bit:
        _config.channels = 1;
        _config.rate = 16000;
        break;
    case AudioPlayer2Format::Stereo48khz16bit:
        _config.channels = 2;
        _config.rate = 48000;
        break;
    default:
        break;
    }

    _config.format = PCM_FORMAT_S16_LE;
    _config.period_size = 1024;
    _config.period_count = 4;
    _config.start_threshold = 0;
    _config.silence_threshold = 0;
    _config.stop_threshold = 0;

    // Open render device
    std::cout << "AudioPlayer2::Open calling pcm_open..." << std::endl;
    _pcmHandle = pcm_open(0, 0, PCM_OUT, &_config);
    std::cout << "AudioPlayer2::Open pcm_open DONE" << std::endl;

    if (_pcmHandle == NULL)
    {
        fprintf(stderr, "Failed to allocate memory for PCM to play audio");
        return -1;
    }

    std::cout << "AudioPlayer2: Checking pcm_is_ready" << std::endl;

    if (!pcm_is_ready(_pcmHandle))
    {
        fprintf(stderr, "Failed to open device: %s", pcm_get_error(_pcmHandle));
        Close();
        return -1;
    }

    std::cout << "AudioPlayer2: pcm_is_ready DONE" << std::endl;

    return 0;
}

int AudioPlayer2::Play(uint8_t* buffer, size_t bufferSize)
{
    if (!_pcmHandle)
    {
        std::cout << "AudioPlayer2::Play called without open handle!" << std::endl;
        return -1;
    }

    int writeResult = pcm_write(_pcmHandle, (void*)buffer, bufferSize);
    if (writeResult < 0)
    {
        std::cout << "Error (" << writeResult << ") writing audio" << std::endl;
        return -1;
    }

    return 0;
}

int AudioPlayer2::Play(uint8_t* buffer, size_t bufferSize, AudioPlayer2Format format)
{
    // 16 to 16 or 48 to 48: just play it
    if (format == _format)
    {
        return Play(buffer, bufferSize);
    }
    // 48 to 16: not implemented
    else if (format == AudioPlayer2Format::Stereo48khz16bit && _format == AudioPlayer2Format::Mono16khz16bit)
    {
        return -1;
    }
    // 16 to 48: convert and play
    else if (format == AudioPlayer2Format::Mono16khz16bit && _format == AudioPlayer2Format::Stereo48khz16bit)
    {
        // Convert mono 16KHz to stereo 48KHz.
        uint16_t* input = (uint16_t*)buffer;
        bufferSize /= sizeof(uint16_t);
        auto outputBuffer = std::make_unique<uint16_t[]>(6 * bufferSize);
        for (size_t i = 0, j = 0; i < bufferSize; i++)
        {
            for (int k = 0; k < 6; k++)
            {
                outputBuffer[j++] = input[i];
            }
        }
        bufferSize *= 6 * sizeof(uint16_t);

        return Play((uint8_t*)outputBuffer.get(), bufferSize);
    }
    else
    {
        return -1;
    }
}

int AudioPlayer2::Close()
{
    int result = 0;

    if (_pcmHandle != NULL)
    {
        std::cout << "AudioPlayer2::Close() calling pcm_close" << std::endl;
        result = pcm_close(_pcmHandle);
        std::cout << "AudioPlayer2::Close() pcm_close COMPLETE" << std::endl;
        _pcmHandle = NULL;
    }

    return result;
}