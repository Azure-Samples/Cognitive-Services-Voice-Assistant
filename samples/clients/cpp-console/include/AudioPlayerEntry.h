// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AudioPlayer{
    class AudioPlayerEntry{
        public:
            AudioPlayerEntry(unsigned char* pData, size_t pSize);
            size_t m_size;
            unsigned char *m_data;
    };
}