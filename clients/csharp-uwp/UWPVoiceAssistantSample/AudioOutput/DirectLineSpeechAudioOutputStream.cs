// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using Microsoft.CognitiveServices.Speech.Audio;
    using System.Diagnostics.Contracts;
    using UWPVoiceAssistantSample.AudioCommon;

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
        public DirectLineSpeechAudioOutputStream(PullAudioOutputStream audioSource, DialogAudio format)
            : base(format)
        {
            this.audioSource = audioSource;
        }

        /// <summary>
        /// Reads audio from the underlying PullAudioOutputStream into the provided buffer.
        /// </summary>
        /// <param name="buffer"> The buffer to read the data into. </param>
        /// <param name="count"> The number of bytes to read. Must be equal to buffer.Length. </param>
        /// <returns> The count of bytes successfully read into the provided buffer. </returns>
        protected override uint ReadFromRealDialogSource(byte[] buffer, uint count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(buffer.Length == count);

            return this.audioSource.Read(buffer);
        }
    }
}
