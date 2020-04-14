// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioOutput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.Media.MediaProperties;

    /// <summary>
    /// An abstraction for various output formats available for Direct Line Speech text-to-speech.
    /// </summary>
    public class DirectLineSpeechAudioOutputFormat
    {
        private static readonly Lazy<List<DirectLineSpeechAudioOutputFormat>> LazySupportedFormats
            = new Lazy<List<DirectLineSpeechAudioOutputFormat>>(() =>
            {
                return new List<DirectLineSpeechAudioOutputFormat>()
                {
                    new PcmRawFormat(8000, 16, 1),
                    new PcmRawFormat(16000, 16, 1),
                    new Mp3Format(16000, 32000, 1),
                    new Mp3Format(16000, 64000, 1),
                    new Mp3Format(16000, 128000, 1),
                    new Mp3Format(24000, 48000, 1),
                    new Mp3Format(24000, 96000, 1),
                    new Mp3Format(24000, 160000, 1),
                };
            });

        private DirectLineSpeechAudioOutputFormat(string encodingSubtype, uint sampleRate, uint bitsPerSample, uint channels, uint bitrate)
        {
            string prefix;
            string suffix;
            string infix;
            string channel = (channels == 1) ? "mono" : (channels == 2) ? "stereo" : throw new NotImplementedException();

            if (encodingSubtype == "WAV")
            {
                this.Encoding = AudioEncodingProperties.CreatePcm(sampleRate, channels, bitsPerSample);
                prefix = "raw";
                suffix = "pcm";
                infix = $"{bitsPerSample}bit";
            }
            else if (encodingSubtype == "MP3")
            {
                this.Encoding = AudioEncodingProperties.CreateMp3(sampleRate, channels, bitrate);
                prefix = "audio";
                suffix = "mp3";
                infix = $"{bitrate / 1000}kbitrate";
            }
            else
            {
                throw new NotImplementedException();
            }

            this.FormatLabel = $"{prefix}-{sampleRate / 1000}khz-{infix}-{channel}-{suffix}";
        }

        /// <summary>
        /// Gets a partial list of supported formats for Direct Line text-to-speech. This is a subset of the Speech
        /// SDK's SpeechSynthesisOutputFormat set.
        /// </summary>
        public static List<DirectLineSpeechAudioOutputFormat> SupportedFormats
        {
            get => LazySupportedFormats.Value;
        }

        /// <summary>
        /// Gets the AudioEncodingProperties associated with this output format.
        /// </summary>
        public AudioEncodingProperties Encoding { get; private set; }

        /// <summary>
        /// Gets the string representation of the output format as used by the Speech SDK.
        /// </summary>
        public string FormatLabel { get; private set; }

        /// <summary>
        /// Returns an appropriate <see cref="DirectLineSpeechAudioOutputFormat"/> that matches the provided
        /// <see cref="AudioEncodingProperties"/>.
        /// </summary>
        /// <param name="encoding"> The AudioEncodingProperties object to search for in the list of supported formats. </param>
        /// <returns> A DirectLineSpeechAudioOutputFormat that matches the provided AudioEncodingProperties. </returns>
        public static DirectLineSpeechAudioOutputFormat GetFromEncoding(AudioEncodingProperties encoding)
        {
            return SupportedFormats.First(format =>
                format.Encoding.Subtype == encoding.Subtype
                && format.Encoding.SampleRate == encoding.SampleRate
                && format.Encoding.BitsPerSample == encoding.BitsPerSample
                && format.Encoding.ChannelCount == encoding.ChannelCount
                && format.Encoding.Bitrate == encoding.Bitrate);
        }

        private class Mp3Format : DirectLineSpeechAudioOutputFormat
        {
            public Mp3Format(uint samplerate, uint bitrate, uint channels)
                : base("MP3", samplerate, 0, channels, bitrate)
            {
            }
        }

        private class PcmRawFormat : DirectLineSpeechAudioOutputFormat
        {
            public PcmRawFormat(uint sampleRate, uint bitsPerSample, uint channels)
                : base("WAV", sampleRate, bitsPerSample, channels, 0)
            {
            }
        }
    }
}
