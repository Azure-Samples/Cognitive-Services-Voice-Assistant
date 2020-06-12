// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include "MicMuter.h"

namespace MicMuter
{
    class LinuxMicMuter :public IMicMuter
    {
    public:
        ~LinuxMicMuter();

        virtual int Initialize() final;
        virtual int MuteUnmute() final;
        virtual bool IsMuted() final;

    private:
        bool _muted = false;
    };
};
