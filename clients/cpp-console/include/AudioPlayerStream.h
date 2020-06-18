// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <cstddef>

#pragma once

class IAudioPlayerStream
{
public:
    enum class AudioPlayerStreamType
    {
        PULL_AUDIO_OUTPUT_STREAM,
        FSTREAM
    };

    virtual unsigned int Read(unsigned char* buffer, size_t bufferSize) = 0;
};
