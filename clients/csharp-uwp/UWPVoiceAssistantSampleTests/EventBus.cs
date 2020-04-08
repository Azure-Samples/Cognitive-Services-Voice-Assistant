// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    public class EventBus
    {
        public delegate void ActivationHandler();
        public static event ActivationHandler ReceivedForegroundActivationEvent;
        public static event ActivationHandler ReceivedBackgroundActivationEvent;

        public static void ReceivedForegroundActivation()
        {
            ReceivedForegroundActivationEvent?.Invoke();
        }

        public static void ReceivedBackgroundActivation()
        {
            ReceivedBackgroundActivationEvent?.Invoke();
        }
    }
}
