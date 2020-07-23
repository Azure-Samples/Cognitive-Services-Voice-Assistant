// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioCommon
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using Windows.Media.MediaProperties;

    /// <summary>
    /// An enumeration specifying the desired behavior when writing a WAVEFORMATEX header to a stream.
    /// </summary>
    public enum WaveHeaderLengthOption
    {
        /// <summary>
        /// Apply the actual length of the stream to the RIFF header information.
        /// </summary>
        UseRealLength,

        /// <summary>
        /// Apply the maximum possible value for the advertised size of the stream. Useful for indefinite-length data.
        /// </summary>
        UseMaximumLength,

        /// <summary>
        /// Report an empty stream with no data in the header.
        /// </summary>
        UseZeroLength,
    }

    /// <summary>
    /// An encapsulation of extra data and operations needed to apply a WAVEFORMAT header to an existing stream.
    /// </summary>
    public static class WaveHeader
    {
        /// <summary>
        /// Writes a standard WAVEFORMAT header (RIFF) to the provided stream that matches the provided PCM encoding
        /// properties. This must be applied before any data is written to the stream; it will otherwise overwrite that
        /// data.
        /// </summary>
        /// <param name="stream"> The stream to which the header should be applied. </param>
        /// <param name="encoding"> The AudioEncodingProperties from which to derive the header format information. </param>
        /// <param name="lengthOption"> How the header's length field should be treated. </param>
        public static void WriteWaveFormatHeaderToStream(Stream stream, AudioEncodingProperties encoding, WaveHeaderLengthOption lengthOption)
        {
            Contract.Requires(stream != null);
            Contract.Requires(encoding != null);
            Contract.Requires(encoding.Subtype == "WAV" || encoding.Subtype == "PCM");

            ushort channels = (ushort)encoding.ChannelCount;
            int sampleRate = (int)encoding.SampleRate;
            ushort bytesPerSample = (ushort)(encoding.BitsPerSample / 8);
            var length = lengthOption == WaveHeaderLengthOption.UseMaximumLength ? int.MaxValue - 8
                : lengthOption == WaveHeaderLengthOption.UseZeroLength ? 0
                : lengthOption == WaveHeaderLengthOption.UseRealLength ? (int)stream.Length - 8
                : throw new NotImplementedException();

            stream.Position = 0;

            // RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            stream.Write(BitConverter.GetBytes(length), 0, 4);

            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);

            // Channels.
            stream.Write(BitConverter.GetBytes(channels), 0, 2);

            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            stream.Write(BitConverter.GetBytes(sampleRate * channels * bytesPerSample), 0, 4);

            // Block align.
            stream.Write(BitConverter.GetBytes((ushort)channels * bytesPerSample), 0, 2);

            // Bits per sample.
            stream.Write(BitConverter.GetBytes((ushort)(bytesPerSample * 8)), 0, 2);

            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes((int)(stream.Length - 44)), 0, 4);
        }
    }
}
