using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;

namespace UWPVoiceAssistantSample.AudioOutput
{
    class MediaPlayerDialogAudioOutputAdapter
        : IDialogAudioOutputAdapter
    {
        public bool IsPlaying
        {
            get => this.mediaPlayer?.PlaybackSession?.PlaybackState == MediaPlaybackState.Playing;
        }

        public AudioEncodingProperties OutputEncoding { get; set; }

        public event Action OutputEnded;

        private MediaPlayer mediaPlayer;

        private DialogAudioOutputMediaSource mediaSource;

        private bool alreadyDisposed = false;

        public MediaPlayerDialogAudioOutputAdapter()
        {
            this.mediaPlayer = new MediaPlayer()
            {
                AudioCategory = MediaPlayerAudioCategory.Speech,
                Volume = 1.0,
            };
            this.mediaPlayer.MediaEnded += (_, __) => this.OutputEnded?.Invoke();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!this.alreadyDisposed)
            {
                if (isDisposing)
                {

                }

                this.alreadyDisposed = true;
            }
        }

        public async Task PlayAudioAsync(DialogAudioOutputStream stream)
        {
            this.mediaSource = new DialogAudioOutputMediaSource(stream);
            this.mediaPlayer.Source = this.mediaSource.MediaSource;
            this.mediaPlayer.Play();
        }

        public async Task StopPlaybackAsync()
        {
            this.mediaPlayer.Pause();
        }
    }
}
