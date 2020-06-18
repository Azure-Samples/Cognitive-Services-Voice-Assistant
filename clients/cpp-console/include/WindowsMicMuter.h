// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <AudioClient.h>
#include <EndpointVolume.h>
#include <mfapi.h>
#include <mmdeviceapi.h>
#include <thread>
#include <Windows.h>
#include <wrl\implements.h>
#include "MicMuter.h"

#define SAFE_RELEASE(ptr)  \
              if ((ptr) != NULL)  \
                { (ptr)->Release(); (ptr) = NULL; }

namespace MicMuter
{
    class WindowsMicMuter :public IMicMuter
    {
    public:
        ~WindowsMicMuter();

        virtual int Initialize() final;
        virtual int MuteUnmute() final;
        virtual bool IsMuted() final;

    private:
        ATL::CComAutoCriticalSection m_cs;
        IAudioEndpointVolume* m_endpointVolume = NULL;
        BOOL _originalMuteState = false;
        bool _muted  = false;
    };
};
