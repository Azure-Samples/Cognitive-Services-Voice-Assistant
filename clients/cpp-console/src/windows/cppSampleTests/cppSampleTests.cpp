// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "CppUnitTest.h"
#include "WindowsAudioPlayer.h"
#include "AudioPlayerStreamImpl.h"
#include <fstream>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std;

namespace cppSampleTests
{
    const string testWavFilePath = "..\\..\\cppSampleTests\\CognitiveServicesVoiceAssistantIntro.wav";
    TEST_CLASS(cppSampleTests)
    {
    public:

        TEST_METHOD(TestWindowsAudioPlayerInitializeWithDefaultAudioFormat)
        {
            AudioPlayer::WindowsAudioPlayer player;
            HRESULT hr = player.Initialize();
            Assert::AreEqual(S_OK, hr);
        }

        TEST_METHOD(TestWindowsAudioPlayerPlay) 
        {
            int rc = 0;
            AudioPlayer::WindowsAudioPlayer player;
            player.Initialize();

            int result = ReadWavFileAndInitializeWindowsAudioPlayer(player);

            Assert::AreEqual(rc, result);
        }

        TEST_METHOD(TestWindowsAudioPlayerPullAudioOutputStream) 
        {
            int rc = 0;
            int result = 1;
            unsigned int bytesRead = 0;

            std::shared_ptr<fstream> fs = std::make_shared<fstream>();

            fs->open(testWavFilePath, ios_base::binary | ios_base::in);

            if ((fs->rdstate() & fs->failbit) != 0)
            {
                Assert::Fail();
            }

            fs->seekg(44);

            AudioPlayer::WindowsAudioPlayer player;
            player.Initialize();

            std::shared_ptr<IAudioPlayerStream> playerStream = std::make_shared<AudioPlayer::AudioPlayerStreamImpl>(fs);
            result = player.Play(playerStream);

            Assert::AreEqual(rc, result);
        }

        TEST_METHOD(TestWindowsAudioPlayerStop) 
        {
            int rc = 0;
            AudioPlayer::WindowsAudioPlayer player;
            player.Initialize();

            int result = ReadWavFileAndInitializeWindowsAudioPlayer(player);

            result = player.Stop();
            Assert::AreEqual(rc, result);
        }

        void SleepDuration(int numBytes) 
        {
            Sleep((numBytes / 32000) * 1000);
        }

        int ReadWavFileAndInitializeWindowsAudioPlayer(AudioPlayer::WindowsAudioPlayer& player)
        {
            int bytesRead = 0;
            int result = 1;
            fstream fs;

            fs.open(testWavFilePath, ios_base::binary | ios_base::in);

            if ((fs.rdstate() & fs.failbit) != 0)
            {
                Assert::Fail();
            }

            fs.seekg(44);

            std::array<uint8_t, 1000> buffer;

            while (!fs.eof())
            {
                fs.read((char*)buffer.data(), (uint32_t)buffer.size());
                result = player.Play(buffer.data(), buffer.size());
                bytesRead += 1000;
            }

            fs.close();

            SleepDuration(bytesRead);

            return result;
        }
    };
}
