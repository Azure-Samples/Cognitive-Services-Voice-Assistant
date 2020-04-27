// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioInput
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Devices.Enumeration;
    using Windows.Media.Capture;
    using Windows.Security.Authorization.AppCapabilityAccess;

    /// <summary>
    /// Encapsulation of the audio device and microphone permissions monitoring needed to track
    /// and respond to changes in audio input availability on the system.
    /// </summary>
    public class AudioCaptureControl
        : IDisposable
    {
        private static readonly object InstanceLock = new object();
        private static AudioCaptureControl instanceCaptureControl;
        private readonly SemaphoreSlim mediaCaptureSemaphore;
        private readonly DeviceWatcher captureDeviceWatcher;
        private readonly ManualResetEventSlim enumerationCompletedEvent;
        private bool firstEnumerationCompleted = false;
        private MediaCapture deviceMediaCapture;
        private bool alreadyDisposed = false;
        private Timer audioPollTimer;

        private AudioCaptureControl()
        {
            this.mediaCaptureSemaphore = new SemaphoreSlim(1, 1);

            // We will query and monitor the microphone capability for audio input; the call to RequestAccessAsync needs to occur once we've
            // created a page so it can pop a consent prompt.
            this.MicrophoneCapability = AppCapability.Create("microphone");
            this.MicrophoneCapability.AccessChanged += (capability, args) =>
            {
                if (capability.CheckAccess() == AppCapabilityAccessStatus.Allowed)
                {
                    _ = this.StartVolumePollingAsync();
                }
            };

            // We'll also monitor the audio input devices available on the system. This allows clear feedback to users when no device is connected
            // as well as responsiveness to devices being added or removed.
            this.captureDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.AudioCapture);
            this.captureDeviceWatcher.Added += async (s, e)
                => await this.OnAudioCaptureDevicesChangedAsync();
            this.captureDeviceWatcher.Removed += async (s, e)
                => await this.OnAudioCaptureDevicesChangedAsync();
            this.captureDeviceWatcher.Updated += async (s, e)
                => await this.OnAudioCaptureDevicesChangedAsync();
            this.enumerationCompletedEvent = new ManualResetEventSlim(false);
            this.captureDeviceWatcher.EnumerationCompleted += (s, e) =>
            {
                this.firstEnumerationCompleted = true;
                this.enumerationCompletedEvent.Set();
            };
            this.captureDeviceWatcher.Start();
        }

        /// <summary>
        /// Raised when an audio input device change occurs on the system.
        /// </summary>
        public event Action AudioInputDeviceChanged;

        /// <summary>
        /// Raised upon a change in input volume level (including muting or unmuting).
        /// </summary>
        public event Action InputVolumeStateChanged;

        /// <summary>
        /// Gets the current input audio volume level, measured from 0.0 to 100.0.
        /// </summary>
        public float CaptureVolumeLevel { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the input audio is currently muted.
        /// </summary>
        public bool CaptureMuted { get; private set; }

        /// <summary>
        /// Gets the application capability object for microphone permissions used by this instance.
        /// </summary>
        public AppCapability MicrophoneCapability { get; private set; }

        /// <summary>
        /// Gets a value indicating whether an audio device is available for input.
        /// </summary>
        public bool HasAudioInputAvailable { get; private set; }

        /// <summary>
        /// Asynchronously initializes an instance of the AudioCaptureControl class.
        /// </summary>
        /// <returns> A task that completes when the instance is available. </returns>
        public static async Task<AudioCaptureControl> GetInstanceAsync()
        {
            lock (AudioCaptureControl.InstanceLock)
            {
                if (AudioCaptureControl.instanceCaptureControl == null)
                {
                    AudioCaptureControl.instanceCaptureControl = new AudioCaptureControl();
                }
            }

            var control = AudioCaptureControl.instanceCaptureControl;
            await Task.Run(() => control.enumerationCompletedEvent.Wait());
            return control;
        }

        /// <summary>
        /// Implementation of the IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal implementation of the IDisposable interface using the common disposing pattern.
        /// </summary>
        /// <param name="disposing"> Whether this was invoked from a dispose call. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.alreadyDisposed)
            {
                this.audioPollTimer?.Dispose();
                this.deviceMediaCapture?.Dispose();
                this.mediaCaptureSemaphore?.Dispose();
                this.enumerationCompletedEvent?.Dispose();

                this.audioPollTimer = null;
                this.deviceMediaCapture = null;

                this.alreadyDisposed = true;
            }
        }

        private async Task OnAudioCaptureDevicesChangedAsync()
        {
            var allDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            this.HasAudioInputAvailable = allDevices.Any();

            using (await this.mediaCaptureSemaphore.AutoReleaseWaitAsync())
            {
                this.deviceMediaCapture?.Dispose();
                this.deviceMediaCapture = null;
            }

            if (this.MicrophoneCapability.CheckAccess() == AppCapabilityAccessStatus.Allowed
                && this.HasAudioInputAvailable)
            {
                await this.StartVolumePollingAsync();
            }

            if (this.firstEnumerationCompleted)
            {
                this.AudioInputDeviceChanged?.Invoke();
            }
        }

        private async Task StartVolumePollingAsync()
        {
            var captureSettings = new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                MediaCategory = MediaCategory.Speech,
            };

            using (await this.mediaCaptureSemaphore.AutoReleaseWaitAsync())
            {
                this.deviceMediaCapture = new MediaCapture();
                await this.deviceMediaCapture.InitializeAsync(captureSettings);
            }

            if (this.audioPollTimer == null)
            {
                this.audioPollTimer = new Timer(this.UpdateVolumeState, null, 0, 2000);
            }
        }

        private async void UpdateVolumeState(object timerState)
        {
            var previousVolume = this.CaptureVolumeLevel;
            var previouslyMuted = this.CaptureMuted;

            using (await this.mediaCaptureSemaphore.AutoReleaseWaitAsync())
            {
                if (this.deviceMediaCapture == null)
                {
                    this.CaptureVolumeLevel = 0;
                    this.CaptureMuted = true;
                }
                else
                {
                    var controller = this.deviceMediaCapture.AudioDeviceController;
                    this.CaptureVolumeLevel = controller.VolumePercent;
                    this.CaptureMuted = controller.Muted;
                }
            }

            var volumeChanged = this.CaptureVolumeLevel != previousVolume;
            var mutingChanged = this.CaptureMuted != previouslyMuted;

            if (this.firstEnumerationCompleted && (volumeChanged || mutingChanged))
            {
                this.InputVolumeStateChanged?.Invoke();
            }
        }
    }
}
