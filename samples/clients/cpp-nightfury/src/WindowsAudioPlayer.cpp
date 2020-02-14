// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <cstring>
#include <chrono>
#include <condition_variable>
#include <string>
#include <thread>
#include <mutex>
#include "WindowsAudioPlayer.h"

using namespace AudioPlayer;

WindowsAudioPlayer::WindowsAudioPlayer() {
    Open();
    m_playerThread = std::thread(&WindowsAudioPlayer::PlayerThreadMain, this);

}

WindowsAudioPlayer::~WindowsAudioPlayer() {
    Close();
    m_canceled = true;
    m_threadMutex.unlock();
    m_conditionVariable.notify_one();
    m_playerThread.join();
}

int WindowsAudioPlayer::Open() {
    int rc;
    rc = Open("default", AudioPlayerFormat::Mono16khz16bit);
    return rc;
}

int WindowsAudioPlayer::Open(const std::string& device, AudioPlayerFormat format) {
    
    return 0;
}

void WindowsAudioPlayer::PlayerThreadMain() {
    m_canceled = false;
    while (m_canceled == false) {
        // here we will wait to be woken up since there is no audio left to play
        std::unique_lock<std::mutex> lk{ m_threadMutex };
        m_conditionVariable.wait(lk);
        lk.unlock();

        size_t playBufferSize = 1024;
        while (m_audioQueue.size() > 0) {
            m_isPlaying = true;
            AudioPlayerEntry entry = m_audioQueue.front();
            size_t bufferLeft = entry.m_size;
            std::unique_ptr<unsigned char[]> playBuffer = std::make_unique<unsigned char[]>(playBufferSize);
            while (bufferLeft > 0) {
                if (bufferLeft >= playBufferSize) {
                    memcpy(playBuffer.get(), &entry.m_data[entry.m_size - bufferLeft], playBufferSize);
                    bufferLeft -= playBufferSize;
                }
                else { //there is a smaller amount to play so we will pad with silence
                    memcpy(playBuffer.get(), &entry.m_data[entry.m_size - bufferLeft], bufferLeft);
                    memset(playBuffer.get() + bufferLeft, 0, playBufferSize - bufferLeft);
                    bufferLeft = 0;
                }
                WriteToDriver(playBuffer.get());
            }
            m_queueMutex.lock();
            m_audioQueue.pop_front();
            m_queueMutex.unlock();
        }
        m_isPlaying = false;
    }

}

int WindowsAudioPlayer::Play(uint8_t* buffer, size_t bufferSize) {
    int rc = 0;
    AudioPlayerEntry entry(buffer, bufferSize);
    m_queueMutex.lock();
    m_audioQueue.push_back(entry);
    m_queueMutex.unlock();

    if (!m_isPlaying) {
        //wake up the audio thread
        m_conditionVariable.notify_one();
    }

    return rc;
}

int WindowsAudioPlayer::WriteToDriver(uint8_t* buffer) {
    int rc = 0;

    return rc;
}

int WindowsAudioPlayer::Close() {

    return 0;
}