// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient
{
    using Jot;

    internal static class Services
    {
        private static StateTracker tracker = new StateTracker();

        public static StateTracker Tracker { get => tracker; set => tracker = value; }
    }
}
