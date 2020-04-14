// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Foundation;
    using Windows.Media.MediaProperties;
    using Windows.Security.Cryptography;
    using Windows.Storage.Streams;

    /// <summary>
    /// Abstract representation of the subset of System.IO.Stream functionality used for dialog
    /// audio output.
    /// </summary>
    public abstract class DialogAudioOutputStream
        : Stream, IRandomAccessStream
    {
        private static readonly uint DefaultAdvertisedSize = 1024 * 4;

        protected IRandomAccessStream bufferStream;
        protected bool reachedEndOfStream = false;

        protected DialogAudioOutputStream()
        {
            this.bufferStream = new InMemoryRandomAccessStream();
        }

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

        IInputStream IRandomAccessStream.GetInputStreamAt(ulong position) => throw new NotImplementedException();

        IOutputStream IRandomAccessStream.GetOutputStreamAt(ulong position) => throw new NotImplementedException();

        void IRandomAccessStream.Seek(ulong position) => this.bufferStream.Seek(position);

        IRandomAccessStream IRandomAccessStream.CloneStream() => this;

        ulong IRandomAccessStream.Position => this.bufferStream.Position;

        ulong IRandomAccessStream.Size
        {
            get => this.reachedEndOfStream ? this.bufferStream.Size : DefaultAdvertisedSize;
            set => throw new NotImplementedException(); 
        }

        IAsyncOperationWithProgress<IBuffer, uint> IInputStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            this.EnsureBufferStreamCapacity((uint)this.bufferStream.Position + count);
            return this.bufferStream.ReadAsync(buffer, count, options);
        }

        IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer buffer) => throw new NotImplementedException();


        IAsyncOperation<bool> IOutputStream.FlushAsync() => throw new NotImplementedException();

        protected bool EnsureBufferStreamCapacity(uint newPosition)
        {
            var canReachPosition = true;

            if (this.bufferStream.Size < newPosition)
            {
                var missingBytes = newPosition - this.bufferStream.Size;
                var buffer = new byte[missingBytes];
                var sourceBytesRead = this.Read(buffer, 0, buffer.Length);
                canReachPosition = sourceBytesRead == buffer.Length;
                this.AppendData(buffer, sourceBytesRead);
            }

            return canReachPosition;
        }

        protected void AppendData(byte[] data, int count)
        {
            var bufferToUse = data;
            if (count < data.Length)
            {
                bufferToUse = new byte[count];
                Array.Copy(data, 0, bufferToUse, 0, count);
            }
            var writeBuffer = CryptographicBuffer.CreateFromByteArray(bufferToUse);

            var originalPosition = this.bufferStream.Position;
            this.bufferStream.Seek(this.bufferStream.Size);

            Debugger.Break();
            this.bufferStream.AsStreamForWrite().Write(bufferToUse, 0, bufferToUse.Length);
            // var writeResult = await this.bufferStream.WriteAsync(writeBuffer);


            this.bufferStream.Seek(originalPosition);
        }
    }
}
