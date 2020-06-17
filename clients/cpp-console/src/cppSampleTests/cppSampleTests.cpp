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
		//	Sleep(3000);
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

		TEST_METHOD(TestWindowsAudioPlayerPullAudioOutputStream) 
		{
			std::shared_ptr<Microsoft::CognitiveServices::Speech::Audio::PullAudioOutputStream> pStream;

			auto audio = pStream->CreatePullStream();

			int rc = 0;
			//int bytesRead = 0;
			int result = 1;
			//fstream fs;

			//const string& wavFile = "..\\..\\..\\cppSampleTests\\CognitiveServicesVoiceAssistantIntro.wav";

			//fs.open(wavFile, ios_base::binary | ios_base::in);

			//if ((fs.rdstate() & fs.failbit) != 0)
			//{
			//	Assert::Fail();
			//}

			//fs.seekg(44);

			//std::array<uint8_t, 1000> buffer;

			////uint32_t playBufferSize = 1024;
			unsigned int bytesRead = 0;
			////std::unique_ptr<unsigned char[]> playBuffer = std::make_unique<unsigned char[]>(playBufferSize);

			AudioPlayer::WindowsAudioPlayer player;
			player.Initialize();

			//while (!fs.eof())
			//{
			//	fs.read((char*)buffer.data(), buffer.size());
				
				//audio->Read(buffer.data(), bytesRead);
			result = player.Play(audio);
				//bytesRead += 1000;
				//playBufferSize += 1000;
			//}

			//audio = buffer;

			//audio->Read(buffer.data(), buffer.size());
			//fs.close();

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
