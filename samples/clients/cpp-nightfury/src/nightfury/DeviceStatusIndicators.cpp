#include "DeviceStatusIndicators.h"


void DeviceStatusIndicators::SetStatus(const DeviceStatus status){
    std::cout << "New status : " << DeviceStatusNames::to_string(status) << std::endl;
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