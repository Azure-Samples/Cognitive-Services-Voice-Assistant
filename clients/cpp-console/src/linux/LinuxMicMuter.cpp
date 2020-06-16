// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <string>
#include <thread>
#include "LinuxMicMuter.h"

using namespace MicMuter;

LinuxMicMuter::~LinuxMicMuter()
{
    // todo    
}

int LinuxMicMuter::Initialize()
{
    // todo
    return 0;
}

int LinuxMicMuter::MuteUnmute()
{
    // todo
    return 0;
}

bool LinuxMicMuter::IsMuted()
{
    return _muted;
}