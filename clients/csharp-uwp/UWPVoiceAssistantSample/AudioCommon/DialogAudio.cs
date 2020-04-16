// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioCommon
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Windows.Media.MediaProperties;
    using Windows.Storage;

    /// <summary>
    /// An abstraction of and static holder collection for common audio formats used with assistant experiences.
    /// </summary>
    public class DialogAudio
    {
        private const string KeyForSubtype = "subtype";
        private const string KeyForSampleRate = "sampleRate";
        private const string KeyForBitsPerSample = "bitsPerSample";
        private const string KeyForBitrate = "bitrate";
        private const string KeyForChannelCount = "channelCount";

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudio"/> class.
        /// </summary>
        /// <param name="subtype"> The media subtype (e.g. MP3) for this audio. </param>
        /// <param name="sampleRate"> The sample rate, in Hz, of this audio. </param>
        /// <param name="bitsPerSample"> The number of bits per sample for this audio. Mutually exclusive with bitRate. </param>
        /// <param name="bitRate"> The bits per second for this audio. Mutually exclusive with bitsPerSample. </param>
        /// <param name="channelCount"> The number of channels associated with this audio. </param>
        private DialogAudio(string subtype, uint sampleRate, uint bitsPerSample, uint bitRate, uint channelCount)
        {
            var labelBuilder = new StringBuilder();
            var suffix = string.Empty;

            switch (subtype)
            {
                case "WAV:":
                case "PCM":
                    labelBuilder.Append("Raw");
                    suffix = "Pcm";
                    this.Encoding = AudioEncodingProperties.CreatePcm(sampleRate, channelCount, bitsPerSample);
                    break;
                case "MP3":
                    labelBuilder.Append("Audio");
                    suffix = "Mp3";
                    this.Encoding = AudioEncodingProperties.CreateMp3(sampleRate, channelCount, bitRate);
                    break;
            }

            // Construct a readable label in a format like 'Raw-16KHz-16Bit-Mono-Pcm'
            labelBuilder.Append($"-{sampleRate / 1000}KHz-");
            labelBuilder.Append(bitsPerSample > 0 ? $"{bitsPerSample}Bit-" : string.Empty);
            labelBuilder.Append(bitRate > 0 ? $"{bitRate / 1000}KBitRate-" : string.Empty);
            labelBuilder.Append(channelCount == 1 ? "Mono-" : throw new NotImplementedException());
            labelBuilder.Append(suffix);

            this.Label = labelBuilder.ToString();
        }

        /// <summary>
        /// Gets a DialogAudio for low-quality 8KHz, 16-bit PCM.
        /// </summary>
        public static DialogAudio Pcm8KHz16BitMono { get; } = new DialogAudio("PCM", 16000, 16, 0, 1);

        /// <summary>
        /// Gets a DialogAudio for standard-quality 16KHz, 16-bit PCM. The most common default format.
        /// </summary>
        public static DialogAudio Pcm16KHz16BitMono { get; } = new DialogAudio("PCM", 16000, 16, 0, 1);

        /// <summary>
        /// Gets a DialogAudio for 16KHz, 32kbps MPEG2.
        /// </summary>
        public static DialogAudio Mpeg16KHz32KBitRateMono { get; } = new DialogAudio("MP3", 16000, 0, 32000, 1);

        /// <summary>
        /// Gets a DialogAudio for 16KHz, 64kbps MPEG2.
        /// </summary>
        public static DialogAudio Mpeg16KHz64KBitRateMono { get; } = new DialogAudio("MP3", 16000, 0, 64000, 1);

        /// <summary>
        /// Gets a DialogAudio for 16KHz, 128kbps MPEG2.
        /// </summary>
        public static DialogAudio Mpeg16KHz128KBitRateMono { get; } = new DialogAudio("MP3", 16000, 0, 128000, 1);

        /// <summary>
        /// Gets a DialogAudio for 24KHz, 48kbps MPEG2.
        /// </summary>
        public static DialogAudio Mpeg24KHz48KBitRateMono { get; } = new DialogAudio("MP3", 24000, 0, 48000, 1);

        /// <summary>
        /// Gets a DialogAudio for 24KHz, 96kbps MPEG2.
        /// </summary>
        public static DialogAudio Mpeg24KHz96KBitRateMono { get; } = new DialogAudio("MP3", 24000, 0, 96000, 1);

        /// <summary>
        /// Gets a DialogAudio for 24KHz, 160kbps MPEG2.
        /// </summary>
        public static DialogAudio Mpeg24KHz160KBitRateMono { get; } = new DialogAudio("MP3", 24000, 0, 160000, 1);

        /// <summary>
        /// Gets the full list of enumerated audio formats.
        /// </summary>
        public static IImmutableList<DialogAudio> AllFormats { get; } = new DialogAudio[]
        {
            Pcm8KHz16BitMono,
            Pcm16KHz16BitMono,
            Mpeg16KHz32KBitRateMono,
            Mpeg16KHz64KBitRateMono,
            Mpeg16KHz128KBitRateMono,
            Mpeg24KHz48KBitRateMono,
            Mpeg24KHz96KBitRateMono,
            Mpeg24KHz160KBitRateMono,
        }.ToImmutableList();

        /// <summary>
        /// Gets the underlying AudioEncodingProperties for this audio format.
        /// </summary>
        public AudioEncodingProperties Encoding { get; private set; }

        /// <summary>
        /// Gets a readable label representing this audio format.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Attempts to find an enumerated DialogAudio that matches the serialized form present in a provided LocalSettings
        /// value.
        /// </summary>
        /// <param name="settingsValue"> A serialized DialogAudio object from LocalSettings data. </param>
        /// <param name="dialogAudio"> If successful, a reference to the matching DialogAudio. </param>
        /// <returns> A value indicating whether the search was successful. </returns>
        public static bool TryGetFromSettingsValue(ApplicationDataCompositeValue settingsValue, out DialogAudio dialogAudio)
            => (dialogAudio = GetFromSettingsValue(settingsValue)) != null;

        /// <summary>
        /// find an enumerated DialogAudio that matches the serialized form present in a provided LocalSettings value.
        /// </summary>
        /// <param name="settingsValue"> A serialized DialogAudio object from LocalSettings data. </param>
        /// <returns> A reference to the matching DialogAudio object. Null if not found. </returns>
        public static DialogAudio GetFromSettingsValue(ApplicationDataCompositeValue settingsValue)
        {
            bool Matches<T>(string key, T value)
                where T : IEquatable<T>
                => settingsValue.Keys.Contains(key) && Equals(settingsValue[key], value);

            var dialogAudio = AllFormats.FirstOrDefault(format =>
                Matches<string>(KeyForSubtype, format.Encoding.Subtype)
                && Matches<uint>(KeyForSampleRate, format.Encoding.SampleRate)
                && Matches<uint>(KeyForBitsPerSample, format.Encoding.BitsPerSample)
                && Matches<uint>(KeyForBitrate, format.Encoding.Bitrate)
                && Matches<uint>(KeyForChannelCount, format.Encoding.ChannelCount));

            return dialogAudio;
        }

        /// <summary>
        /// Finds the first enumerated DialogAudio object that matches the provided readable label.
        /// </summary>
        /// <param name="label"> The label to match. Case-sensitive. </param>
        /// <returns> The first match found. Null if not found. </returns>
        public static DialogAudio GetMatchFromLabel(string label) => AllFormats.FirstOrDefault(format => format.Label == label);

        /// <summary>
        /// Serializes the data for this DialogAudio into a composite data value suitable for storage in LocalSettings.
        /// </summary>
        /// <returns> A new ApplicationDataCompositeValue that can be written to LocalSettings for later retrieval. </returns>
        public ApplicationDataCompositeValue SerializeToSettingsValue()
        {
            var result = new ApplicationDataCompositeValue
            {
                [KeyForSubtype] = this.Encoding.Subtype,
                [KeyForSampleRate] = this.Encoding.SampleRate,
                [KeyForBitsPerSample] = this.Encoding.BitsPerSample,
                [KeyForBitrate] = this.Encoding.Bitrate,
                [KeyForChannelCount] = this.Encoding.ChannelCount,
            };

            return result;
        }
    }
}
