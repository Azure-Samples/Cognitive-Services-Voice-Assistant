// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "DeviceStatusIndicators.h"

void DeviceStatusIndicators::SetStatus(const DeviceStatus status, const bool muted)
{
    if (muted)
    {
        std::cout << ansi::foreground_yellow << "New status : " << DeviceStatusNames::to_string(status) << ansi::foreground_red << "         (Microphone is muted)" << ansi::reset << std::endl;
    }
    else
    {
        std::cout << ansi::foreground_yellow << "New status : " << DeviceStatusNames::to_string(status) << ansi::reset << std::endl;
    }

    switch (status)
    {
    case DeviceStatus::Idle:
        break;
    case DeviceStatus::Initializing:
        break;
    case DeviceStatus::Ready:
        break;
    case DeviceStatus::Detecting:
        break;
    case DeviceStatus::Listening:
        break;
    case DeviceStatus::Thinking:
        break;
    case DeviceStatus::Speaking:
    default:
        break;
    }
}