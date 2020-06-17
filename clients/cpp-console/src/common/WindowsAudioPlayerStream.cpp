// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AudioPlayerStreamImpl.h"

using namespace AudioPlayer;

AudioPlayerStreamImpl::AudioPlayerStreamImpl(std::shared_ptr<Audio::PullAudioOutputStream> pStream)
{
    m_pullStream = pStream;
    m_streamType = AudioPlayerStreamType::PULLAUDIOOUTPUTSTREAM;
}

AudioPlayerStreamImpl::AudioPlayerStreamImpl(std::shared_ptr<fstream> fStream)
{
    m_fStream = fStream;
    m_streamType = AudioPlayerStreamType::FSTREAM;
}

int AudioPlayerStreamImpl::Read(unsigned char* buffer, size_t bufferSize)
{
    switch (m_streamType)
    {
    case AudioPlayerStreamType::PULLAUDIOOUTPUTSTREAM:
       return m_pullStream->Read(buffer, bufferSize);
    case AudioPlayerStreamType::FSTREAM:
        if (m_fStream->eof())
        {
            return 0;
        }
        m_fStream->read((char*)buffer, (uint32_t)bufferSize);
        int numberOfBytes = m_fStream->gcount();
        return numberOfBytes;
    }
}