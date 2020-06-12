// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "speechapi_cxx.h"
using namespace Microsoft::CognitiveServices::Speech;

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
        AudioPlayerEntry(std::shared_ptr<Audio::PullAudioOutputStream> pStream);

        PlayerEntryType m_entryType;
        std::shared_ptr<Audio::PullAudioOutputStream> m_pullStream;
        size_t m_size;
        unsigned char* m_data;
    };
}