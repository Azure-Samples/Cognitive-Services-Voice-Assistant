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

//using namespace Microsoft::WRL;
//using namespace Windows::Media::Devices;
//using namespace Windows::Storage::Streams;

#define SAFE_RELEASE(punk)  \
              if ((punk) != NULL)  \
                { (punk)->Release(); (punk) = NULL; }

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
        BOOL _originalMuteState;
        bool _muted;
    };
};
