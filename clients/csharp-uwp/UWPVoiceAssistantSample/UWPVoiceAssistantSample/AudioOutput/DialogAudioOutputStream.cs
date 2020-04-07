// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.IO;

    /// <summary>
    /// Abstract representation of the subset of System.IO.Stream functionality used for dialog
    /// audio output.
    /// </summary>
    public abstract class DialogAudioOutputStream
        : Stream
    {
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
        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotImplementedException();

        /// <summary>
        /// Not implemented for DialogAudioOutputStream.
        /// </summary>
        /// <param name="buffer"> Buffer to write to. Unused. </param>
        /// <param name="offset"> Offset to write to. Unused. </param>
        /// <param name="count"> Number of bytes to write. Unused. </param>
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotImplementedException();
    }
}
