#include "pch.h"
#include "CppUnitTest.h"
#include "WindowsAudioPlayer.h"
#include "DialogManager.h"
//#include "AudioPlayer.h"
//#include "AudioPlayerEntry.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace Microsoft::CognitiveServices::Speech;

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

			const string& wavFile = "C:\\Users\\v-saplum\\Downloads\\spx-zips\\spx-netcore30-win-x64\\CognitiveServicesVoiceAssistantIntro.wav";
			//const string& wavFile = "this";
			fstream fs;
			fs.open(wavFile, ios_base::binary | ios_base::in);
			fs.seekg(44);

			std::array<uint8_t, 1000> buffer;
			fs.read((char*)wavFile.data(), (uint32_t)wavFile.size());
			
			fs.close();

			AudioPlayer::WindowsAudioPlayer player;
			player.Initialize();
			int result = player.Play(buffer.data(), buffer.size());
			Assert::AreEqual(rc, result);
		}

		TEST_METHOD(TestWindowsAudioPlayerStop) 
		{
			int rc = 0;
			AudioPlayer::WindowsAudioPlayer player;
			player.Initialize();
			TestWindowsAudioPlayerPlay();

			int result = player.Stop();
			Assert::AreEqual(rc, result);
		}
	};
}
