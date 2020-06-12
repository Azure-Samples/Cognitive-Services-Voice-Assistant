// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <atlbase.h>
#include <string>
#include <thread>
#include "WindowsMicMuter.h"

//#include <mutex>
//#include <avrt.h>

using namespace MicMuter;

WindowsMicMuter::~WindowsMicMuter()
{
    m_endpointVolume->SetMute(_originalMuteState, NULL);
    SAFE_RELEASE(m_endpointVolume);
}

int WindowsMicMuter::Initialize()
{
    HRESULT hr = S_OK;
    CComPtr<IMMDeviceEnumerator> pEnumerator;
    CComPtr<IMMDevice> pDevice;

    // begin Audio Device Setup
    CComCritSecLock<CComAutoCriticalSection> lock(m_cs);

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
        eCapture, eConsole, &pDevice);
    if (hr != S_OK)
    {
        goto exit;
    }

    hr = pDevice->Activate(__uuidof(IAudioEndpointVolume), CLSCTX_INPROC_SERVER, NULL,
        reinterpret_cast<void**>(&m_endpointVolume));
    if (hr != S_OK)
    {
        goto exit;
    }
    m_endpointVolume->GetMute(&_originalMuteState);
    _muted = _originalMuteState == TRUE;

exit:

    return hr;
}

int WindowsMicMuter::MuteUnmute()
{
    HRESULT hr = S_OK;

    if (_muted)
    {
        hr = m_endpointVolume->SetMute(FALSE, NULL);
    }
    else
    {
        hr = m_endpointVolume->SetMute(TRUE, NULL);
    }

    if (hr == S_OK)
    {
        _muted = !_muted;
    }

    return hr;
}

bool WindowsMicMuter::IsMuted()
{
    return _muted;
}