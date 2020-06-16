#include "pch.h"
#include "CppUnitTest.h"
#include "WindowsAudioPlayer.h"
#include <fstream>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std;

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

		//TEST_METHOD(TestWindowsAudioPlayerInitializeWithUnsupportedAudioFormat)
		//{
		//	AudioPlayer::WindowsAudioPlayer player;
		//	HRESULT hr = player.Initialize("stereo", IAudioPlayer::AudioPlayerFormat::Stereo48khz16bit);
		//	Assert::AreEqual(E_FAIL, hr);
		//}

		TEST_METHOD(TestWindowsAudioPlayerPlay) 
		{
			int rc = 0;
			int bytesRead = 0;
			int result = 1;
			fstream fs;

			const string& wavFile = "..\\..\\..\\cppSampleTests\\CognitiveServicesVoiceAssistantIntro.wav";

			fs.open(wavFile, ios_base::binary | ios_base::in);

			if ((fs.rdstate() & fs.failbit) != 0)
			{
				Assert::Fail();
			}

			fs.seekg(44);

			std::array<uint8_t, 1000> buffer;

			AudioPlayer::WindowsAudioPlayer player;
			player.Initialize();

			while (!fs.eof())
			{
				fs.read((char*)buffer.data(), (uint32_t)buffer.size());
				result = player.Play(buffer.data(), buffer.size());
				bytesRead += 1000;
			}

			fs.close();

			SleepDuration(bytesRead);

			Assert::AreEqual(rc, result);
		}

		TEST_METHOD(TestWindowsAudioPlayerStop) 
		{
			int rc = 0;
			int bytesRead = 0;
			int result = 1;
			fstream fs;

			const string& wavFile = "..\\..\\..\\cppSampleTests\\CognitiveServicesVoiceAssistantIntro.wav";

			fs.open(wavFile, ios_base::binary | ios_base::in);

			if ((fs.rdstate() & fs.failbit) != 0)
			{
				Assert::Fail();
			}

			fs.seekg(44);

			std::array<uint8_t, 1000> buffer;

			AudioPlayer::WindowsAudioPlayer player;
			player.Initialize();

			while (!fs.eof())
			{
				fs.read((char*)buffer.data(), (uint32_t)buffer.size());
				result = player.Play(buffer.data(), buffer.size());
				bytesRead += 1000;
			}

			fs.close();

			SleepDuration(bytesRead);

			result = player.Stop();
			Assert::AreEqual(rc, result);
		}

		void SleepDuration(int numBytes) 
		{
			Sleep((numBytes / 32000) * 1000);
		}
	};
}
