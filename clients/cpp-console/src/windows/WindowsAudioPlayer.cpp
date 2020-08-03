// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <cstring>
#include <chrono>
#include <condition_variable>
#include <string>
#include <thread>
#include <mutex>
#include <atlbase.h>
#include <avrt.h>
#include "WindowsAudioPlayer.h"

using namespace AudioPlayer;

#define ENGINE_LATENCY_IN_MSEC 1000

void SAFE_CLOSEHANDLE(HANDLE _handle_)
{
    if (NULL != _handle_)
    {
        CloseHandle(_handle_);
        _handle_ = NULL;
    }
}

WindowsAudioPlayer::WindowsAudioPlayer()
{
    m_state = AudioPlayerState::UNINITIALIZED;
    m_hAudioClientEvent = NULL;
    m_hRenderThread = NULL;
    m_hStartEvent = NULL;
    m_hStopEvent = NULL;
    m_hRenderingDoneEvent = NULL;
    m_pAudioClient = NULL;
    m_pRenderClient = NULL;
    m_renderBuffer = NULL;

    m_renderBufferOffsetInFrames = 0;
    m_renderBufferSizeInFrames = 0;
    m_loopRenderBufferFlag = false; //by default looping render buffer is off
    m_muteChannelMask = 0;
    if (CoInitialize(nullptr) != S_OK)
    {
        fprintf(stdout, "CoInitialize failed\n");
    }
    m_playerThread = std::thread(&WindowsAudioPlayer::PlayerThreadMain, this);
}

WindowsAudioPlayer::~WindowsAudioPlayer()
{
    Close();
}

int WindowsAudioPlayer::Initialize()
{
    int rc;
    rc = Initialize("default", AudioPlayerFormat::Mono16khz16bit);
    return rc;
}

int WindowsAudioPlayer::Initialize(const std::string& device, AudioPlayerFormat format)
{
    m_state = AudioPlayerState::INITIALIZING;
    HRESULT hr = S_OK;
    CComPtr<IMMDeviceEnumerator> pEnumerator;
    CComPtr<IMMDevice> pDevice;
    REFERENCE_TIME hnsRequestedDuration = REFTIMES_PER_MILLISEC * ENGINE_LATENCY_IN_MSEC;

    switch (format)
    {
    case AudioPlayerFormat::Mono16khz16bit:
        /* Signed 16-bit little-endian format */
        fprintf(stdout, "Format = Mono16khz16bit\n");
        m_pwf.nChannels = 1;
        m_pwf.nBlockAlign = 2;
        m_pwf.wBitsPerSample = 16;
        m_pwf.nSamplesPerSec = 16000;
        m_pwf.nAvgBytesPerSec = m_pwf.nSamplesPerSec * m_pwf.nBlockAlign;
        m_pwf.cbSize = 0;
        m_pwf.wFormatTag = WAVE_FORMAT_PCM;
        break;
    default:
        hr = E_FAIL;
    }

    //Begin Audio Device Setup
    CComCritSecLock<CComAutoCriticalSection> lock(m_cs);
    
    if (hr != S_OK) 
    {
        goto exit;
    }

    // get a device enumator from the OS
    hr = CoCreateInstance(
        __uuidof(MMDeviceEnumerator), NULL,
        CLSCTX_ALL, __uuidof(IMMDeviceEnumerator),
        (void**)&pEnumerator);
    if (hr != S_OK)
    {
        goto exit;
    }

    // use the enumerator to get the default device
    hr = pEnumerator->GetDefaultAudioEndpoint(
        eRender, eConsole, &pDevice);
    if (hr != S_OK)
    {
        goto exit;
    }

    // use the device to activate an AudioClient
    hr = pDevice->Activate(
        __uuidof(IAudioClient), CLSCTX_ALL,
        NULL, (void**)&m_pAudioClient);
    if (hr != S_OK)
    {
        goto exit;
    }

    hr = m_pAudioClient->Initialize(
        AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM,
        hnsRequestedDuration,
        0,
        &m_pwf,
        NULL);
    // Note: AUDCLNT_SHAREMODE_EXCLUSIVE is not supported in Durango
    if (hr != S_OK)
    {
        goto exit;
    }

    hr = m_pAudioClient->GetService(
        _uuidof(IAudioRenderClient),
        (void**)&m_pRenderClient);
    if (hr != S_OK)
    {
        goto exit;
    }
    hr = m_pAudioClient->Start();
    m_state = AudioPlayerState::PAUSED;
exit:

    return hr;
}

void WindowsAudioPlayer::PlayerThreadMain()
{
    HRESULT hr = S_OK;
    m_shuttingDown = false;

    while (m_shuttingDown == false)
    {
        // here we will wait to be woken up since there is no audio left to play
        std::unique_lock<std::mutex> lk{ m_threadMutex };
        m_conditionVariable.wait(lk);
        lk.unlock();

        while (m_audioQueue.size() > 0)
        {
            m_state = AudioPlayerState::PLAYING;

            std::shared_ptr<AudioPlayerEntry> entry = std::make_shared<AudioPlayerEntry>(m_audioQueue.front());
            m_queueMutex.lock();
            if (!m_audioQueue.empty())
            {
                //remove the item we just used.
                m_audioQueue.pop_front();
            }
            m_queueMutex.unlock();
            if (entry == nullptr)
            {
                continue;
            }
            switch (entry->m_entryType)
            {
            case PlayerEntryType::BYTE_ARRAY:
                PlayByteBuffer(entry);
                break;
            case PlayerEntryType::PULL_AUDIO_OUTPUT_STREAM:
                PlayAudioPlayerStream(entry);
                break;
            default:
                fprintf(stderr, "Unknown Audio Player Entry type\n");
            }

        }
        m_state = AudioPlayerState::PAUSED;
    }
}

void WindowsAudioPlayer::PlayAudioPlayerStream(std::shared_ptr<AudioPlayerEntry> pEntry)
{
    HRESULT hr = S_OK;
    UINT32 maxBufferSizeInFrames = 0;
    UINT32 paddingFrames;
    UINT32 framesAvailable;
    UINT32 framesToWrite;
    BYTE* pData, * pStreamData;
    unsigned int bytesRead = 0;
    std::shared_ptr<IAudioPlayerStream> stream = pEntry->m_audioPlayerStream;

    hr = m_pAudioClient->GetBufferSize(&maxBufferSizeInFrames);
    if (FAILED(hr))
    {
        fprintf(stderr, "Error. Failed to GetBufferSize. Error: 0x%08x\n", hr);
        return;
    }

    do
    {
        hr = m_pAudioClient->GetCurrentPadding(&paddingFrames);
        if (FAILED(hr))
        {
            fprintf(stderr, "Error. Failed to GetCurrentPadding. Error: 0x%08x\n", hr);
            continue;
        }

        framesAvailable = maxBufferSizeInFrames - paddingFrames;
        UINT32 sizeToWrite = framesAvailable * m_pwf.nBlockAlign;

        if (sizeToWrite == 0)
        {
            //this means the buffer is full so we will wait till some room opens up
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
            continue;
        }

        pStreamData = (BYTE*)malloc(sizeToWrite);
        bytesRead = stream->Read((unsigned char*)pStreamData, sizeToWrite);

        if (sizeToWrite > bytesRead)
        {
            sizeToWrite = (UINT32)bytesRead;
        }
        framesToWrite = sizeToWrite / m_pwf.nBlockAlign;

        hr = m_pRenderClient->GetBuffer(framesToWrite, &pData);
        if (FAILED(hr))
        {
            fprintf(stderr, "Error. Failed to GetBuffer. Error: 0x%08x\n", hr);
            goto freeBeforeContinue;
        }

        memcpy_s(pData, sizeToWrite, pStreamData, sizeToWrite);

        hr = m_pRenderClient->ReleaseBuffer(framesToWrite, 0);
        if (FAILED(hr))
        {
            printf("Error. Failed to ReleaseBuffer. Error: 0x%08x\n", hr);
            goto freeBeforeContinue;
        }

    freeBeforeContinue:
        free(pStreamData);
    } while (bytesRead > 0 && m_canceled == false);

}

void WindowsAudioPlayer::PlayByteBuffer(std::shared_ptr<AudioPlayerEntry> pEntry)
{
    HRESULT hr = S_OK;
    UINT32 maxBufferSizeInFrames = 0;
    UINT32 paddingFrames;
    UINT32 framesAvailable;
    UINT32 framesToWrite;
    BYTE* pData;
    size_t bufferLeft = pEntry->m_size;

    hr = m_pAudioClient->GetBufferSize(&maxBufferSizeInFrames);
    if (FAILED(hr))
    {
        fprintf(stderr, "Error. Failed to GetBufferSize. Error: 0x%08x\n", hr);
        return;
    }

    while (bufferLeft > 0 && m_canceled == false)
    {
        //  We want to find out how much of the buffer *isn't* available (is padding).

        hr = m_pAudioClient->GetCurrentPadding(&paddingFrames);
        if (FAILED(hr))
        {
            fprintf(stderr, "Error. Failed to GetCurrentPadding. Error: 0x%08x\n", hr);
            continue;
        }

        framesAvailable = maxBufferSizeInFrames - paddingFrames;
        UINT32 sizeToWrite = framesAvailable * m_pwf.nBlockAlign;

        if (sizeToWrite > bufferLeft)
        {
            sizeToWrite = (UINT32)bufferLeft;
        }

        framesToWrite = sizeToWrite / m_pwf.nBlockAlign;
        hr = m_pRenderClient->GetBuffer(framesToWrite, &pData);
        if (FAILED(hr))
        {
            fprintf(stderr, "Error. Failed to GetBuffer. Error: 0x%08x\n", hr);
            continue;
        }

        memcpy_s(pData, sizeToWrite, &pEntry->m_data[pEntry->m_size - bufferLeft], sizeToWrite);

        bufferLeft -= sizeToWrite;

        hr = m_pRenderClient->ReleaseBuffer(framesToWrite, 0);
        if (FAILED(hr))
        {
            printf("Error. Failed to ReleaseBuffer. Error: 0x%08x\n", hr);
            continue;
        }
    }
}

int WindowsAudioPlayer::Play(uint8_t* buffer, size_t bufferSize)
{
    int rc = 0;
    if (m_state == AudioPlayerState::UNINITIALIZED)
    {
        rc = -1;
    }
    else
    {
        AudioPlayerEntry entry(buffer, bufferSize);
        m_queueMutex.lock();
        m_audioQueue.push_back(entry);
        m_queueMutex.unlock();

        //make sure the canceled variable is not set
        m_canceled = false;

        if (m_state != AudioPlayerState::PLAYING)
        {
            //wake up the audio thread
            m_conditionVariable.notify_one();
        }
    }

    return rc;
}

int WindowsAudioPlayer::Play(std::shared_ptr<IAudioPlayerStream> pStream)
{
    int rc = 0;

    if (m_state == AudioPlayerState::UNINITIALIZED)
    {
        rc = -1;
    }
    else
    {
        AudioPlayerEntry entry(pStream);
        m_queueMutex.lock();
        m_audioQueue.push_back(entry);
        m_queueMutex.unlock();

        //make sure the canceled variable is not set
        m_canceled = false;

        if (m_state != AudioPlayerState::PLAYING)
        {
            //wake up the audio thread
            m_conditionVariable.notify_one();
        }
    }

    return rc;
}

int WindowsAudioPlayer::Stop()
{
    //set the canceled flag to stop playback
    m_canceled = true;

    //clear the audio queue safely
    m_queueMutex.lock();
    m_audioQueue.clear();
    m_queueMutex.unlock();

    return 0;
}

int WindowsAudioPlayer::Pause()
{
    //TODO implement
    return -1;
}

int WindowsAudioPlayer::Resume()
{
    //TODO implement
    return -1;
}

int WindowsAudioPlayer::SetVolume(unsigned int percent)
{
    return 0;
}

AudioPlayerState WindowsAudioPlayer::GetState()
{
    return m_state;
}

int WindowsAudioPlayer::Close()
{
    m_shuttingDown = true;
    m_canceled = true;
    m_conditionVariable.notify_one();
    m_playerThread.join();

    SAFE_CLOSEHANDLE(m_hAudioClientEvent);
    SAFE_RELEASE(m_pRenderClient);
    SAFE_RELEASE(m_pAudioClient);
    CoUninitialize();
    return 0;
}