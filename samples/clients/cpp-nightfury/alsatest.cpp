#include <stdio.h>
#include <stdlib.h>
#include <alsa/asoundlib.h>

int main(int argc, char *argv[])
{
	int i;
	int err;
	int buf[BUF_SIZE];
	snd_pcm_t *playback_handle;
	FILE *fin;
	size_t nread;
	snd_pcm_format_t format = SND_PCM_FORMAT_S16_LE;

	if((err = snd_pcm_open(&playback_handle, argv[1], SND_PCM_STREAM_PLAYBACK, 0)) < 0) {
		fprintf(stderr, "cannot open audio device %s (%s)\n", argv[1], snd_strerror (err));
		exit(1);
	}

	return 0;
}
