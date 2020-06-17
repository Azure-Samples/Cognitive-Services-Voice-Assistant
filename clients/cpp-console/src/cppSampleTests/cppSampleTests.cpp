// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "pch.h"
#include "CppUnitTest.h"
#include "WindowsAudioPlayer.h"
#include "AudioPlayerStreamImpl.h"
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
            std::shared_ptr<fstream> fs = std::make_shared<fstream>();

            const string& wavFile = "..\\..\\..\\cppSampleTests\\CognitiveServicesVoiceAssistantIntro.wav";

            fs->open(wavFile, ios_base::binary | ios_base::in);

            if ((fs->rdstate() & fs->failbit) != 0)
            {
                Assert::Fail();
            }

            fs->seekg(44);

            int rc = 0;
            int result = 1;
            unsigned int bytesRead = 0;

            AudioPlayer::WindowsAudioPlayer player;
            player.Initialize();

            std::shared_ptr<IAudioPlayerStream> playerStream = std::make_shared<AudioPlayer::AudioPlayerStreamImpl>(fs);
            result = player.Play(playerStream);

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

    private:
        fstream fs;
    };
}
