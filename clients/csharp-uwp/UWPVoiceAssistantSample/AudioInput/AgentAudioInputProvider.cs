// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UWPVoiceAssistantSample.AudioInput;
    using Windows.Media;
    using Windows.Media.Audio;
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.Media.Render;
    using Windows.Storage.Streams;

    /// <summary>
    /// Audio Processing.
    /// Audio Graph is created, Wave file header and content are processed.
    /// https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Audio.AudioGraph.
    /// </summary>
    public class AgentAudioInputProvider
        : IDialogAudioInputProvider<List<byte>>, IDisposable
    {
        /// <summary>
        /// The default encoding information to use for input.
        /// </summary>
        protected static readonly AudioEncodingProperties DefaultEncoding = AudioEncodingProperties.CreatePcm(16000, 1, 16);
        private readonly ILogProvider logger;
        private AudioDeviceInputNode inputNode;
        private bool dataAvailableInitialized = false;
        private int bytesToSkip;
        private int bytesAlreadySkipped;
        private DebugAudioCapture debugAudioCapture;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentAudioInputProvider"/> class.
        /// </summary>
        public AgentAudioInputProvider()
        {
            this.logger = LogRouter.GetClassLogger();
        }

        /// <summary>
        /// Raised when new audio data is available from the producer.
        /// </summary>
        public event Action<List<byte>> DataAvailable;

        /// <summary>
        /// Raised when audio data is discarded in accordance with initial skip as provided by
        /// StartWithInitialSkipAsync.
        /// </summary>
        public event Action<List<byte>> DataDiscarded;

        /// <summary>
        /// Gets an amount of time experimentally determined to create a reasonable amount of leading audio before
        /// a keyword in an audio stream. Some amount of silence/audio prior to the keyword is necessary for
        /// normal operation.
        /// </summary>
        public static TimeSpan InitialKeywordTrimDuration { get; } = new TimeSpan(0, 0, 0, 0, 2250);

        /// <summary>
        /// Gets or sets a value indicating whether debug audio output to local file capture
        /// is enabled. This is typically associated with the first-stage keyword spotter and
        /// thus may capture a substantial number of unintended interactions, depending on
        /// environment and keyword spotter characteristics.
        /// </summary>
        public bool DebugAudioCaptureFilesEnabled { get; set; }

        /// <summary>
        /// Gets or sets the selected output encoding for audio emitted from this provider.
        /// </summary>
        protected AudioEncodingProperties OutputEncoding { get; set; } = DefaultEncoding;

        /// <summary>
        /// Gets or sets the current agent session associated with incoming input audio.
        /// </summary>
        protected IAgentSessionWrapper AgentSession { get; set; }

        /// <summary>
        /// Gets or sets the AudioGraph currently in use for agent audio capture.
        /// </summary>
        protected AudioGraph InputGraph { get; set; }

        /// <summary>
        /// Gets or sets the output node to which incoming agent audio is emitted.
        /// </summary>
        protected AudioFrameOutputNode OutputNode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audio graph is currently in use.
        /// </summary>
        protected bool GraphRunning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this object has already been disposed.
        /// </summary>
        protected bool AlreadyDisposed { get; set; }

        /// <summary>
        /// Creates an Audio Graph with defaultEncoding property to generate a generic wave file header.
        /// </summary>
        /// <param name="session">Conversation session state.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeFromAgentSessionAsync(IAgentSessionWrapper session)
        {
            await this.InitializeFromAgentAsync(session, DefaultEncoding);
        }

        /// <summary>
        /// Audio Input is encoded and processed to generate the wave file header.
        /// </summary>
        /// <param name="session">Conversation session state.</param>
        /// <param name="properties">AudioEncodingProperties for writing of wave file header.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeFromAgentAsync(IAgentSessionWrapper session, AudioEncodingProperties properties)
        {
            this.AgentSession = session;
            this.OutputEncoding = properties;
            this.GraphRunning = false;

            await this.PerformAudioSetupAsync();
        }

        /// <summary>
        /// Starts an audio producer from now rather than from an agent's cached position its
        /// last session. Useful for push-to-talk and other on-demand scenarios.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeFromNowAsync()
            => await this.InitializeFromAgentSessionAsync(null);

        /// <summary>
        /// Begins the capture of audio for an agent interaction, skipping an initial span of
        /// time at the beginning of the input.
        /// </summary>
        /// <param name="initialAudioToSkip"> The duration of audio at the beginning of input to be skipped. </param>
        /// <returns> A task that completes when audio flow has begun. </returns>
        public Task StartWithInitialSkipAsync(TimeSpan initialAudioToSkip)
        {
            this.bytesToSkip = 0;
            this.bytesAlreadySkipped = 0;

            // If desired, the producer will attempt to skip a portion of the audio at the beginning of the input.
            if (initialAudioToSkip.TotalMilliseconds > 0)
            {
                var bytesPerSecond = this.OutputEncoding.Bitrate / 8;
                this.bytesToSkip = (int)(bytesPerSecond * initialAudioToSkip.TotalSeconds);
            }

            if (this.DebugAudioCaptureFilesEnabled)
            {
                this.debugAudioCapture = new DebugAudioCapture("agentAudio");
            }

            this.InputGraph.Start();
            this.logger.Log(LogMessageLevel.AudioLogs, "Audio Graph Started");
            this.GraphRunning = true;

            return Task.FromResult(0);
        }

        /// <summary>
        /// Begins the capture of audio for an agent interaction.
        /// </summary>
        /// <returns> A task that completes once the audio capture has started. </returns>
        public Task StartAsync() => this.StartWithInitialSkipAsync(TimeSpan.Zero);

        /// <summary>
        /// Stops Audio Graph.
        /// </summary>
        /// <returns> A task that completes once audio input has been stopped. </returns>
        public Task StopAsync()
        {
            if (this.GraphRunning)
            {
                this.InputGraph.Stop();
                this.inputNode.Stop();
                this.inputNode.Dispose();
                this.InputGraph.Dispose();

                this.logger.Log(LogMessageLevel.AudioLogs, "Audio Graph Stopped");
                this.GraphRunning = false;
                this.InputGraph.QuantumStarted -= this.OnQuantumStarted;

                this.debugAudioCapture?.Dispose();
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Called upon every frame quantum while the AudioGraph is running, requesting that data is provided to the
        /// calling AudioGraph.
        /// </summary>
        /// <param name="sender"> The AudioGraph associated with the quantum request. </param>
        /// <param name="unused"> Unused parameter. </param>
        public void OnQuantumStarted(AudioGraph sender, object unused)
        {
            if (!this.GraphRunning)
            {
                return;
            }

            var newData = new List<byte>();
            var discardedData = new List<byte>();

            using (var frame = this.OutputNode.GetFrame())
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            {
                var memBuffer = Windows.Storage.Streams.Buffer.CreateCopyFromMemoryBuffer(buffer);
                memBuffer.Length = buffer.Length;
                using (var reader = DataReader.FromBuffer(memBuffer))
                {
                    // AudioGraph produces 32-bit floating point samples. This typically needs to be
                    // converted to another format for consumption. Most agents use 16khz, 16-bit mono
                    // audio frames
                    if (this.OutputEncoding == DefaultEncoding)
                    {
                        this.OnQuantumStarted_Process16khzMonoPCM(reader, newData, discardedData);
                    }

                    // (More encoding support may be added here as needed)
                    else
                    {
                        throw new FormatException($"Unsupported format for agent audio conversion: {this.OutputEncoding}");
                    }
                }
            }

            if (discardedData.Count > 0)
            {
                this.DataDiscarded?.Invoke(discardedData);
            }

            if (newData.Count > 0)
            {
                this.DataAvailable?.Invoke(newData);
            }
        }

        /// <summary>
        /// Default Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handle the IDisposable pattern, specifically for the managed resources here.
        /// </summary>
        /// <param name="disposing"> whether managed resources are being disposed. </param>
        protected virtual async void Dispose(bool disposing)
        {
            if (!this.AlreadyDisposed)
            {
                if (disposing)
                {
                    await this.StopAsync();
                    this.InputGraph?.Dispose();
                    this.inputNode?.Dispose();
                    this.OutputNode?.Dispose();
                    this.debugAudioCapture?.Dispose();
                }

                this.InputGraph = null;
                this.inputNode = null;
                this.OutputNode = null;
                this.debugAudioCapture = null;

                this.AlreadyDisposed = true;
            }
        }

        /// <summary>
        /// Creates the underlying AudioGraph for input audio and initializes it, including by
        /// subscribing the optional debug output file to data availability.
        /// </summary>
        /// <returns> A task that completes once the underlying AudioGraph is initialized. </returns>
        private async Task PerformAudioSetupAsync()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Speech)
            {
                EncodingProperties = this.OutputEncoding,
            };
            var graphResult = await AudioGraph.CreateAsync(settings);
            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                var message = $"Failed to initialize AudioGraph with creation status: {graphResult.Status.ToString()}";
                throw new InvalidOperationException(message, graphResult.ExtendedError);
            }

            this.InputGraph = graphResult.Graph;

            this.logger.Log(LogMessageLevel.AudioLogs, $"Audio graph created: {graphResult.Status}");

            if (this.AgentSession != null)
            {
                this.logger.Log(LogMessageLevel.AudioLogs, $"{Environment.TickCount} Initializing audio from session");
                this.inputNode = await this.AgentSession.CreateAudioDeviceInputNodeAsync(this.InputGraph);
            }
            else
            {
                this.logger.Log(LogMessageLevel.AudioLogs, $"{Environment.TickCount} Initializing audio from real-time input");
                var nodeResult = await this.InputGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech);
                if (nodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    throw new InvalidOperationException($"Cannot make a real-time device input node.", nodeResult.ExtendedError);
                }

                this.inputNode = nodeResult.DeviceInputNode;
            }

            this.OutputNode = this.InputGraph.CreateFrameOutputNode();
            this.inputNode.AddOutgoingConnection(this.OutputNode);
            this.InputGraph.QuantumStarted += this.OnQuantumStarted;

            if (!this.dataAvailableInitialized)
            {
                this.dataAvailableInitialized = true;
                this.DataAvailable += (bytes) =>
                {
                    this.debugAudioCapture?.Write(bytes.ToArray());
                };
            }
        }

        private void OnQuantumStarted_Process16khzMonoPCM(DataReader reader, List<byte> newData, List<byte> discardedData)
        {
            reader.ByteOrder = ByteOrder.LittleEndian;

            while (reader.UnconsumedBufferLength > 0)
            {
                // AudioGraph data is retrieved in 32-bit floats; clamp this for conversion to 16-bit PCM
                var nextFloatValue = reader.ReadSingle();
                nextFloatValue = nextFloatValue > 1.0f ? 1.0f : nextFloatValue;
                nextFloatValue = nextFloatValue < -1.0f ? -1.0f : nextFloatValue;

                byte[] bytes;
                switch (this.OutputEncoding.BitsPerSample)
                {
                    case 16:
                        bytes = BitConverter.GetBytes((short)(nextFloatValue * short.MaxValue));
                        break;
                    case 32:
                        bytes = BitConverter.GetBytes((int)(nextFloatValue * int.MaxValue));
                        break;
                    default:
                        // Only 16 and 32 are currently supported
                        throw new FormatException();
                }

                foreach (var convertedByte in bytes)
                {
                    if (this.bytesAlreadySkipped < this.bytesToSkip)
                    {
                        discardedData.Add(convertedByte);
                        this.bytesAlreadySkipped++;
                    }
                    else
                    {
                        newData.Add(convertedByte);
                    }
                }
            }
        }
    }
}