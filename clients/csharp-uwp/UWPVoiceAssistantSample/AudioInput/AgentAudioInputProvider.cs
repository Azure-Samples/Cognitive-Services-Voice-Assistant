// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Media;
    using Windows.Media.Audio;
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.Media.Render;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// Audio Processing.
    /// Audio Graph is created, Wave file header and content are processed.
    /// https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Audio.AudioGraph.
    /// </summary>
    public class AgentAudioInputProvider
        : IDialogAudioInputProvider<List<byte>>, IDisposable
    {
        protected static readonly AudioEncodingProperties DefaultEncoding = AudioEncodingProperties.CreatePcm(16000, 1, 16);
        protected AudioEncodingProperties outputEncoding;
        protected IAgentSessionWrapper agentSession;
        protected AudioGraph inputGraph;
        protected AudioDeviceInputNode inputNode;
        protected AudioFrameOutputNode outputNode;
        protected bool graphRunning;
        protected bool disposed;
        protected SemaphoreSlim debugAudioOutputFileSemaphore;
        protected Stream debugAudioOutputFileStream;
        private bool dataAvailableInitialized = false;
        private int bytesToSkip;
        private int bytesAlreadySkipped;
        private ILogProvider logger;

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
        public static TimeSpan InitialKeywordTrimDuration { get; } = new TimeSpan(0, 0, 0, 0, 2000);

        /// <summary>
        /// Gets or sets a value indicating whether debug audio output to local file capture
        /// is enabled. This is typically associated with the first-stage keyword spotter and
        /// thus may capture a substantial number of unintended interactions, depending on
        /// environment and keyword spotter characteristics.
        /// </summary>
        public bool DebugAudioCaptureFilesEnabled { get; set; }

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
            this.agentSession = session;
            this.outputEncoding = properties;
            this.graphRunning = false;
            this.debugAudioOutputFileSemaphore = new SemaphoreSlim(1, 1);

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
        public async Task StartWithInitialSkipAsync(TimeSpan initialAudioToSkip)
        {
            if (this.DebugAudioCaptureFilesEnabled)
            {
                await this.OpenDebugAudioOutputFileAsync();
            }

            this.bytesToSkip = 0;
            this.bytesAlreadySkipped = 0;

            // If desired, the producer will attempt to skip a portion of the audio at the beginning of the input.
            if (initialAudioToSkip.TotalMilliseconds > 0)
            {
                var bytesPerSecond = this.outputEncoding.Bitrate / 8;
                this.bytesToSkip = (int)(bytesPerSecond * initialAudioToSkip.TotalSeconds);
            }

            this.inputGraph.Start();
            this.logger.Log(LogMessageLevel.AudioLogs, "Audio Graph Started");
            this.graphRunning = true;
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
        public async Task StopAsync()
        {
            if (this.graphRunning)
            {
                await this.FinishDebugAudioDumpIfNeededAsync();

                this.inputGraph.QuantumStarted -= this.OnQuantumStarted;
                this.outputNode?.Stop();
                this.inputNode?.Stop();
                this.inputGraph?.Stop();
                this.Dispose(true);

                this.logger.Log(LogMessageLevel.AudioLogs, "Audio Graph Stopped");
                this.graphRunning = false;
            }
        }

        /// <summary>
        /// Handle the IDisposable pattern, specifically for the managed resources here.
        /// </summary>
        /// <param name="disposing"> whether managed resources are being disposed. </param>
        public async void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.inputNode?.Dispose();
                    this.outputNode?.Dispose();
                    this.inputGraph?.Dispose();
                    this.debugAudioOutputFileSemaphore?.Dispose();
                    this.debugAudioOutputFileStream?.Dispose();
                }

                this.inputGraph = null;
                this.inputNode = null;
                this.outputNode = null;
                this.debugAudioOutputFileSemaphore = null;
                this.debugAudioOutputFileStream = null;

                this.disposed = true;
            }
        }

        /// <summary>
        /// Called upon every frame quantum while the AudioGraph is running, requesting that data is provided to the
        /// calling AudioGraph.
        /// </summary>
        /// <param name="sender"> The AudioGraph associated with the quantum request. </param>
        public void OnQuantumStarted(AudioGraph sender, object _)
        {
            var newData = new List<byte>();
            var discardedData = new List<byte>();

            using (var frame = this.outputNode.GetFrame())
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            {
                var memBuffer = Windows.Storage.Streams.Buffer.CreateCopyFromMemoryBuffer(buffer);
                memBuffer.Length = buffer.Length;
                using (var reader = DataReader.FromBuffer(memBuffer))
                {
                    // AudioGraph produces 32-bit floating point samples. This typically needs to be
                    // converted to another format for consumption. Most agents use 16khz, 16-bit mono
                    // audio frames
                    if (this.outputEncoding == DefaultEncoding)
                    {
                        this.OnQuantumStarted_Process16khzMonoPCM(reader, newData, discardedData);
                    }

                    // (More encoding support may be added here as needed)
                    else
                    {
                        throw new FormatException($"Unsupported format for agent audio conversion: {this.outputEncoding.ToString()}");
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
        /// Creates the underlying AudioGraph for input audio and initializes it, including by
        /// subscribing the optional debug output file to data availability.
        /// </summary>
        /// <returns> A task that completes once the underlying AudioGraph is initialized. </returns>
        private async Task PerformAudioSetupAsync()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Speech)
            {
                EncodingProperties = this.outputEncoding,
            };
            var graphResult = await AudioGraph.CreateAsync(settings);
            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                var message = $"Failed to initialize AudioGraph with creation status: {graphResult.Status.ToString()}";
                throw new InvalidOperationException(message, graphResult.ExtendedError);
            }

            this.inputGraph = graphResult.Graph;

            this.logger.Log(LogMessageLevel.AudioLogs, $"Audio graph created: {graphResult.Status}");

            if (this.agentSession != null)
            {
                this.logger.Log(LogMessageLevel.AudioLogs, $"{Environment.TickCount} Initializing audio from session");
                this.inputNode = await this.agentSession.CreateAudioDeviceInputNodeAsync(this.inputGraph);
            }
            else
            {
                this.logger.Log(LogMessageLevel.AudioLogs, $"{Environment.TickCount} Initializing audio from real-time input");
                var nodeResult = await this.inputGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech);
                if (nodeResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    throw new InvalidOperationException($"Cannot make a real-time device input node.", nodeResult.ExtendedError);
                }

                this.inputNode = nodeResult.DeviceInputNode;
            }

            this.outputNode = this.inputGraph.CreateFrameOutputNode();
            this.inputNode.AddOutgoingConnection(this.outputNode);
            this.inputGraph.QuantumStarted += this.OnQuantumStarted;
            this.disposed = false;

            if (!this.dataAvailableInitialized)
            {
                this.dataAvailableInitialized = true;
                this.DataAvailable += async (bytes) =>
                {
                    using (await this.debugAudioOutputFileSemaphore.AutoReleaseWaitAsync())
                    {
                        this.debugAudioOutputFileStream?.Write(bytes.ToArray(), 0, bytes.Count);
                    }
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
                switch (this.outputEncoding.BitsPerSample)
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

        /// <summary>
        /// Creates a debug audio output file that will capture all audio provided to the dialog
        /// backend. This is typically audio that has passed 1st-stage, high-accept keyword
        /// spotting but has not yet been verified against subsequent stages, so the number of
        /// files captured can grow quickly depending on the accuracy characteristics of the
        /// 1st-stage keyword spotter and the environment being listened to.
        /// </summary>
        /// <returns> A task that completes once the file is created and ready for writing. </returns>
        private async Task OpenDebugAudioOutputFileAsync()
        {
            using (await this.debugAudioOutputFileSemaphore.AutoReleaseWaitAsync())
            {
                if (this.debugAudioOutputFileStream == null)
                {
                    this.debugAudioOutputFileStream =
                        await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(
                            $"agentaudio_{DateTime.Now.ToString("yyyyMMdd_HHmmss", null)}.wav", CreationCollisionOption.ReplaceExisting).ConfigureAwait(true);
                    this.debugAudioOutputFileStream.Write(new byte[44], 0, 44);
                }
            }
        }

        /// <summary>
        /// Writes a 44-byte PCM header to the head of the provided stream.The data will overwrite at
        /// this position (not insert), so it's important that the audio data be written *after* the
        /// 44th byte position in the stream. Suggested usage:
        ///     Stream s = MakeNewStream();
        ///     s.Write(new byte[44]);
        ///     PopulateStream(s);
        ///     producer.WriteWavHeaderToStream(s);.
        /// </summary>
        /// <param name="stream">In-Memory Audio Stream.</param>
        private void WriteDebugWavHeader(Stream stream)
        {
            this.logger.Log(LogMessageLevel.AudioLogs, "Beginning writing of wav file header");
            Contract.Requires(stream != null);

            ushort channels = (ushort)this.outputEncoding.ChannelCount;
            int sampleRate = (int)this.outputEncoding.SampleRate;
            ushort bytesPerSample = (ushort)(this.outputEncoding.BitsPerSample / 8);

            stream.Position = 0;

            // RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            stream.Write(BitConverter.GetBytes((int)stream.Length - 8), 0, 4);

            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);

            // Channels.
            stream.Write(BitConverter.GetBytes(channels), 0, 2);

            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            stream.Write(BitConverter.GetBytes(sampleRate * channels * bytesPerSample), 0, 4);

            // Block align.
            stream.Write(BitConverter.GetBytes((ushort)channels * bytesPerSample), 0, 2);

            // Bits per sample.
            stream.Write(BitConverter.GetBytes((ushort)(bytesPerSample * 8)), 0, 2);

            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes((int)(stream.Length - 44)), 0, 4);

            this.logger.Log(LogMessageLevel.AudioLogs, "Wav file header written");
        }

        private async Task FinishDebugAudioDumpIfNeededAsync()
        {
            await this.debugAudioOutputFileSemaphore.WaitAsync();
            try
            {
                if (this.debugAudioOutputFileStream != null)
                {
                    const int bytesPerMillisecond = 32;
                    var dataLength = this.debugAudioOutputFileStream.Length - 44;
                    var dataDuration = 1.0 * dataLength / bytesPerMillisecond;
                    this.logger.Log(LogMessageLevel.AudioLogs, $"Completing write of microphone audio file. Length: {(int)dataDuration}ms");
                    this.WriteDebugWavHeader(this.debugAudioOutputFileStream);
                    this.debugAudioOutputFileStream.Flush();
                    this.debugAudioOutputFileStream.Close();
                    this.debugAudioOutputFileStream.Dispose();
                    this.debugAudioOutputFileStream = null;
                }
            }
            finally
            {
                this.debugAudioOutputFileSemaphore.Release();
            }
        }
    }
}