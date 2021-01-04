// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioInput
{
    using Microsoft.CognitiveServices.Speech;
    using UWPVoiceAssistantSample.AudioCommon;
    using Windows.Media.MediaProperties;

    /// <summary>
    /// Encapsulation of the data source types usable by PullAudioInputSink.
    /// </summary>
    public class PullAudioDataSource
    {
        private PullAudioDataSource(object baseSource, bool padWithZeroes = true)
        {
            this.BaseSource = baseSource;
            this.PadWithZeroes = padWithZeroes;
        }

        /// <summary>
        /// Gets a predefined PullAudioDataSource for empty input (all zeroes).
        /// </summary>
        public static PullAudioDataSource EmptyInput { get; } = new PullAudioDataSource(null);

        /// <summary>
        /// Gets a predefined PullAudioDataSource that designates data will be manually pushed into the consuming sink.
        /// </summary>
        public static PullAudioDataSource PushedData { get; } = new PullAudioDataSource(null);

        /// <summary>
        /// Gets the underlying object, if applicable, used as the data source.
        /// </summary>
        public object BaseSource { get; private set; }

        /// <summary>
        /// Gets a value indicating whether incomplete reads should be padded with zeroes for this source.
        /// </summary>
        public bool PadWithZeroes { get; private set; } = true;

        /// <summary>
        /// Gets or sets the encoding information associated with the current base audio source.
        /// </summary>
        public AudioEncodingProperties AudioFormat { get; set; } = DirectLineSpeechAudio.DefaultInput.Encoding;

        /// <summary>
        /// Creates a PullAudioDataSource from the provided KeywordRecognitionResult that will instruct consumers to
        /// read data from the derived AudioDataInputStream.
        /// </summary>
        /// <param name="result"> The KeywordRecognitionResult from which to derive the input audio. </param>
        /// <param name="padWithZeroes"> Whether incomplete reads should be padded with zeroes. </param>
        /// <returns> A PullAudioDataSource for this KeywordRecognitionResult. </returns>
        public static PullAudioDataSource FromKeywordResult(KeywordRecognitionResult result, bool padWithZeroes = true)
            => new PullAudioDataSource(AudioDataStream.FromResult(result), padWithZeroes);
    }
}
