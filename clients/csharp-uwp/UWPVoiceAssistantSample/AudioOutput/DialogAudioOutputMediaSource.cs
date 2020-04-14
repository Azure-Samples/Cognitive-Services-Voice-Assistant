using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Security.Cryptography;

namespace UWPVoiceAssistantSample.AudioOutput
{
    public class DialogAudioOutputMediaSource
    {
        private static readonly TimeSpan TimeToBuffer = TimeSpan.FromSeconds(1.5);

        private DialogAudioOutputStream sourceStream;
        private TimeSpan totalPlaybackDuration = TimeSpan.Zero;
        private TimeSpan sampleDuration;
        private byte[] sampleBuffer;

        public MediaSource MediaSource { get; private set; }

        public DialogAudioOutputMediaSource(DialogAudioOutputStream stream)
        {
            Contract.Requires(stream != null);

            this.sourceStream = stream;
            this.sampleDuration = SampleDurationForEncoding(stream.Encoding);
            var bytesInSample = (uint)(this.sampleDuration.TotalSeconds * stream.Encoding.Bitrate / 8);
            this.sampleBuffer = new byte[bytesInSample];
            var sourceDescriptor = new AudioStreamDescriptor(stream.Encoding);
            var mediaStreamSource = new MediaStreamSource(sourceDescriptor)
            {
                IsLive = true,
                BufferTime = TimeToBuffer,
            };
            mediaStreamSource.SampleRequested += OnMediaSampleRequested;
            this.MediaSource = MediaSource.CreateFromIMediaSource(mediaStreamSource);
        }

        private void OnMediaSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            // Retrieve the deferral for the request to ensure the consumer doesn't time out
            var request = args.Request;
            var deferral = request.GetDeferral();

            var bytesRetrieved = this.sourceStream.Read(this.sampleBuffer, 0, this.sampleBuffer.Length);
            if (bytesRetrieved == this.sampleBuffer.Length)
            {
                var mediaSampleBuffer = CryptographicBuffer.CreateFromByteArray(this.sampleBuffer);
                var mediaSample = MediaStreamSample.CreateFromBuffer(mediaSampleBuffer, this.totalPlaybackDuration);
                mediaSample.KeyFrame = true;
                mediaSample.Duration = this.sampleDuration;
                request.Sample = mediaSample;
                this.totalPlaybackDuration += this.sampleDuration;
            }

            deferral.Complete();
        }

        public static TimeSpan SampleDurationForEncoding(AudioEncodingProperties encoding)
        {
            return TimeSpan.FromMilliseconds(
                encoding.Subtype == "WAV" ? 10
                : encoding.SampleRate == 16000 ? 36
                : encoding.SampleRate == 24000 ? 24
                : throw new NotImplementedException());
        }
    }
}
