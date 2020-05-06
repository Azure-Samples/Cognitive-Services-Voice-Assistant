// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "DeviceStatusIndicators.h"

void DeviceStatusIndicators::SetStatus(const DeviceStatus status)
{
    std::cout << "New status : " << DeviceStatusNames::to_string(status) << std::endl;
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