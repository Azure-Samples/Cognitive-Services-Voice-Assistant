using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using System.Runtime.CompilerServices;

namespace UWPVoiceAssistantSample.AudioOutput
{
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

        public static List<DirectLineSpeechAudioOutputFormat> SupportedFormats
        {
            get => LazySupportedFormats.Value;
        }

        public static DirectLineSpeechAudioOutputFormat GetFromEncoding(AudioEncodingProperties encoding)
        {
            return SupportedFormats.First(format =>
                format.Encoding.Subtype == encoding.Subtype
                && format.Encoding.SampleRate == encoding.SampleRate
                && format.Encoding.BitsPerSample == encoding.BitsPerSample
                && format.Encoding.ChannelCount == encoding.ChannelCount
                && format.Encoding.Bitrate == encoding.Bitrate);
        }

        public AudioEncodingProperties Encoding { get; private set; }

        public string FormatLabel { get; private set; }
        
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
