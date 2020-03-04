// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <cstring>
#include <memory>
#include "AudioPlayerEntry.h"

using namespace AudioPlayer;

AudioPlayerEntry::AudioPlayerEntry(unsigned char* pData, size_t pSize){
    m_size = pSize;
    m_data = (unsigned char *)malloc(pSize);
    if (m_data) {
        memcpy(m_data, pData, pSize);
    }
};