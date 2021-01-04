// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioInput
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using System.Threading;
    using UWPVoiceAssistantSample.AudioCommon;
    using Windows.Media.MediaProperties;
    using Windows.Storage;

    /// <summary>
    /// Wrapper used to encapsulate a span of audio to be captured for diagnostic and development
    /// purposes. Performs just-in-time creation of the file upon first write and flushes/closes
    /// the file upon disposal of the object.
    /// </summary>
    public class DebugAudioCapture : IDisposable
    {
        private Stream outputStream;
        private SemaphoreSlim outputStreamSemaphore = new SemaphoreSlim(1, 1);
        private string label;
        private bool alreadyDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugAudioCapture"/> class.
        /// If later written to, this will create an output file timestamped with the given
        /// label and serialize all audio to a file for future examination.
        /// </summary>
        /// <param name="label"> The label to use for the debug audio file. </param>
        /// <returns> A task that completes once the file is created and ready for writing. </returns>
        public DebugAudioCapture(string label)
        {
            this.label = label;
        }

        /// <summary>
        /// Gets or sets the output encoding to use for the generated file.
        /// </summary>
        public AudioEncodingProperties OutputEncoding { get; set; } = DialogAudio.Pcm16KHz16BitMono.Encoding;

        /// <summary>
        /// Writes the provided data to the debug output. Will be flushed and finalized when
        /// this object is disposed.
        /// </summary>
        /// <param name="data"> The data to write. </param>
        public void Write(byte[] data)
        {
            Contract.Requires(data != null);

            using (this.outputStreamSemaphore.AutoReleaseWait())
            {
                if (this.alreadyDisposed)
                {
                    return;
                }

                if (this.outputStream == null)
                {
                    var serializedTime = $"{DateTime.Now:yyyyMMdd_HHmmss}";
                    var filename = $"{serializedTime}_{this.label}.wav";
                    var filePath = Path.Combine(
                        ApplicationData.Current.LocalFolder.Path,
                        filename);
                    this.outputStream = File.OpenWrite(filePath);
                    this.outputStream.Write(new byte[44], 0, 44);
                }

                this.outputStream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Disposes of the object according to the IDisposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the object according to the augmented IDisposable pattern.
        /// </summary>
        /// <param name="disposing"> Whether this is being invoked from a top-level Dispose. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.alreadyDisposed)
            {
                this.alreadyDisposed = true;
                using (this.outputStreamSemaphore.AutoReleaseWait())
                {
                    this.WriteDebugWavHeader();
                    this.outputStream?.Dispose();
                    this.outputStream = null;
                }

                this.outputStreamSemaphore?.Dispose();
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
        private void WriteDebugWavHeader()
        {
            if (this.outputStream == null)
            {
                return;
            }

            ushort channels = (ushort)this.OutputEncoding.ChannelCount;
            int sampleRate = (int)this.OutputEncoding.SampleRate;
            ushort bytesPerSample = (ushort)(this.OutputEncoding.BitsPerSample / 8);

            this.outputStream.Position = 0;

            // RIFF header.
            // Chunk ID.
            this.outputStream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            this.outputStream.Write(BitConverter.GetBytes((int)this.outputStream.Length - 8), 0, 4);

            // Format.
            this.outputStream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            this.outputStream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            this.outputStream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            this.outputStream.Write(BitConverter.GetBytes((ushort)1), 0, 2);

            // Channels.
            this.outputStream.Write(BitConverter.GetBytes(channels), 0, 2);

            // Sample rate.
            this.outputStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            this.outputStream.Write(BitConverter.GetBytes(sampleRate * channels * bytesPerSample), 0, 4);

            // Block align.
            this.outputStream.Write(BitConverter.GetBytes((ushort)channels * bytesPerSample), 0, 2);

            // Bits per sample.
            this.outputStream.Write(BitConverter.GetBytes((ushort)(bytesPerSample * 8)), 0, 2);

            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            this.outputStream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            this.outputStream.Write(BitConverter.GetBytes((int)(this.outputStream.Length - 44)), 0, 4);
        }
    }
}
