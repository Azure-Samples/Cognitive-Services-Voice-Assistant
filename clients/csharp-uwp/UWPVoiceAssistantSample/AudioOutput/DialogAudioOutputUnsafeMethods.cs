// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioOutput
{
    using System;
    using System.Diagnostics.Contracts;
    using Windows.Media;
    
    /// <summary>
    /// Implementation of unsafe helper methods needed for the direct manipulation of input AudioGraph data.
    /// </summary>
    public static class DialogAudioOutputUnsafeMethods
    {
        /// <summary>
        /// Creates a new AudioFrame from the specified subset of input bytes.
        /// </summary>
        /// <param name="frameDataBuffer"> The bytes to use as the data source for the new AudioFrame. </param>
        /// <param name="length"> The number of bytes, from the beginning of the array, to use. </param>
        /// <returns> A new AudioFrame with the specified data. The caller is responsible for Disposing. </returns>
        public static unsafe AudioFrame CreateFrameFromBytes(byte[] frameDataBuffer, int length)
        {
            Contract.Requires(frameDataBuffer != null);

            if (length > frameDataBuffer.Length || length <= 0)
            {
                throw new ArgumentException($"Cannot create an AudioFrame of size {length}. Valid: 1 to {frameDataBuffer.Length}");
            }

            var resultFrame = new AudioFrame((uint)length);

            using (var audioBuffer = resultFrame.LockBuffer(AudioBufferAccessMode.Write))
            using (var bufferReference = audioBuffer.CreateReference())
            {
                var bufferAccess = (IMemoryBufferByteAccess)bufferReference;
                bufferAccess.GetBuffer(out byte* unsafeBuffer, out uint unsafeBufferCapacity);

                for (uint i = 0; i < unsafeBufferCapacity; i++)
                {
                    unsafeBuffer[i] = frameDataBuffer[i];
                }
            }

            return resultFrame;
        }
    }
}
