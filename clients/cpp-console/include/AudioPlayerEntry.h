// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//#include "speechapi_cxx.h"
#include "AudioPlayerStream.h"

//using namespace Microsoft::CognitiveServices::Speech;

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
        AudioPlayerEntry(std::shared_ptr<IWindowsAudioPlayerStream> pStream);

        PlayerEntryType m_entryType;
        std::shared_ptr<IWindowsAudioPlayerStream> m_AudioPlayerStream;
        size_t m_size;
        unsigned char* m_data;
    };
}