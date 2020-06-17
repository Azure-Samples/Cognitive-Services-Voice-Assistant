// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#pragma once

class IAudioPlayerStream
{
public:
    enum class AudioPlayerStreamType
    {
        PULLAUDIOOUTPUTSTREAM,
        FSTREAM
    };

    virtual int Read(unsigned char* buffer, size_t bufferSize) = 0;
};
