// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AudioPlayerStream.h"
#include "speechapi_cxx.h"
#include <fstream>

using namespace std;
using namespace Microsoft::CognitiveServices::Speech;

namespace AudioPlayer
{
	class WindowsAudioPlayerStream : public IAudioPlayerStream
	{
	public:

		//WindowsAudioPlayerStream();
		WindowsAudioPlayerStream(std::shared_ptr<Audio::PullAudioOutputStream> pStream);

		WindowsAudioPlayerStream(std::shared_ptr<fstream> fStream);

		virtual int Read(unsigned char* buffer, size_t bufferSize) final;

	private:
		std::shared_ptr<Audio::PullAudioOutputStream> m_pullStream;
		std::shared_ptr<fstream> m_fStream;

		AudioPlayerStreamType m_streamType;
	};
}

