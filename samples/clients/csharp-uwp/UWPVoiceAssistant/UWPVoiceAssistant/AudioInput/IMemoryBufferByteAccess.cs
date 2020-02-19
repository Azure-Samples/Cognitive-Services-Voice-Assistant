// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Needed for direct writes to frame input
    /// See https://docs.microsoft.com/windows/uwp/audio-video-camera/audio-graphs#audio-frame-input-node for more information.
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        /// <summary>
        /// Gets the buffer and capacity for the In-Memory Stream to write to the Audio Graph.
        /// </summary>
        /// <param name="buffer">Number of Bytes in the Audio Stream.</param>
        /// <param name="capacity">Capacity of Bytes present in the Audio Stream.</param>
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
