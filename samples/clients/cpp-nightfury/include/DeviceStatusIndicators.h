//
// Copyright (c) Microsoft Corporation. All rights reserved.
//

#pragma once

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
namespace DeviceStatusIndicators
{
    inline static void SetStatus(const DeviceStatus status)
    {
        std::cout << "New status : " << DeviceStatusNames::to_string(status) << std::endl;

        // return;

        switch (status)
        {
        case DeviceStatus::Idle:
            system("adk-message-send 'voiceui_status_idle {}' >/dev/null");
            break;
        case DeviceStatus::Initializing:
            system("adk-message-send 'led_start_pattern {pattern:3}' >/dev/null");
            break;
        case DeviceStatus::Ready:
            system("adk-message-send 'led_start_pattern {pattern:13}' >/dev/null");
            break;
        case DeviceStatus::Detecting:
            system("adk-message-send 'led_start_pattern {pattern:7}' >/dev/null");
            break;
        case DeviceStatus::Listening:
            system("adk-message-send 'led_start_pattern {pattern:0}' >/dev/null");
            break;
        case DeviceStatus::Thinking:
            system("adk-message-send 'led_start_pattern {pattern:10}' >/dev/null");
            break;
        case DeviceStatus::Speaking:
        default:
            break;
        }
    }
}