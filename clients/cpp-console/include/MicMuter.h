// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//#include <AudioClient.h>
//#include <mmdeviceapi.h>
//#include <Windows.h>

/// <summary>
/// Abstract object used to define the interface to an AudioCapturer
/// </summary>
/// <remarks>
/// </remarks>
class IMicMuter
{
public:
    virtual ~IMicMuter() = default;

    virtual int Initialize() = 0;
    virtual int MuteUnmute() = 0;
    virtual bool IsMuted() = 0;
};
