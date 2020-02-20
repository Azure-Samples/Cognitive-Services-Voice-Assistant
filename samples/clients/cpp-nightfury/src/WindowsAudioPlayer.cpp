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

void SAFE_CLOSEHANDLE(HANDLE _handle_) {
    if (NULL != _handle_)
    {
        CloseHandle(_handle_);
        _handle_ = NULL;
    }
}

WindowsAudioPlayer::WindowsAudioPlayer() {
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
    if (CoInitialize(nullptr) != S_OK) {
        fprintf(stdout, "CoInitialize failed\n");
    }
    Open();
    m_playerThread = std::thread(&WindowsAudioPlayer::PlayerThreadMain, this);
}

WindowsAudioPlayer::~WindowsAudioPlayer() {
    Close();
    m_canceled = true;
    m_threadMutex.unlock();
    m_conditionVariable.notify_one();
    m_playerThread.join();

    CoUninitialize();
    SAFE_CLOSEHANDLE(m_hAudioClientEvent);
    //SAFE_CLOSEHANDLE(m_hRenderThread);
    //SAFE_CLOSEHANDLE(m_hStartEvent);
    //SAFE_CLOSEHANDLE(m_hStopEvent);
    // Do not SAFE_CLOSEHANDLE(m_hRenderingDoneEvent) - This handle is managed by the caller
    SAFE_RELEASE(m_pRenderClient);
    SAFE_RELEASE(m_pAudioClient);
}

int WindowsAudioPlayer::Open() {
    int rc;
    rc = Open("default", AudioPlayerFormat::Mono16khz16bit);
    return rc;
}

int WindowsAudioPlayer::Open(const std::string& device, AudioPlayerFormat format) {
    HRESULT hr = S_OK;
    CComPtr<IMMDeviceEnumerator> pEnumerator;
    CComPtr<IMMDevice> pDevice;
    REFERENCE_TIME hnsRequestedDuration = REFTIMES_PER_MILLISEC * ENGINE_LATENCY_IN_MSEC;

    switch (format) {
    case AudioPlayerFormat::Mono16khz16bit:
    default:
        /* Signed 16-bit little-endian format */
        fprintf(stdout, "Format = Mono16khz16bit\n");
        m_pwf.nChannels = 1;
        m_pwf.nBlockAlign = 2;
        m_pwf.wBitsPerSample = 16;
        m_pwf.nSamplesPerSec =16000;
        m_pwf.nAvgBytesPerSec = m_pwf.nSamplesPerSec * m_pwf.nBlockAlign;
        m_pwf.cbSize = 0;
        m_pwf.wFormatTag = WAVE_FORMAT_PCM;
    }

    //Begin Audio Device Setup
    CComCritSecLock<CComAutoCriticalSection> lock(m_cs);

    // get a device enumator from the OS
    hr = CoCreateInstance(
        __uuidof(MMDeviceEnumerator), NULL,
        CLSCTX_ALL, __uuidof(IMMDeviceEnumerator),
        (void**)&pEnumerator);
    if (hr != S_OK) {
        goto exit;
    }

    // use the enumerator to get the default device
    hr = pEnumerator->GetDefaultAudioEndpoint(
        eRender, eConsole, &pDevice);
    if (hr != S_OK) {
        goto exit;
    }

    // use the device to activate an AudioClient
    hr = pDevice->Activate(
        __uuidof(IAudioClient), CLSCTX_ALL,
        NULL, (void**)&m_pAudioClient);
    if (hr != S_OK) {
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
    if (hr != S_OK) {
        goto exit;
    }

    hr = m_pAudioClient->GetService(
    _uuidof(IAudioRenderClient),
    (void**)&m_pRenderClient);
    if (hr != S_OK) {
        goto exit;
    }

exit:

    return hr;
}

void WindowsAudioPlayer::PlayerThreadMain() {
    m_canceled = false;
    while (m_canceled == false) {
        // here we will wait to be woken up since there is no audio left to play
        std::unique_lock<std::mutex> lk{ m_threadMutex };
        m_conditionVariable.wait(lk);
        lk.unlock();
        UINT32 maxBufferSizeInFrames = 0;
        m_pAudioClient->GetBufferSize(&maxBufferSizeInFrames);
        size_t playBufferSize = m_pwf.nChannels * m_pwf.nBlockAlign * maxBufferSizeInFrames / 2;
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
                WriteToDriver(playBuffer.get(), playBufferSize);
                std::this_thread::sleep_for(std::chrono::seconds(1));
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

int WindowsAudioPlayer::WriteToDriver(uint8_t* buffer, size_t bufferSize) {
    int hr = S_OK;
    BYTE* pData = NULL;
    size_t bufferSizeInFrames = bufferSize / m_pwf.nBlockAlign;
    //size_t bufferSizeInFrames = 32;
    UINT32 maxBufferSize = 0;
    if (bufferSize == 0) {
        goto exit;
    }
    
    m_pAudioClient->GetBufferSize(&maxBufferSize);

    // Grab all the available space in the shared buffer.
    hr = m_pRenderClient->GetBuffer(bufferSizeInFrames, &pData);
    if (hr != S_OK) {
        fprintf(stderr, "GetBuffer failed with HR = %d\n", hr);
        goto exit;
    }

    // Copy next chunk of audio data
    if (0 != memcpy_s(pData,
        bufferSize,
        (BYTE*)buffer,
        bufferSize)) {
        hr = E_FAIL;
        fprintf(stderr, "memcpy failed\n");
        goto exit;
    }

    hr = m_pRenderClient->ReleaseBuffer(bufferSizeInFrames, 0);
    if (hr != S_OK) {
        fprintf(stderr, "ReleaseBuffer failed\n");
        goto exit;
    }

exit:

    return hr;
}

int WindowsAudioPlayer::Close() {

    return 0;
}

//HRESULT WindowsAudioPlayer::Initialize(
//    _Inout_opt_                           HANDLE hRenderingDoneEvent,
//    _In_                                  WAVEFORMATEX* pwf,
//    _In_count_(renderBufferSizeInSamples) INT16* pRenderBuffer,
//    _In_                                  DWORD         renderBufferSizeInSamples,
//    _In_                                  bool          loopOnFlag)
//{
//    HRESULT hr = S_OK;
//    REFERENCE_TIME hnsRequestedDuration = REFTIMES_PER_MILLISEC * ENGINE_LATENCY_IN_MSEC;
//    UINT32 bufferFrameCount = 0;
//    BYTE* pData = NULL;
//    CComPtr<IMMDeviceEnumerator> pEnumerator;
//    CComPtr<IMMDevice> pDevice;
//    CComCritSecLock<CComAutoCriticalSection> lock(m_cs);
//
//    if (pwf == nullptr) {
//        hr = E_INVALIDARG;
//        goto exit;
//    }
//
//    if (pRenderBuffer == nullptr) {
//        hr = E_INVALIDARG;
//        goto exit;
//    }
//
//    if (renderBufferSizeInSamples <= 0) {
//        hr = E_INVALIDARG;
//        goto exit;
//    }
//
//    hr = CoCreateInstance(
//        __uuidof(MMDeviceEnumerator), NULL,
//        CLSCTX_ALL, __uuidof(IMMDeviceEnumerator),
//        (void**)&pEnumerator);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    hr = pEnumerator->GetDefaultAudioEndpoint(
//        eRender, eConsole, &pDevice);
//    if (hr != S_OK){
//        goto exit;
//    }
//
//    hr = pDevice->Activate(
//        __uuidof(IAudioClient), CLSCTX_ALL,
//        NULL, (void**)&m_pAudioClient);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    hr = m_pAudioClient->Initialize(
//        AUDCLNT_SHAREMODE_SHARED,
//        AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
//        hnsRequestedDuration,
//        0,
//        pwf,
//        NULL);
//    // Note: AUDCLNT_SHAREMODE_EXCLUSIVE is not supported in Durango
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    // Create an event handle and register it for buffer-event notifications.
//    m_hAudioClientEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
//    if (m_hAudioClientEvent == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    hr = m_pAudioClient->SetEventHandle(m_hAudioClientEvent);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    // Get the actual size of the allocated buffer.
//    hr = m_pAudioClient->GetBufferSize(&bufferFrameCount);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    hr = m_pAudioClient->GetService(
//        _uuidof(IAudioRenderClient),
//        (void**)&m_pRenderClient);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    if (bufferFrameCount * pwf->nChannels > renderBufferSizeInSamples) {
//        // Take care of playback of very short buffers, shorter than the preload
//        bufferFrameCount = renderBufferSizeInSamples / pwf->nChannels;
//    }
//
//    // Grab the entire buffer for the initial fill operation.
//    hr = m_pRenderClient->GetBuffer(bufferFrameCount, &pData);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    // Pre-load the initial data into the shared buffer.
//    if(0 != memcpy_s(pData, bufferFrameCount * pwf->nBlockAlign, (void*)pRenderBuffer, bufferFrameCount * pwf->nBlockAlign)) {
//        hr = E_FAIL;
//        goto exit;
//    }
//
//    hr = m_pRenderClient->ReleaseBuffer(bufferFrameCount, 0);
//    if(hr == S_OK);
//
//    m_pwf = pwf;
//    m_renderBuffer = pRenderBuffer;
//    m_renderBufferOffsetInFrames = bufferFrameCount;
//    m_renderBufferSizeInFrames = renderBufferSizeInSamples / pwf->nChannels;
//
//    m_hStartEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
//    if (m_hStartEvent == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    m_hStopEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
//    if (m_hStopEvent == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    //  Now create the thread which is going to drive the render. It will be waiting until Start() is called
//    if (loopOnFlag) {
//        m_loopRenderBufferFlag = true;      //Render buffer loops to keep on playing
//    }
//    else {
//        m_loopRenderBufferFlag = false;
//    }
//
//    m_hRenderingDoneEvent = hRenderingDoneEvent;
//
//    m_hRenderThread = CreateThread(NULL, 0, DoRenderThread, this, 0, NULL);
//    if (m_hRenderThread == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//exit:
//    return hr;
//}
//
//HRESULT WindowsAudioPlayer::GetFramesRendered(_Inout_opt_ DWORD* pFrames, _Inout_opt_ DWORD* pTotalFrames) {
//    if (NULL != pFrames) {
//        *pFrames = m_renderBufferOffsetInFrames;
//    }
//
//    if (NULL != pTotalFrames) {
//        *pTotalFrames = m_renderBufferSizeInFrames;
//    }
//
//    return S_OK;
//}
//
//DWORD WINAPI WindowsAudioPlayer::DoRenderThread(__in LPVOID lpParameter)
//{
//    HRESULT hr = S_OK;
//    if (lpParameter == nullptr) {
//        hr = E_INVALIDARG;
//        goto exit;
//    }
//
//    {
//        WindowsAudioPlayer* pRender = static_cast<WindowsAudioPlayer*>(lpParameter);
//        hr = pRender->DoRenderThread();
//    }
//
//exit:
//    return (DWORD)(FAILED(hr));
//}
//
///// <summary>
///// Render thread - Sends audio to WASAPI render buffer
///// </summary>
///// <returns>
///// Thread return value.
///// </returns>
//HRESULT WindowsAudioPlayer::DoRenderThread() {
//    HRESULT hr = S_OK;
//    BOOL stillPlayingFlag = TRUE;
//    HANDLE mmcssHandle = NULL;
//    DWORD mmcssTaskIndex = 0;
//    UINT32 bufferFrameCount = 0;
//    REFERENCE_TIME actualDurationHNS = 0;
//    HANDLE handleArr[2] = { NULL, NULL };
//    DWORD retVal = 0;
//    BYTE* pRenderBuffer = NULL;
//
//    mmcssHandle = AvSetMmThreadCharacteristicsW(L"Audio", &mmcssTaskIndex);
//    
//    if (mmcssHandle == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    // Get the actual size of the allocated buffer.
//    hr = m_pAudioClient->GetBufferSize(&bufferFrameCount);
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    // Calculate the actual duration of the allocated buffer.
//    actualDurationHNS = REFERENCE_TIME((double)REFTIMES_PER_SEC * bufferFrameCount / m_pwf.nSamplesPerSec);
//
//    // Park here until the start or stop events are signaled
//    handleArr[0] = m_hStopEvent;
//    handleArr[1] = m_hStartEvent;
//    if (handleArr[0] == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    if (handleArr[1] == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    pRenderBuffer = (BYTE*)m_renderBuffer;
//    if (pRenderBuffer == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    retVal = WaitForMultipleObjects(2, handleArr, FALSE, INFINITE);
//
//    switch (retVal) {
//    case WAIT_OBJECT_0: // Stop playback and exit thread
//        hr = S_OK;
//        goto exit;
//
//    case WAIT_OBJECT_0 + 1: // Continue on to start playback
//        break;
//
//    default: // Some other error
//        hr = E_FAIL;
//        goto exit;
//    }
//
//    // Now we either wait for a stop event of WASAPI event for more audio samples
//    handleArr[0] = m_hStopEvent;
//    handleArr[1] = m_hAudioClientEvent;
//    if (handleArr[0] == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    if (handleArr[1] == nullptr) {
//        hr = E_POINTER;
//        goto exit;
//    }
//
//    hr = m_pAudioClient->Start(); // Start playing.
//    if (hr != S_OK) {
//        goto exit;
//    }
//
//    while (stillPlayingFlag) {
//        UINT32 numFramesPadding = 0;
//        BYTE* pData = NULL;
//
//        // Wait for next buffer event to be signaled.
//        //Todo why 2000? get it from hnsDuration
//        retVal = WaitForMultipleObjects(2, handleArr, FALSE, 2000);
//
//        switch (retVal) {
//        case WAIT_OBJECT_0: // Stop playback
//            hr = S_OK;
//            goto exit;
//
//        case WAIT_OBJECT_0 + 1: // WASAPI needs more data to play
//            break;
//
//        case WAIT_TIMEOUT: // Event handle timed out after a 2-second wait.
//            hr = ERROR_TIMEOUT;
//            goto exit;
//
//        default: // Some other error
//            hr = E_FAIL;
//            goto exit;
//        }
//        //Space available in buffer uiNumFramesPadding
//        //How many more samples we have in hand m_renderBufferSizeInFrames - m_renderBufferOffsetInFrames
//        //Check if we have sufficient frames to fill
//        // See how much buffer space is available.
//        hr = m_pAudioClient->GetCurrentPadding(&numFramesPadding);
//        if (hr != S_OK) {
//            goto exit;
//        }
//
//        UINT32 numFramesAvailable = bufferFrameCount - numFramesPadding;
//
//        // Check how many frames are left to play
//        if (numFramesAvailable + m_renderBufferOffsetInFrames > m_renderBufferSizeInFrames) {
//            if (m_loopRenderBufferFlag) {
//                //Reset buffer offset to 0 so that the render buffer loops
//                m_renderBufferOffsetInFrames = 0;
//            }
//            if (numFramesAvailable + m_renderBufferOffsetInFrames > m_renderBufferSizeInFrames) {
//                numFramesAvailable = m_renderBufferSizeInFrames - m_renderBufferOffsetInFrames;
//                stillPlayingFlag = FALSE;
//            }
//        }
//
//        if (0 == numFramesAvailable) {
//            // We will hit this if the input buffer has no space ready yet or this is the last loop and no more data to play
//            continue;
//        }
//
//        // Grab all the available space in the shared buffer.
//        hr = m_pRenderClient->GetBuffer(numFramesAvailable, &pData);
//        if (hr != S_OK) {
//            goto exit;
//        }
//
//        // Copy next chunk of audio data
//        if (0 != memcpy_s(pData,
//            numFramesAvailable * m_pwf.nBlockAlign,
//            ((BYTE*)pRenderBuffer + (m_renderBufferOffsetInFrames * m_pwf.nBlockAlign)),
//            numFramesAvailable * m_pwf.nBlockAlign)) {
//            hr = E_FAIL;
//            goto exit;
//        }
//
//        // Mute selected channels if needed
//        if (m_muteChannelMask > 0) {
//            UINT32 bytePerSample = m_pwf.wBitsPerSample >> 3;
//            for (UINT32 i = 0; i < m_pwf.nChannels; i++) {
//                if (m_muteChannelMask & (1 << i)) {
//                    for (UINT32 j = 0; j < numFramesAvailable; j++) {
//                        memset(pData + i * bytePerSample + j * m_pwf.nBlockAlign, 0, bytePerSample);
//                    }
//                }
//            }
//        }
//
//        hr = m_pRenderClient->ReleaseBuffer(numFramesAvailable, 0);
//        if (hr != S_OK) {
//            goto exit;
//        }
//
//        m_renderBufferOffsetInFrames += numFramesAvailable;
//    }
//
//    // Wait for last data in buffer to play before stopping.
//    Sleep((DWORD)(actualDurationHNS / REFTIMES_PER_MILLISEC));
//
//exit:
//
//    //If the function fails, the return value is zero
//    if (!AvRevertMmThreadCharacteristics(mmcssHandle)) {
//        hr |= HRESULT_FROM_WIN32(GetLastError());
//    }
//
//    if (m_pAudioClient) {
//        m_pAudioClient->Stop();
//    }
//
//    if (IsValidHandle(m_hRenderingDoneEvent)) {
//        if (!SetEvent(m_hRenderingDoneEvent)) {
//            hr |= HRESULT_FROM_WIN32(GetLastError());
//        }
//    }
//
//    return hr;
//}
