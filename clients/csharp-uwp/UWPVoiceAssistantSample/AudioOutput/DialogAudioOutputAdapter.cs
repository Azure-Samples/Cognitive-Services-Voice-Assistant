// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UWPVoiceAssistantSample.AudioOutput;
    using Windows.Devices.Enumeration;
    using Windows.Media.Audio;
    using Windows.Media.Devices;
    using Windows.Media.MediaProperties;
    using Windows.Media.Render;

    /// <summary>
    /// Output audio implementation generic to any DialogAudioOutputStream source that manages the creation and use of
    /// a Windows AudioGraph for the output of media associated with a DialogResponse, such as text-to-speech output.
    /// </summary>
    public class DialogAudioOutputAdapter
        : IDisposable, IDialogAudioOutputAdapter
    {
        private readonly SemaphoreSlim graphSemaphore;
        private readonly AutoResetEvent outputEndedEvent;
        private readonly ILogProvider log = LogRouter.GetClassLogger();
        private DialogAudioOutputStream activeOutputStream;
        private AudioGraph graph;
        private AudioFrameInputNode frameInputNode;
        private AudioEncodingProperties frameInputEncoding;
        private AudioDeviceOutputNode deviceOutputNode;
        private string lastOutputDeviceId;
        private bool alreadyDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudioOutputAdapter"/> class.
        /// </summary>
        private DialogAudioOutputAdapter()
        {
            this.outputEndedEvent = new AutoResetEvent(false);
            this.graphSemaphore = new SemaphoreSlim(1, 1);
            this.frameInputEncoding = LocalSettingsHelper.OutputFormat.Encoding;
            this.ConfigureOutputDeviceWatching();
        }

        /// <summary>
        /// Raised when the current active audio playback has finished.
        /// </summary>
        public event Action OutputEnded;

        /// <summary>
        /// Gets a value indicating whether the adapter is currently playing audio.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Gets or sets the encoding to be used for output. Should match the expected format of data being provided.
        /// </summary>
        public AudioEncodingProperties OutputEncoding
        {
            get => this.frameInputEncoding;
            set
            {
                this.frameInputEncoding = value;
                _ = this.RegenerateAudioGraphAsync();
            }
        }

        /// <summary>
        /// Asynchronously creates a new instance of the DialogAudioOutputAdapter class and initializes the underlying
        /// audio resources to begin audio output to the default output device.
        /// </summary>
        /// <returns> A task that completes once the adapter is ready. </returns>
        public static async Task<DialogAudioOutputAdapter> CreateAsync()
        {
            var adapter = new DialogAudioOutputAdapter();
            await adapter.RegenerateAudioGraphAsync();
            return adapter;
        }

        /// <summary>
        /// Cancels any current playback on the adapter and asynchronously begins playback of the provided Speech SDK
        /// dialog output audio.
        /// </summary>
        /// <param name="stream"> The output stream to play. </param>
        /// <returns> A task that completes once the pending output is completed. </returns>
        public async Task PlayAudioAsync(DialogAudioOutputStream stream)
        {
            await this.StopPlaybackAsync();

            using (await this.graphSemaphore.AutoReleaseWaitAsync())
            {
                this.activeOutputStream = stream;
                this.outputEndedEvent.Reset();
                this.frameInputNode?.Start();
                this.IsPlaying = true;
            }

            await Task.Run(() => this.outputEndedEvent.WaitOne());
        }

        /// <summary>
        /// Immediately stops output playback and discards the active output stream.
        /// </summary>
        /// <returns> A task that completes once playback has stopped. </returns>
        public async Task StopPlaybackAsync()
        {
            using (await this.graphSemaphore.AutoReleaseWaitAsync())
            {
                this.frameInputNode?.Stop();
                if (this.activeOutputStream != null)
                {
                    this.outputEndedEvent.Set();
                    this.OutputEnded?.Invoke();
                    this.Dispose(true);
                    this.activeOutputStream = null;
                }

                this.IsPlaying = false;
            }

            this.log.Log(LogMessageLevel.AudioLogs, $"Stopping Audio Playback");
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Free disposable resources per the IDisposable interface.
        /// </summary>
        /// <param name="disposing"> Whether managed state is being disposed. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.alreadyDisposed)
            {
                if (disposing)
                {
                    this.frameInputNode?.Dispose();
                    this.deviceOutputNode?.Dispose();
                    this.graph?.Dispose();
                    this.graphSemaphore?.Dispose();
                    this.outputEndedEvent?.Dispose();
                }

                this.alreadyDisposed = true;
            }
        }

        private void ConfigureOutputDeviceWatching()
        {
            // DefaultAudioRenderDeviceChanged will fire every time the 'Default Device' or 'Default Communications
            // Device' for playback changes.
            MediaDevice.DefaultAudioRenderDeviceChanged += async (_, args) =>
            {
                // Our output AudioGraph with MediaCategory.Speech will correspond to the Communications role.
                if (args.Role == AudioDeviceRole.Communications)
                {
                    var allCurrentDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
                    foreach (var device in allCurrentDevices)
                    {
                        // Device change reporting can be "bouncy," so we'll ensure we're not "changing" unnecessarily
                        // to the same device.
                        if (device.Id == args.Id && args.Id != this.lastOutputDeviceId)
                        {
                            this.log.Log(LogMessageLevel.AudioLogs, $"New audio output device: {device.Name}");
                            await this.RegenerateAudioGraphAsync();
                        }
                    }
                }
            };
        }

        private async Task RegenerateAudioGraphAsync()
        {
            using (await this.graphSemaphore.AutoReleaseWaitAsync())
            {
                // End any playback already happening and dispose existing resources
                this.graph?.Stop();

                var graphSettings = new AudioGraphSettings(AudioRenderCategory.Speech);
                var graphCreationResult = await AudioGraph.CreateAsync(graphSettings);
                if (graphCreationResult.Status != AudioGraphCreationStatus.Success)
                {
                    var statusText = graphCreationResult.Status.ToString();
                    var errorText = graphCreationResult.ExtendedError.ToString();
                    this.log.Error($"Unable to create AudioGraph: {statusText}: {errorText}");
                    return;
                }

                this.graph = graphCreationResult.Graph;

                this.frameInputNode = this.graph.CreateFrameInputNode(this.OutputEncoding);
                this.frameInputNode.QuantumStarted += this.OnFrameInputQuantumStartedAsync;
                this.frameInputNode.Stop();

                var outputNodeCreationResult = await this.graph.CreateDeviceOutputNodeAsync();
                if (outputNodeCreationResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    var statusText = outputNodeCreationResult.Status.ToString();
                    var errorText = outputNodeCreationResult.ExtendedError.ToString();
                    this.log.Error($"Unable to create DeviceOutputNode: {statusText}: {errorText}");
                    return;
                }

                this.deviceOutputNode = outputNodeCreationResult.DeviceOutputNode;
                this.lastOutputDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Communications);
                this.frameInputNode.AddOutgoingConnection(this.deviceOutputNode);
                this.deviceOutputNode.Start();

                this.graph.Start();

                if (this.IsPlaying)
                {
                    this.frameInputNode.Start();
                }
            }
        }

        private async void OnFrameInputQuantumStartedAsync(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            var bytesPerSample = this.OutputEncoding.BitsPerSample / 8;
            var bytesRequested = args.RequiredSamples * bytesPerSample;

            var requestBuffer = new byte[bytesRequested];
            var bytesActuallyRead = this.activeOutputStream.Read(requestBuffer, 0, requestBuffer.Length);

            if (bytesActuallyRead > 0)
            {
                using (var frameToAdd = DialogAudioOutputUnsafeMethods.CreateFrameFromBytes(requestBuffer, bytesActuallyRead))
                {
                    this.frameInputNode.AddFrame(frameToAdd);
                }
            }

            // If fewer than the requested number of bytes were read, the source stream is exhausted and this playback
            // is complete.
            if (bytesActuallyRead < bytesRequested)
            {
                await this.StopPlaybackAsync();
            }
        }
    }
}