// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading;
    using UWPVoiceAssistantSample.AudioCommon;
    using Windows.Foundation;
    using Windows.Storage.Streams;

    /// <summary>
    /// Abstract representation of the subset of System.IO.Stream and Windows.Storage.Streams.IRandomAccessStream
    /// functionality used for dialog audio output.
    /// </summary>
    public abstract class DialogAudioOutputStream
        : Stream, IRandomAccessStream
    {
        private const uint DefaultAdvertisedSize = 1024 * 4;
        private readonly SemaphoreSlim bufferStreamSemaphore = new SemaphoreSlim(1, 1);
        private readonly Stream bufferStream;
        private readonly ILogProvider log = LogRouter.GetClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudioOutputStream"/> class.
        /// </summary>
        protected DialogAudioOutputStream()
        {
            this.bufferStream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudioOutputStream"/> class.
        /// </summary>
        /// <param name="format"> The audio format of the underlying data. </param>
        protected DialogAudioOutputStream(DialogAudio format)
            : this()
        {
            Contract.Requires(format != null);
            Contract.Requires(this.bufferStream.Position == 0);

            this.Format = format;
            var encoding = this.Format.Encoding;

            // For PCM streams, we will assume that there's no header information available and write what's needed
            // for playback.
            if (encoding.Subtype == "WAV" || encoding.Subtype == "PCM")
            {
                WaveHeader.WriteWaveFormatHeaderToStream(this.bufferStream, encoding, WaveHeaderLengthOption.UseMaximumLength);
                this.bufferStream.Seek(0, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Gets or sets the AudioEncodingProperties associated with this output stream.
        /// </summary>
        public DialogAudio Format { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the stream can be read from. Always true.
        /// </summary>
        public override bool CanRead { get => true; }

        /// <summary>
        /// Gets a value indicating whether the stream can seek. Always true.
        /// </summary>
        public override bool CanSeek { get => true; }

        /// <summary>
        /// Gets a value indicating whether the stream can be written to. Always false.
        /// </summary>
        public override bool CanWrite { get => false; }

        /// <summary>
        /// Gets the length of the stream.
        /// Not supported for DialogAudioOutputStream.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Gets or sets the position of the stream.
        /// Not supported for DialogAudioOutputStream.
        /// </summary>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the current seek position of the underlying backing buffer stream.
        /// </summary>
        ulong IRandomAccessStream.Position => (ulong)this.bufferStream.Position;

        /// <summary>
        /// Gets or sets a value not implemented for DialogAudioOutputStream.
        /// </summary>
        public ulong Size
        {
            get => this.ReachedEndOfStream ? (ulong)this.bufferStream.Length : DefaultAdvertisedSize;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether the end of available data has been reached and the buffer stream
        /// now has the entirety of streamed audio for the prompt.
        /// </summary>
        protected bool ReachedEndOfStream { get; private set; } = false;

        /// <summary>
        /// Not supported for DialogAudioOutputStream.
        /// </summary>
        public override void Flush() => throw new NotSupportedException();

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="value"> The provided length. Unused. </param>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="offset"> Offset to seek to. Unused. </param>
        /// <param name="origin"> Origin from which to seek. Unused. </param>
        /// <returns> Unused. </returns>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        /// <summary>
        /// Reads data from the underlying buffered stream into the provided buffer. Attempts to fill as much data
        /// as needed to fulfill the request from the underlying data source and blocks until this is successful or
        /// the source is exhausted.
        /// </summary>
        /// <param name="buffer"> The buffer into which the desired data should be written. </param>
        /// <param name="offset"> The offset in the array at which to begin the read. </param>
        /// <param name="count"> The number of bytes, beginning at offset, to read. </param>
        /// <returns>
        ///     The number of bytes actually read. Fewer than requested indicates the stream is exhausted.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            using (this.bufferStreamSemaphore.AutoReleaseWait())
            {
                this.EnsureDataInBufferUpToPosition((uint)this.bufferStream.Position + (uint)count);
                var bytesActuallyRead = this.bufferStream.Read(buffer, offset, count);
                return bytesActuallyRead;
            }
        }

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="buffer"> Buffer to write to. Unused. </param>
        /// <param name="offset"> Offset to write to. Unused. </param>
        /// <param name="count"> Number of bytes to write. Unused. </param>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="position"> The position from which to read in the resulting view of the stream. Not used.
        /// </param>
        /// <returns> An input stream view of the underlying stream at the specified position. Unused. </returns>
        IInputStream IRandomAccessStream.GetInputStreamAt(ulong position) => throw new NotImplementedException();

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="position"> The position from which to write in the output stream view. Not used. </param>
        /// <returns> An IOutputStream view of the stream at the provided position. Not used. </returns>
        IOutputStream IRandomAccessStream.GetOutputStreamAt(ulong position) => throw new NotImplementedException();

        /// <summary>
        /// Sets the seek position for subsequent read and write operations to the specified position.
        /// </summary>
        /// <param name="position"> The position to seek to relative to the start of the stream. </param>
        void IRandomAccessStream.Seek(ulong position) => this.bufferStream.Seek((long)position, SeekOrigin.Begin);

        /// <summary>
        /// Returns the identity instance of this stream. Does not copy the stream.
        /// </summary>
        /// <returns> This object. </returns>
        public IRandomAccessStream CloneStream() => this;

        /// <summary>
        /// Ensures sufficient capacity in the backing buffer stream, reading from the underlying data source to
        /// expand it, and then executes the requested read on the buffer stream.
        /// </summary>
        /// <param name="buffer"> The buffer with which to fill from the read request. </param>
        /// <param name="count"> The number of bytes to at most fill the buffer with. </param>
        /// <param name="options"> Extra options to allow, for example, a partial read to complete. </param>
        /// <returns> An IAsyncOperationWithProgress for the underlying read operation. </returns>
        IAsyncOperationWithProgress<IBuffer, uint> IInputStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="buffer"> The buffer to write. Not used. </param>
        /// <returns> An IAsyncOperationWithProgress for the write operation. Not used. </returns>
        IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer buffer) => throw new NotImplementedException();

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <returns> An IAsyncOperation for the underlying Flush operation. Not used. </returns>
        IAsyncOperation<bool> IOutputStream.FlushAsync() => throw new NotImplementedException();

        /// <summary>
        /// Abstract definition of the read call used by concrete descendants to draw real data from a dialog audio
        /// source. Used to fill the internal buffer stream and fulfill read requests.
        /// </summary>
        /// <param name="buffer"> The buffer into which the data will be written. </param>
        /// <param name="count"> The number of bytes to read into this array. </param>
        /// <returns> The number of bytes actually read. Fewer than requested indicates the end of stream. </returns>
        protected abstract uint ReadFromRealDialogSource(byte[] buffer, uint count);

        /// <summary>
        /// Ensures that the internal buffer stream has as much data as needed or possible to fulfill an incoming
        /// request operating to a specified position. Attempts to draw data from the incoming data source to fill
        /// the buffer.
        /// </summary>
        /// <param name="newPosition">
        /// The position to which the buffer stream will be filled, beginning at the end of the stream.
        /// </param>
        private void EnsureDataInBufferUpToPosition(uint newPosition)
        {
            if (this.bufferStream.Length < newPosition)
            {
                var missingBytes = newPosition - this.bufferStream.Length;
                var buffer = new byte[missingBytes];
                var sourceBytesRead = this.ReadFromRealDialogSource(buffer, (uint)buffer.Length);
                if (sourceBytesRead < buffer.Length)
                {
                    this.ReachedEndOfStream = true;
                    this.log.Log(LogMessageLevel.AudioLogs, $"End of stream, total of {this.bufferStream.Length} bytes");
                }

                this.ReachedEndOfStream = sourceBytesRead < buffer.Length;
                this.AppendData(buffer, (int)sourceBytesRead);
            }
        }

        /// <summary>
        /// Adds the provided data to the end of the backing stream, preserving and restoring the current seek position.
        /// </summary>
        /// <param name="data"> The data to add to the end of the backing buffer stream. </param>
        /// <param name="count"> The number of bytes from the provided data to append. </param>
        private void AppendData(byte[] data, int count)
        {
            var bufferToUse = data;
            if (count < data.Length)
            {
                bufferToUse = new byte[count];
                Array.Copy(data, 0, bufferToUse, 0, count);
            }

            var originalPosition = this.bufferStream.Position;
            this.bufferStream.Seek(this.bufferStream.Length, SeekOrigin.Begin);
            this.bufferStream.Write(bufferToUse, 0, bufferToUse.Length);
            this.bufferStream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }
}
