// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Windows.Foundation;
    using Windows.Media.MediaProperties;
    using Windows.Security.Cryptography;
    using Windows.Storage.Streams;

    /// <summary>
    /// Abstract representation of the subset of System.IO.Stream and Windows.Storage.Streams.IRandomAccessStream
    /// functionality used for dialog audio output.
    /// </summary>
    public abstract class DialogAudioOutputStream
        : Stream, IRandomAccessStream
    {
        private const uint DefaultAdvertisedSize = 1024 * 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudioOutputStream"/> class.
        /// </summary>
        protected DialogAudioOutputStream()
        {
            this.BufferStream = new InMemoryRandomAccessStream();
        }

        /// <summary>
        /// Gets or sets the AudioEncodingProperties associated with this output stream.
        /// </summary>
        public AudioEncodingProperties Encoding { get; protected set; }

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
        ulong IRandomAccessStream.Position => this.BufferStream.Position;

        /// <summary>
        /// Gets or sets a value not implemented for DialogAudioOutputStream.
        /// </summary>
        public ulong Size
        {
            get => this.ReachedEndOfStream ? this.BufferStream.Size : DefaultAdvertisedSize;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a backing in-memory stream used to facilitate random access use with the restricted nature of
        /// non-fixed-length service-based output streams.
        /// </summary>
        protected IRandomAccessStream BufferStream { get; private set; }

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
        void IRandomAccessStream.Seek(ulong position) => this.BufferStream.Seek(position);

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
            this.EnsureBufferStreamCapacity((uint)this.BufferStream.Position + count);
            return this.BufferStream.ReadAsync(buffer, count, options);
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

        private void EnsureBufferStreamCapacity(uint newPosition)
        {
            if (this.BufferStream.Size < newPosition)
            {
                var missingBytes = newPosition - this.BufferStream.Size;
                var buffer = new byte[missingBytes];
                var sourceBytesRead = this.Read(buffer, 0, buffer.Length);
                this.ReachedEndOfStream = sourceBytesRead < buffer.Length;
                this.AppendData(buffer, sourceBytesRead);
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

            var originalPosition = this.BufferStream.Position;
            this.BufferStream.Seek(this.BufferStream.Size);
            this.BufferStream.AsStreamForWrite().Write(bufferToUse, 0, bufferToUse.Length);
            this.BufferStream.Seek(originalPosition);
        }
    }
}
