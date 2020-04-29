﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using UWPVoiceAssistantSample.AudioInput;
    using Windows.Security.Authorization.AppCapabilityAccess;
    using Windows.System.Power;
    using Windows.UI;

    /// <summary>
    /// Helper class used to abstract symbol, color, and text related to audio capture status.
    /// </summary>
    public class UIAudioStatus
    {
        /// <summary>
        /// Constant indicating microphone is available.
        /// </summary>
        public static readonly string MicrophoneAvailable = "Microphone is available.";

        /// <summary>
        /// Constant indiciating voice activation is allowed.
        /// </summary>
        public static readonly string VoiceActivationEnabledMessage = "Voice activation is configured and available.";

        /// <summary>
        /// Initializes a new instance of the <see cref="UIAudioStatus"/> class.
        /// </summary>
        /// <param name="symbol"> The visual symbol associated with the status. </param>
        /// <param name="color"> The color associated with the status. </param>
        /// <param name="status"> The textual representation of the status. </param>
        private UIAudioStatus(string glyph, Color color, List<string> status)
        {
            this.Glyph = glyph;
            this.Color = color;
            this.Status = status;
        }

        /// <summary>
        /// Gets the Unicode point for the visual symbol representing this piece of status.
        /// </summary>
        public string Glyph { get; private set; }

        /// <summary>
        /// Gets the color used to represent this status in UI.
        /// </summary>
        public Color Color { get; private set; }

        /// <summary>
        /// Gets the status string for this piece of audio status.
        /// </summary>
        public List<string> Status { get; private set; }

        /// <summary>
        /// Queries microphone status via the AudioCaptureControl global instance and constructs
        /// a UIAudioStatus object representing current state.
        /// </summary>
        /// <returns> A UIAudioStatus representing the state of the microphone. </returns>
        public static async Task<UIAudioStatus> GetMicrophoneStatusAsync()
        {
            var control = await AudioCaptureControl.GetInstanceAsync();
            var capabilityStatus = control.MicrophoneCapability.CheckAccess();

            string glyph;
            Color color;
            List<string> statusText = new List<string>();

            if (capabilityStatus == AppCapabilityAccessStatus.UserPromptRequired)
            {
                glyph = Glyphs.Cancel;
                color = Colors.Red;
                statusText.Add("Microphone permissions have not yet been prompted.");
            }
            else if (capabilityStatus != AppCapabilityAccessStatus.Allowed)
            {
                glyph = Glyphs.Cancel;
                color = Colors.Red;
                statusText.Add("Microphone permission is denied.");
            }
            else if (!control.HasAudioInputAvailable)
            {
                glyph = Glyphs.Cancel;
                color = Colors.Red;
                statusText.Add("No audio input device is present.");
            }
            else if (control.CaptureMuted)
            {
                glyph = Glyphs.Microphone;
                color = Colors.Red;
                statusText.Add("Microphone is muted and keywords can't be heard.");
            }
            else if (control.CaptureVolumeLevel < 10f)
            {
                glyph = Glyphs.Microphone;
                color = Colors.Red;
                statusText.Add("Microphone volume is very low and keywords may not be heard.");
            }
            else if (capabilityStatus == AppCapabilityAccessStatus.Allowed)
            {
                glyph = Glyphs.Microphone;
                color = Colors.Green;
                statusText.Add(MicrophoneAvailable);
            }
            else
            {
                glyph = Glyphs.Cancel;
                color = Colors.Red;
            }

            return new UIAudioStatus(glyph, color, statusText);
        }

        /// <summary>
        /// Queries the Conversational Agent Platform for the current state of the selected
        /// configuration and generates a UIAudioStatus object representing the state of voice
        /// activation.
        /// </summary>
        /// <returns> A UIAudioStatus representing current voice activation state. </returns>
        public static async Task<UIAudioStatus> GetVoiceActivationStatusAsync()
        {
            var services = (App.Current as App).Services;
            var keywordRegistration = services.GetRequiredService<IKeywordRegistration>();
            var agentSessionManager = services.GetRequiredService<IAgentSessionManager>();

            var session = await agentSessionManager.GetSessionAsync();
            var config = await keywordRegistration.GetOrCreateKeywordConfigurationAsync();
            var audioControl = await AudioCaptureControl.GetInstanceAsync();

            string glyph = Glyphs.Cancel;
            Color color = Colors.Red;
            List<string> status = new List<string>();

            if (session == null)
            {
                status.Add("Unable to obtain agent session. Please verify registration.");
            }
            else if (config == null)
            {
                status.Add("No valid keyword configuration. Please check your source code configuration.");
            }
            else if (!config.AvailabilityInfo.HasPermission)
            {
                status.Add("Voice activation permissions are currently denied.");
            }
            else if (!config.AvailabilityInfo.HasSystemResourceAccess)
            {
                status.Add("Voice activation is unavailable. Please verify against keyword conflicts.");
            }
            else if (!config.AvailabilityInfo.IsEnabled)
            {
                status.Add("Voice activation is programmatically disabled by the app.");
            }
            else if (!config.IsActive)
            {
                status.Add("Voice activation is unavailable for an unknown reason.");
            }
            else if (audioControl.CaptureMuted || audioControl.CaptureVolumeLevel < 5f)
            {
                glyph = Glyphs.Warning;
                color = Colors.DarkOrange;
                status.Add("Voice activation is available but may be degraded due to microphone state.");
            }
            else if (!MVARegistrationHelpers.IsBackgroundTaskRegistered)
            {
                glyph = Glyphs.Warning;
                color = Colors.DarkOrange;
                status.Add("Background task is not configured and voice activation will only work while the application is already active.");
            }
            else if (VoiceActivationIsPowerRestricted())
            {
                glyph = Glyphs.Warning;
                color = Colors.DarkOrange;
                status.Add("The system is currently power restricted and voice activation may not be available.");
            }
            else if (config.AvailabilityInfo.IsEnabled && MVARegistrationHelpers.IsBackgroundTaskRegistered)
            {
                glyph = Glyphs.FeedbackApp;
                color = Colors.Green;
                status.Add(VoiceActivationEnabledMessage);
            }
            else
            {
                glyph = Glyphs.Warning;
                color = Colors.DarkOrange;
            }

            return new UIAudioStatus(glyph, color, status);
        }

        private static bool VoiceActivationIsPowerRestricted()
        {
            return
                PowerManager.EnergySaverStatus == EnergySaverStatus.On
                && PowerManager.BatteryStatus == BatteryStatus.Discharging
                && PowerManager.RemainingChargePercent <= 20;
        }
    }
}
