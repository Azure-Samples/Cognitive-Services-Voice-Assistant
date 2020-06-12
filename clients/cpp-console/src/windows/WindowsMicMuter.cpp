// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <atlbase.h>
#include <string>
#include <thread>
#include "WindowsMicMuter.h"

using namespace MicMuter;

WindowsMicMuter::~WindowsMicMuter()
{
    if (m_endpointVolume)
    {
        m_endpointVolume->SetMute(_originalMuteState, NULL);
        SAFE_RELEASE(m_endpointVolume);
    }
}

int WindowsMicMuter::Initialize()
{
    HRESULT hr = S_OK;
    CComPtr<IMMDeviceEnumerator> pEnumerator;
    CComPtr<IMMDevice> pDevice;

    // Begin audio device setup
    CComCritSecLock<CComAutoCriticalSection> lock(m_cs);

    // Get a device enumator from the OS
    hr = CoCreateInstance(
        __uuidof(MMDeviceEnumerator), NULL,
        CLSCTX_ALL, __uuidof(IMMDeviceEnumerator),
        (void**)&pEnumerator);
    if (hr != S_OK)
    {
        fprintf(stderr, "Error. Failed to get a device enumator from the OS. Error: 0x%08x\n", hr);
        goto exit;
    }

    // Use the enumerator to get the default capture device
    hr = pEnumerator->GetDefaultAudioEndpoint(
        eCapture, eConsole, &pDevice);
    if (hr != S_OK)
    {
        fprintf(stderr, "Error. Failed to use the enumerator to get the default capture device. Error: 0x%08x\n", hr);
        goto exit;
    }

    // Activate the default capture device.
    hr = pDevice->Activate(__uuidof(IAudioEndpointVolume), CLSCTX_INPROC_SERVER, NULL,
        reinterpret_cast<void**>(&m_endpointVolume));
    if (hr != S_OK)
    {
        fprintf(stderr, "Error. Failed to activate the default capture device. Error: 0x%08x\n", hr);
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
    else
    {
        fprintf(stderr, "Error. Failed to mute/unmute the default capture device. Error: 0x%08x\n", hr);
    }

    return hr;
}

bool WindowsMicMuter::IsMuted()
{
    return _muted;
}