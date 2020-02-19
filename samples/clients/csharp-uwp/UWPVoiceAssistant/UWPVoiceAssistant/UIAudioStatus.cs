// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System.Threading.Tasks;
    using UWPVoiceAssistant.AudioInput;
    using Windows.Security.Authorization.AppCapabilityAccess;
    using Windows.UI;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Helper class used to abstract symbol, color, and text related to audio capture status.
    /// </summary>
    public class UIAudioStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIAudioStatus"/> class.
        /// </summary>
        /// <param name="symbol"> The visual symbol associated with the status. </param>
        /// <param name="color"> The color associated with the status. </param>
        /// <param name="status"> The textual representation of the status. </param>
        private UIAudioStatus(Symbol symbol, Color color, string status)
        {
            this.Symbol = symbol;
            this.Color = color;
            this.Status = status;
        }

        /// <summary>
        /// Gets the visual symbol to use for this piece of status.
        /// </summary>
        public Symbol Symbol { get; private set; }

        /// <summary>
        /// Gets the color used to represent this status in UI.
        /// </summary>
        public Color Color { get; private set; }

        /// <summary>
        /// Gets the status string for this piece of audio status.
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Queries microphone status via the AudioCaptureControl global instance and constructs
        /// a UIAudioStatus object representing current state.
        /// </summary>
        /// <returns> A UIAudioStatus representing the state of the microphone. </returns>
        public static async Task<UIAudioStatus> GetMicrophoneStatusAsync()
        {
            var control = await AudioCaptureControl.GetInstanceAsync();
            var capabilityStatus = control.MicrophoneCapability.CheckAccess();

            Symbol symbol;
            Color color;
            string statusText;

            if (capabilityStatus == AppCapabilityAccessStatus.UserPromptRequired)
            {
                symbol = Symbol.Cancel;
                color = Colors.Red;
                statusText = "Microphone permissions have not yet been prompted.";
            }
            else if (capabilityStatus != AppCapabilityAccessStatus.Allowed)
            {
                symbol = Symbol.Cancel;
                color = Colors.Red;
                statusText = "Microphone permission is denied. Click here to view settings.";
            }
            else if (!control.HasAudioInputAvailable)
            {
                symbol = Symbol.Cancel;
                color = Colors.Red;
                statusText = "No audio input device is present.";
            }
            else if (control.CaptureMuted)
            {
                symbol = Symbol.Microphone;
                color = Colors.Red;
                statusText = "Microphone is muted and keywords can't be heard.";
            }
            else if (control.CaptureVolumeLevel < 10f)
            {
                symbol = Symbol.Microphone;
                color = Colors.Red;
                statusText = "Microphone volume is very low and keywords may not be heard.";
            }
            else
            {
                symbol = Symbol.Microphone;
                color = Colors.Black;
                statusText = "Microphone is available.";
            }

            return new UIAudioStatus(symbol, color, statusText);
        }

        /// <summary>
        /// Queries the Conversational Agent Platform for the current state of the selected
        /// configuration and generates a UIAudioStatus object representing the state of voice
        /// activation.
        /// </summary>
        /// <returns> A UIAudioStatus representing current voice activation state. </returns>
        public static async Task<UIAudioStatus> GetVoiceActivationStatusAsync()
        {
            var session = await AppSharedState.GetSessionAsync();
            var config = AppSharedState.KeywordConfiguration;
            var audioControl = await AudioCaptureControl.GetInstanceAsync();

            Symbol symbol = Symbol.Cancel;
            Color color = Colors.Red;
            string status;

            if (session == null)
            {
                status = "Unable to obtain agent session. Please verify registration.";
            }
            else if (config == null)
            {
                status = "No valid keyword configuration. Please check your source code configuration.";
            }
            else if (!config.AvailabilityInfo.HasPermission)
            {
                status = "Voice activation permissions are currently denied. Click here to view settings.";
            }
            else if (!config.AvailabilityInfo.HasSystemResourceAccess)
            {
                status = "Voice activation is unavailable. Please verify against keyword conflicts.";
            }
            else if (!config.AvailabilityInfo.IsEnabled)
            {
                status = "Voice activation is programmatically disabled by the app.";
            }
            else if (!config.IsActive)
            {
                status = "Voice activation is unavailable for an unknown reason.";
            }
            else if (audioControl.CaptureMuted || audioControl.CaptureVolumeLevel < 5f)
            {
                symbol = Symbol.LikeDislike;
                color = Colors.DarkOrange;
                status = "Voice activation is available but may be degraded due to microphone state.";
            }
            else if (!MVARegistrationHelpers.IsBackgroundTaskRegistered)
            {
                symbol = Symbol.LikeDislike;
                color = Colors.DarkOrange;
                status = "Background task is not configured and voice activation will only work while the application is already active.";
            }
            else
            {
                symbol = Symbol.Like;
                color = Colors.Black;
                status = "Voice activation is configured and available.";
            }

            return new UIAudioStatus(symbol, color, status);
        }
    }
}
