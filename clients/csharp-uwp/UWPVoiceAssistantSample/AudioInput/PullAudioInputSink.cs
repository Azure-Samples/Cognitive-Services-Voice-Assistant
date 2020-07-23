// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioInput
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Windows.Media.MediaProperties;

    /// <summary>
    /// Helper class that encapsulates state management for a reusable PullAudioInputStream object that may use a variety
    /// of underlying sources.
    /// </summary>
    public class PullAudioInputSink : PullAudioInputStreamCallback
    {
        private readonly List<byte> pushDataBuffer = new List<byte>();
        private readonly object dataSourceLock = new object();
        private PullAudioDataSource dataSource;

        /// <summary>
        /// Raised upon the first read that crosses the current BookmarkPosition, as counted since last reset.
        /// </summary>
        public event Action<TimeSpan> BookmarkReached;

        /// <summary>
        /// Gets the duration of audio pulled from this sink since its last reset operation.
        /// </summary>
        public TimeSpan AudioReadSinceReset { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the next position when BookmarkReached will be fired if a read causes BytesReadSinceReset to cross
        /// the position.
        /// </summary>
        public TimeSpan BookmarkPosition { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets a friendly label to associate with this input sink.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the active data source to be used for subsequent reads from this input sink.
        /// </summary>
        public PullAudioDataSource DataSource
        {
            get => this.dataSource;
            set
            {
                if (this.dataSource != null)
                {
                    switch (this.dataSource.BaseSource)
                    {
                        case AudioDataStream resultStream:
                            resultStream.DetachInput();
                            resultStream.Dispose();
                            break;
                        default:
                            break;
                    }
                }

                this.dataSource = value;
            }
        }

        /// <summary>
        /// Adds the provided data to the buffer to be consumed when an appropriate source type is set.
        /// </summary>
        /// <param name="bytes"> The bytes to enqueue to the buffer. </param>
        public void PushData(IEnumerable<byte> bytes)
        {
            lock (this.dataSourceLock)
            {
                this.pushDataBuffer.AddRange(bytes);
            }
        }

        /// <summary>
        /// Resets the source and data consumption count of this input sink.
        /// </summary>
        public void Reset()
        {
            this.DataSource = null;
            lock (this.dataSourceLock)
            {
                this.pushDataBuffer.Clear();
            }

            this.AudioReadSinceReset = TimeSpan.Zero;
        }

        /// <summary>
        /// Implemented for PullAudioInputStreamCallback. Fills the provided buffer from the currently selected data
        /// source and optionally pads incomplete base reads with zeroes to prevent a stream termination.
        /// </summary>
        /// <param name="dataBuffer"> The buffer to fill with data. </param>
        /// <param name="size"> The size of the buffer. </param>
        /// <returns> The final number of bytes populated in dataBuffer. </returns>
        public override int Read(byte[] dataBuffer, uint size)
        {
            Contract.Requires(dataBuffer != null);

            var baseSource = this.DataSource?.BaseSource;

            var bytesRead =
                this.DataSource == PullAudioDataSource.PushedData ? this.ReadFromBuffer(dataBuffer)
                : baseSource is AudioDataStream ? this.ReadFromKeyword(dataBuffer)
                : baseSource is null ? 0
                : throw new ArgumentException("Unsupported PullAudioDataSource");

            if (this.DataSource != null && this.DataSource.PadWithZeroes)
            {
                Array.Fill<byte>(dataBuffer, 0, bytesRead, dataBuffer.Length - bytesRead);
                bytesRead = dataBuffer.Length;
            }

            if (this.DataSource?.AudioFormat is AudioEncodingProperties audioFormat)
            {
                var priorTotalReadDuration = this.AudioReadSinceReset;
                this.AudioReadSinceReset += TimeSpan.FromSeconds(8.0f * bytesRead / audioFormat.Bitrate);

                if (priorTotalReadDuration < this.BookmarkPosition
                    && this.AudioReadSinceReset >= this.BookmarkPosition)
                {
                    this.BookmarkReached?.Invoke(this.AudioReadSinceReset);
                }
            }

            return bytesRead;
        }

        private int ReadFromKeyword(byte[] buffer)
        {
            for (var doneWaiting = false; !doneWaiting;)
            {
                lock (this.dataSourceLock)
                {
                    doneWaiting = !(this.DataSource?.BaseSource is AudioDataStream keywordAudioSource)
                        || keywordAudioSource.GetStatus() != StreamStatus.PartialData
                        || keywordAudioSource.CanReadData((uint)buffer.Length);
                }

                if (!doneWaiting)
                {
                    Thread.Sleep(50);
                }
            }

            lock (this.dataSourceLock)
            {
                if (!(this.DataSource?.BaseSource is AudioDataStream keywordAudioSource))
                {
                    return 0;
                }

                var bytesToRead = buffer.Length;

                while (!keywordAudioSource.CanReadData((uint)bytesToRead))
                {
                    bytesToRead--;
                }

                var bufferToUse = bytesToRead == buffer.Length ? buffer : new byte[bytesToRead];
                var result = keywordAudioSource.ReadData(bufferToUse);

                if (bufferToUse != buffer)
                {
                    Array.Copy(bufferToUse, buffer, bufferToUse.Length);
                }

                return (int)result;
            }
        }

        private int ReadFromBuffer(byte[] readBuffer)
        {
            while (true)
            {
                lock (this.dataSourceLock)
                {
                    if (this.pushDataBuffer.Count >= readBuffer.Length || this.DataSource != PullAudioDataSource.PushedData)
                    {
                        var bytesAvailable = (int)Math.Min(readBuffer.Length, this.pushDataBuffer.Count);
                        this.pushDataBuffer.CopyTo(0, readBuffer, 0, bytesAvailable);
                        this.pushDataBuffer.RemoveRange(0, bytesAvailable);
                        return bytesAvailable;
                    }
                }

                Thread.Sleep(50);
            }
        }
    }
}
