#include "WindowsAudioPlayerStream.h"

using namespace AudioPlayer;

WindowsAudioPlayerStream::WindowsAudioPlayerStream(std::shared_ptr<Audio::PullAudioOutputStream> pStream)
{
	m_pullStream = pStream;
	m_streamType = AudioPlayerStreamType::PULLAUDIOOUTPUTSTREAM;
}

WindowsAudioPlayerStream::WindowsAudioPlayerStream(fstream fStream)
{
	fstream& m_fStream = fStream;
	m_streamType = AudioPlayerStreamType::FSTREAM;
}

int Read(unsigned char* buffer, size_t bufferSize)
{
	switch (m_streamType)
	{
	case AudioPlayerStreamType::PULLAUDIOOUTPUTSTREAM:
		return m_pullStream->Read(buffer, bufferSize);
	case AudioPlayerStreamType::FSTREAM:
		if (m_fStream.eof())
		{
			return 0;
		}
		m_fStream.read((char*)buffer, (uint32_t)bufferSize);
		int numberOfBytes = m_fStream.gcount();
		return numberOfBytes;
		//break;
	}
}