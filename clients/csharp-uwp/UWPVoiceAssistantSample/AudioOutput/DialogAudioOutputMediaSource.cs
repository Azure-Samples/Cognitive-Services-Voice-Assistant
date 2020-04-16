// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioOutput
{
    using System;
    using System.Diagnostics.Contracts;
    using Windows.Media.Core;
    using Windows.Media.MediaProperties;
    using Windows.Security.Cryptography;

    /// <summary>
    /// A wrapper on top of a DialogAudioOutputStream that encapsulates a MediaSource for Windows Audio playback that
    /// in turn uses a MediaStreamSource. Requires an underlying, partial IRandomAccessStream implementation on the
    /// provided streams to fulfill.
    /// </summary>
    public class DialogAudioOutputMediaSource
    {
        // This value controls how much audio a buffered player (like MediaPlayer) will accumulate before beginning
        // or resuming playback. The default is 3.0 seconds. Lower values will decrease latency but may increase
        // the rate of playback problems like stuttering or artifacts.
        private static readonly TimeSpan TimeToBuffer = TimeSpan.FromSeconds(1.5);

        private readonly DialogAudioOutputStream sourceStream;
        private TimeSpan sampleDuration;
        private TimeSpan totalPlaybackDuration = TimeSpan.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAudioOutputMediaSource"/> class.
        /// </summary>
        /// <param name="stream"> The DialogAudioOutputStream to use as input for this MediaElement. </param>
        public DialogAudioOutputMediaSource(DialogAudioOutputStream stream)
        {
            Contract.Requires(stream != null);

            this.sourceStream = stream;

            // Here we precompute constants for the duration of this source to avoid doing it every sample
            var encoding = stream.Format.Encoding;
            this.sampleDuration = SampleDurationForEncoding(encoding);
            var bytesInSample = (uint)(this.sampleDuration.TotalSeconds * encoding.Bitrate / 8);

            var sourceDescriptor = new AudioStreamDescriptor(stream.Format.Encoding);
            var mediaStreamSource = new MediaStreamSource(sourceDescriptor)
            {
                IsLive = true,
                BufferTime = TimeToBuffer,
            };
            mediaStreamSource.SampleRequested += this.OnMediaSampleRequested;
            this.WindowsMediaSource = MediaSource.CreateFromIMediaSource(mediaStreamSource);
        }

        /// <summary>
        /// Gets the MediaSource object used in Windows Audio components.
        /// </summary>
        public MediaSource WindowsMediaSource { get; private set; }

        /// <summary>
        /// Retrieves a duration associated with MediaStreamSamples for the provided AudioEncodingProperties. These
        /// values were determined based on observation and may require adjustment on other architectures or
        /// environments.
        /// </summary>
        /// <param name="encoding"> The AudioEncodingProperties for this source. </param>
        /// <returns> A TimeSpan representing the expected duration of a single sample for this source. </returns>
        public static TimeSpan SampleDurationForEncoding(AudioEncodingProperties encoding)
        {
            Contract.Requires(encoding != null);

            return TimeSpan.FromMilliseconds(
                encoding.Subtype == "WAV" ? 10
                : encoding.SampleRate == 16000 ? 36
                : encoding.SampleRate == 24000 ? 24
                : throw new NotImplementedException());
        }

        private void OnMediaSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            // Retrieve the deferral for the request to ensure the consumer doesn't time out
            var request = args.Request;
            var deferral = request.GetDeferral();

            var encoding = this.sourceStream.Format.Encoding;
            var bytesForBuffer = new byte[(uint)(this.sampleDuration.TotalSeconds * encoding.Bitrate / 8)];

            var bytesRetrieved = this.sourceStream.Read(bytesForBuffer, 0, bytesForBuffer.Length);
            if (bytesRetrieved == bytesForBuffer.Length)
            {
                var mediaSampleBuffer = CryptographicBuffer.CreateFromByteArray(bytesForBuffer);
                var mediaSample = MediaStreamSample.CreateFromBuffer(mediaSampleBuffer, this.totalPlaybackDuration);
                mediaSample.KeyFrame = true;
                mediaSample.Duration = this.sampleDuration;
                request.Sample = mediaSample;
                this.totalPlaybackDuration += this.sampleDuration;
            }

            deferral.Complete();
        }
    }
}
