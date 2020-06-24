// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AudioPlayerStream.h"

namespace AudioPlayer
{
    enum class PlayerEntryType
    {
        BYTE_ARRAY,
        PULL_AUDIO_OUTPUT_STREAM
    };

    class AudioPlayerEntry
    {
    public:
        AudioPlayerEntry(unsigned char* pData, size_t pSize);
        AudioPlayerEntry(std::shared_ptr<IAudioPlayerStream> pStream);

        PlayerEntryType m_entryType;
        std::shared_ptr<IAudioPlayerStream> m_audioPlayerStream;
        size_t m_size;
        unsigned char* m_data;
    };
}