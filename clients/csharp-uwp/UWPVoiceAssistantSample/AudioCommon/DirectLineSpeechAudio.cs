// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioCommon
{
    using System.Collections.Immutable;

    /// <summary>
    /// An abstraction of the audio selection and availability specific to Direct Line Speech.
    /// </summary>
    public static class DirectLineSpeechAudio
    {
        /// <summary>
        /// Gets the collection of DialogAudio objects supported by Direct Line Speech for audio input.
        /// </summary>
        public static IImmutableList<DialogAudio> SupportedInputFormats { get; } = new DialogAudio[]
        {
            DialogAudio.Pcm16KHz16BitMono,
        }.ToImmutableList();

        /// <summary>
        /// Gets the collection of DialogAudio objects supported by Direct Line Speech for audio output.
        /// </summary>
        public static IImmutableList<DialogAudio> SupportedOutputFormats { get; } = new DialogAudio[]
        {
            DialogAudio.Pcm8KHz16BitMono,
            DialogAudio.Mpeg16KHz32KBitRateMono,
            DialogAudio.Mpeg16KHz64KBitRateMono,
            DialogAudio.Mpeg16KHz128KBitRateMono,
            DialogAudio.Mpeg24KHz48KBitRateMono,
            DialogAudio.Mpeg24KHz96KBitRateMono,
            DialogAudio.Mpeg24KHz160KBitRateMono,
        }.ToImmutableList();

        /// <summary>
        /// Gets the default DialogAudio to be used by Direct Line Speech for audio input.
        /// </summary>
        public static DialogAudio DefaultInput { get; } = DialogAudio.Pcm16KHz16BitMono;

        /// <summary>
        /// Gets the default DialogAudio to be used by Direct Line Speech for audio output.
        /// </summary>
        public static DialogAudio DefaultOutput { get; } = DialogAudio.Mpeg24KHz96KBitRateMono;
    }
}
