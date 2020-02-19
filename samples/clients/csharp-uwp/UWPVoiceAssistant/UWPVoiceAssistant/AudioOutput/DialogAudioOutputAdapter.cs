// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Devices.Enumeration;
    using Windows.Media;
    using Windows.Media.Audio;
    using Windows.Media.MediaProperties;
    using Windows.Media.Render;

    /// <summary>
    /// Responsible for Creating and maintaining the Audio Graph for comminucation with Direct Line Speech.
    /// </summary>
    public class DialogAudioOutputAdapter
        : IDisposable
    {
        private readonly SemaphoreSlim graphSemaphore;
        private readonly AutoResetEvent outputEndedEvent;
        private readonly object audioOutputStreamsLock;
        private readonly Queue<DialogAudioOutputStream> audioOutputStreams;

        private AudioGraph graph;
        private AudioFrameInputNode frameInputNode;
        private AudioDeviceOutputNode deviceOutputNode;
        private DeviceWatcher audioOutputDeviceWatcher;
        private bool firstDeviceEnumerationComplete = false;
        private bool alreadyDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudioOutputAdapter"/> class.
        /// </summary>
        private DialogAudioOutputAdapter()
        {
            this.audioOutputStreams = new Queue<DialogAudioOutputStream>();
            this.audioOutputStreamsLock = new object();
            this.outputEndedEvent = new AutoResetEvent(false);
            this.graphSemaphore = new SemaphoreSlim(1, 1);
            this.InitializeAudioDeviceWatching();
        }

        /// <summary>
        /// Raised when all enqueued audio has completed playback.
        /// </summary>
        public event Action OutputEnded;

        /// <summary>
        /// Gets a value indicating whether the adapter is currently playing audio.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Asynchronously creates a new instance of the DialogAudioOutputAdapter class
        /// and initializes the underlying audio resources to begin audio output to the
        /// default output device.
        /// </summary>
        /// <returns> A task that completes once the adapter is ready. </returns>
        public static async Task<DialogAudioOutputAdapter> CreateAsync()
        {
            var adapter = new DialogAudioOutputAdapter();
            await adapter.RegenerateAudioGraphAsync();
            return adapter;
        }

        /// <summary>
        /// Cancels any current playback on the adapter and asynchronously begins playback of the
        /// provided Speech SDK dialog output audio.
        /// </summary>
        /// <param name="stream"> The output stream to play. </param>
        /// <returns> A task that completes once all pending output is completed. </returns>
        public Task PlayAudioAsync(DialogAudioOutputStream stream)
        {
            lock (this.audioOutputStreamsLock)
            {
                this.audioOutputStreams.Clear();
            }

            return Task.Run(() =>
            {
                this.EnqueueDialogAudio(stream);
                this.outputEndedEvent.WaitOne();
            });
        }

        /// <summary>
        /// Enqueues a new audio output source for the adapter and begins playback if
        /// it has not already begun.
        /// </summary>
        /// <param name="audioData"> The output stream to enqueue. </param>
        public void EnqueueDialogAudio(DialogAudioOutputStream audioData)
        {
            lock (this.audioOutputStreamsLock)
            {
                this.audioOutputStreams.Enqueue(audioData);
            }

            Task.Run(async () =>
            {
                using (await this.graphSemaphore.AutoReleaseWaitAsync())
                {
                    if (!this.IsPlaying)
                    {
                        this.outputEndedEvent.Reset();
                        this.graph?.Start();
                        this.IsPlaying = true;
                    }
                }
            });
        }

        /// <summary>
        /// Ends Audio Playback and regenerates Audio Graph with corresponding Input and Output Nodes.
        /// </summary>
        /// <returns> A task that completes once playback has stopped. </returns>
        public async Task StopPlaybackAsync()
        {
            using (await this.graphSemaphore.AutoReleaseWaitAsync())
            {
                this.StopOutputInternal();
            }
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
                    this.graph?.Dispose();
                    this.frameInputNode?.Dispose();
                    this.deviceOutputNode?.Dispose();
                    this.graphSemaphore?.Dispose();
                    this.outputEndedEvent?.Dispose();
                }

                this.alreadyDisposed = true;
            }
        }

        private void InitializeAudioDeviceWatching()
        {
            this.audioOutputDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.AudioRender);
            this.audioOutputDeviceWatcher.Added += async (s, e)
                => await this.OnAudioOutputDevicesChangedAsync();
            this.audioOutputDeviceWatcher.Removed += async (s, e)
                => await this.OnAudioOutputDevicesChangedAsync();
            this.audioOutputDeviceWatcher.Updated += async (s, e)
                => await this.OnAudioOutputDevicesChangedAsync();
            this.audioOutputDeviceWatcher.EnumerationCompleted += (s, e)
                => this.firstDeviceEnumerationComplete = true;
            this.audioOutputDeviceWatcher.Start();
        }

        private async Task OnAudioOutputDevicesChangedAsync()
        {
            if (this.firstDeviceEnumerationComplete)
            {
                await this.RegenerateAudioGraphAsync();
            }
        }

        private async Task RegenerateAudioGraphAsync()
        {
            using (await this.graphSemaphore.AutoReleaseWaitAsync())
            {
                // Optimization: don't recreate if the default output device didn't
                // change.
                if (await this.IsDefaultDeviceSameAsGraphAsync())
                {
                    return;
                }

                // End any playback already happening
                this.StopOutputInternal();

                var graphSettings = new AudioGraphSettings(AudioRenderCategory.Speech)
                {
                    QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency,
                };

                var creationResult = await AudioGraph.CreateAsync(graphSettings);
                if (creationResult.Status != AudioGraphCreationStatus.Success)
                {
                    Debug.WriteLine($"Unable to create AudioGraph for output to default device: {creationResult.ExtendedError.ToString()}");
                    return;
                }

                this.graph = creationResult.Graph;

                this.frameInputNode = this.graph.CreateFrameInputNode(AudioEncodingProperties.CreatePcm(16000, 1, 16));
                this.frameInputNode.QuantumStarted += this.FrameInputNode_QuantumStarted;
                this.frameInputNode.AudioFrameCompleted += async (s, e) =>
                {
                    if (this.audioOutputStreams.Count == 0)
                    {
                        await this.StopPlaybackAsync();
                    }
                };
                this.frameInputNode.Start();

                var nodeResult = await this.graph.CreateDeviceOutputNodeAsync();
                if (nodeResult.Status == AudioDeviceNodeCreationStatus.Success)
                {
                    Debug.WriteLine($"Generating audio device output node for default device");
                    this.deviceOutputNode = nodeResult.DeviceOutputNode;
                    this.frameInputNode.AddOutgoingConnection(this.deviceOutputNode);
                    this.deviceOutputNode.Start();
                }
            }
        }

        private async Task<bool> IsDefaultDeviceSameAsGraphAsync()
        {
            if (this.deviceOutputNode == null)
            {
                return false;
            }

            var lastDevice = this.deviceOutputNode.Device;

            var allCurrentDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            DeviceInformation defaultDevice = null;

            for (int i = 0; i < allCurrentDevices.Count; i++)
            {
                var device = allCurrentDevices[i];
                if (device.IsDefault)
                {
                    defaultDevice = device;
                    break;
                }
            }

            return defaultDevice == lastDevice;
        }

        // Internal implementation to stop the audio graph shared along a few code
        // paths.
        // Precondition: graphSemaphore has been acquired (exactly once)
        private void StopOutputInternal()
        {
            this.graph?.Stop();
            if (this.IsPlaying)
            {
                this.IsPlaying = false;
                this.outputEndedEvent.Set();
                this.OutputEnded?.Invoke();
            }
        }

        private void FrameInputNode_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            if (args.RequiredSamples == 0)
            {
                // If no samples are actually requested, no work is needed.
                return;
            }

            var encoding = this.frameInputNode.EncodingProperties;
            var byteRate = encoding.Bitrate / 8;
            var bytesRequested = args.RequiredSamples * byteRate;

            var requestBuffer = new byte[bytesRequested];
            var requestBytesRead = 0;

            lock (this.audioOutputStreamsLock)
            {
                if (this.audioOutputStreams.Count == 0)
                {
                    return;
                }

                var currentStream = this.audioOutputStreams.Peek();
                requestBytesRead = currentStream.Read(requestBuffer, 0, requestBuffer.Length);

                if (requestBytesRead < bytesRequested)
                {
                    // As current sources are exhausted, remove them from the queue.
                    this.audioOutputStreams.Dequeue();
                }
            }

            if (requestBytesRead == 0)
            {
                return;
            }

            using (var frameToPush = new AudioFrame((uint)requestBytesRead))
            {
                this.FrameInputNode_PushFrame(frameToPush, requestBuffer);
            }
        }

        private unsafe void FrameInputNode_PushFrame(AudioFrame frame, byte[] frameData)
        {
            using (var audioBuffer = frame.LockBuffer(AudioBufferAccessMode.Write))

            // TODO: using (Windows.Storage.Streams.Buffer.) -- way to do this without unsafe?
            using (var bufferReference = audioBuffer.CreateReference())
            {
                var bufferAccess = (IMemoryBufferByteAccess)bufferReference;
                bufferAccess.GetBuffer(out byte* unsafeBuffer, out uint unsafeBufferCapacity);

                for (uint i = 0; i < unsafeBufferCapacity; i++)
                {
                    unsafeBuffer[i] = frameData[i];
                }
            }

            this.frameInputNode.AddFrame(frame);
        }
    }
}