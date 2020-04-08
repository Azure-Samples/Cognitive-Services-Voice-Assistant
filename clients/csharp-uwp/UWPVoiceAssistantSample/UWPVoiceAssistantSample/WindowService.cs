// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using Windows.ApplicationModel.Core;

    /// <summary>
    /// Service for managing the app window 
    /// </summary>
    public static class WindowService
    {
        /// <summary>
        /// Closes application window, triggering CoreApplication.Exiting.
        /// </summary>
        public static void CloseWindow()
        {
            CoreApplication.Exit();
        }
    }
}
