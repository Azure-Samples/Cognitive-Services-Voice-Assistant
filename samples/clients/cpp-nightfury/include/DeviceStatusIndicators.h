//
// Copyright (c) Microsoft Corporation. All rights reserved.
//

#pragma once
#include <string>
#include <iostream>

enum class DeviceStatus
{
    // The device is in an inactive or passively listening (keyword-only) mode
    Idle,
    // The device is currently working to become ready to accept input
    Initializing,
    // The device is now ready to accept input
    Ready,
    // The device is in the process of detecting a keyword
    Detecting,
    // The device is actively capturing and transmitting all captured audio
    Listening,
    // The device has finished active capture and is now waiting for an action
    Thinking,
    // The device is speaking
    Speaking
};

namespace DeviceStatusNames
{
    const std::string name_map[]{ "Idle", "Initializing", "Ready", "Detecting", "Listening", "Thinking", "Speaking" };

    static const std::string to_string(DeviceStatus status)
    {
        return name_map[(int)status];
    }
}

// Individual devices will have differing capabilities and interfaces for sharing
// interaction state in a headless environment. This one implementation under a
// general abstraction.
class DeviceStatusIndicators
{
    public:
        static void SetStatus(const DeviceStatus status);
};