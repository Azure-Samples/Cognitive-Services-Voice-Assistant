// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Diagnostics.Contracts;
    using Microsoft.CognitiveServices.Speech.Audio;

    /// <summary>
    /// A derived specialization of the DialogAudioOutputStream abstract class that provides the
    /// required DialogAudioOutputStream functionality using a PullAudioOutputStream as provided
    /// by the Speech SDK and used by Direct Line Speech.
    /// </summary>
    public class DirectLineSpeechAudioOutputStream
        : DialogAudioOutputStream
    {
        private readonly PullAudioOutputStream audioSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLineSpeechAudioOutputStream"/> class.
        /// </summary>
        /// <param name="audioSource"> The PullAudioOutputStream that should be read from. </param>
        public DirectLineSpeechAudioOutputStream(PullAudioOutputStream audioSource)
        {
            this.audioSource = audioSource;
        }

        /// <summary>
        /// Reads audio from the underlying PullAudioOutputStream into the provided buffer.
        /// </summary>
        /// <param name="buffer"> The buffer to read the data into. </param>
        /// <param name="offset"> The offset to read from. Must be 0. </param>
        /// <param name="count"> The number of bytes to read. Must be equal to buffer.Length. </param>
        /// <returns> The count of bytes successfully read into the provided buffer. </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Contract.Requires(buffer != null);

            if (offset != 0 || count != buffer.Length)
            {
                throw new NotSupportedException();
            }

            return (int)this.audioSource.Read(buffer);
        }
    }
}
