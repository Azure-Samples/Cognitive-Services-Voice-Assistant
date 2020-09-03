// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.AudioOutput
{
    using System;
    using System.Threading.Tasks;
    using Windows.Media.MediaProperties;
    using Windows.Media.Playback;

    /// <summary>
    /// An implementation of the IDialogAudioOutputAdapter interface that uses the MediaPlayer object as its playback
    /// mechanism. Relies on a MediaStreamSource object that in turn requires a backing IRandomAccessStream partial
    /// implementation to fulfill.
    /// </summary>
    public class MediaPlayerDialogAudioOutputAdapter
        : IDialogAudioOutputAdapter
    {
        private readonly MediaPlayer mediaPlayer;
        private DialogAudioOutputMediaSource mediaSource;
        private MediaPlaybackState lastPlaybackState;
        private bool alreadyDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlayerDialogAudioOutputAdapter"/> class.
        /// </summary>
        public MediaPlayerDialogAudioOutputAdapter()
        {
            this.mediaPlayer = new MediaPlayer()
            {
                AudioCategory = MediaPlayerAudioCategory.Speech,
                Volume = 1.0,
            };
            this.mediaPlayer.PlaybackSession.PlaybackStateChanged += (session, _) =>
            {
                if (this.lastPlaybackState == MediaPlaybackState.Playing
                    && session.PlaybackState != MediaPlaybackState.Playing)
                {
                    this.OutputEnded?.Invoke();
                }

                this.lastPlaybackState = session.PlaybackState;
            };
        }

        /// <summary>
        /// An event raised when the output of the current media on the adapter has finished.
        /// </summary>
        public event Action OutputEnded;

        /// <summary>
        /// Gets a value indicating whether the output adapter is currently outputting audio.
        /// </summary>
        public bool IsPlaying
        {
            get => this.mediaPlayer?.PlaybackSession?.PlaybackState == MediaPlaybackState.Playing;
        }

        /// <summary>
        /// Gets or sets the output encoding to use for this adapter. Must match the encoding of incoming data streams
        /// for playback.
        /// </summary>
        public AudioEncodingProperties OutputEncoding { get; set; }

        /// <summary>
        /// Disposes of underlying managed resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates a MediaSource from the provided DialogAudioOutputStream and immediately plays it on the underlying
        /// MediaPlayer.
        /// </summary>
        /// <param name="stream"> The dialog audio to begin playing. </param>
        /// <returns> A task that immediately completes. </returns>
        public Task PlayAudioAsync(DialogAudioOutputStream stream)
        {
            this.mediaPlayer.Pause();
            this.OutputEncoding = LocalSettingsHelper.OutputFormat.Encoding;
            this.mediaSource = new DialogAudioOutputMediaSource(stream);
            this.mediaPlayer.Source = this.mediaSource.WindowsMediaSource;
            this.mediaPlayer.Play();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Immediately stops any ongoing audio playback on the underlying MediaPlayer.
        /// </summary>
        /// <returns> A task that immediately completes. </returns>
        public Task StopPlaybackAsync()
        {
            this.mediaPlayer.Pause();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Disposes of underlying managed resources using a common pattern to avoid redundancy.
        /// </summary>
        /// <param name="isDisposing"> Whether this is called from a Dispose top-level method. </param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!this.alreadyDisposed)
            {
                if (isDisposing)
                {
                    this.mediaPlayer?.Dispose();
                }

                this.alreadyDisposed = true;
            }
        }
    }
}
