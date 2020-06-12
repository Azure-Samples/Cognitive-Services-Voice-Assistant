#include "pch.h"
#include "CppUnitTest.h"
#include "WindowsAudioPlayer.h"
//#include "AudioPlayer.h"
//#include "AudioPlayerEntry.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace cppSampleTests
{
	TEST_CLASS(cppSampleTests)
	{
	public:
		
		TEST_METHOD(TestWindowsAudioPlayerInitializeWithDefaultAudioFormat)
		{		
			AudioPlayer::WindowsAudioPlayer player;
			HRESULT hr = player.Initialize();
			Assert::AreEqual(S_OK, hr);
		}

		TEST_METHOD(TestWindowsAudioPlayerInitializeWithUnsupportedAudioFormat)
		{
			AudioPlayer::WindowsAudioPlayer player;
			HRESULT hr = player.Initialize("default", IAudioPlayer::AudioPlayerFormat::Stereo48khz16bit);
			Assert::AreNotEqual(S_OK, hr);
		}

		
	};
}
