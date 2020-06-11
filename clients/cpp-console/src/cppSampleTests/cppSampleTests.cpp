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
		
		TEST_METHOD(TestMethod1)
		{
			AudioPlayer::WindowsAudioPlayer player;
			player.Initialize();
		}
	};
}
