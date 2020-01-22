//
// Copyright (c) Microsoft Corporation. All rights reserved.
//

#pragma once

#include <cstdio>
#include <cstdint>


#include <tinyalsa/asoundlib.h>

enum class AudioPlayer2Format
{
    Mono16khz16bit,
    Stereo48khz16bit
};

class AudioPlayer2
{
public:
    AudioPlayer2() {}
    int Open();
    int Open(const std::string& device, AudioPlayer2Format format);
    int Play(uint8_t* buffer, size_t bufferSize);
    int Play(uint8_t* buffer, size_t bufferSize, AudioPlayer2Format format);
    int Close();

private:
    pcm* _pcmHandle = NULL;
    AudioPlayer2Format _format;
    pcm_config _config;
};