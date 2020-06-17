// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AudioPlayerStream.h"
#include "AudioPlayer.h"
#include <fstream>
#include <iostream>
#include <string>

using namespace std;
using namespace Microsoft::CognitiveServices::Speech;

namespace AudioPlayer
{
	class WindowsAudioPlayerStream : public IWindowsAudioPlayerStream
	{
	public:

		//WindowsAudioPlayerStream();
		WindowsAudioPlayerStream(std::shared_ptr<Audio::PullAudioOutputStream> pStream);

		WindowsAudioPlayerStream(fstream fStream);

		virtual int Read(unsigned char* buffer, size_t bufferSize) final;

	private:
		std::shared_ptr<Audio::PullAudioOutputStream> m_pullStream;
		std::fstream m_fStream;

		AudioPlayerStreamType m_streamType;
	};
}

