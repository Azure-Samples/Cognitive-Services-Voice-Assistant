using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace VoiceAssistantTest
{
    enum FileReadStatus
    {
        HASDATA,
        NODATA
    }

    class FilePullStreamCallback : PullAudioInputStreamCallback
    {
        public bool realTime = true;

        private const int MaxSizeOfTtsAudioInBytes = 65536;
        private const int WavHeaderSizeInBytes = 44;
        private FileReadStatus fileReadStatus = FileReadStatus.NODATA;
        private FileStream currentFileStream = null;
        private string filepath = string.Empty;
        private int mBytesPerSecond = 32000;
        private int totalTimeRead = 0;

        public override int Read(byte[] dataBuffer, uint size)
        {
            switch (this.fileReadStatus)
            {
                case FileReadStatus.HASDATA:
                    // read the data from the file
                    var readTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    int readBytes = this.currentFileStream.Read(dataBuffer, 0, (int)size);
                    readTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - readTime;
                    if (readBytes == 0)
                    {
                        // we are at end of file
                        this.fileReadStatus = FileReadStatus.NODATA;
                        Array.Clear(dataBuffer, 0, (int)size);
                        this.currentFileStream.Close();
                        this.currentFileStream.Dispose();
                        this.currentFileStream = null;
                        return (int)size;
                    }
                    else
                    {
                        if (this.realTime)
                        {
                            this.totalTimeRead += 1000 * (int)Math.Round((double)readBytes / this.mBytesPerSecond);
                            int timeToSleep = (int)Math.Round((1000 * ((double)readBytes / this.mBytesPerSecond)) - readTime);
                            Thread.Sleep(timeToSleep);
                        }

                        return readBytes;
                    }

                case FileReadStatus.NODATA:
                default:
                    Array.Clear(dataBuffer, 0, (int)size);
                    return (int)size;
            }
        }

        public void ReadFile(string filepath)
        {
            if (this.currentFileStream != null)
            {
                this.currentFileStream.Close();
                this.currentFileStream = null;
            }

            this.currentFileStream = File.OpenRead(filepath);

            byte[] dataBuffer = new byte[WavHeaderSizeInBytes];

            // Reading header bytes
            int headerBytes = this.currentFileStream.Read(dataBuffer, 0, WavHeaderSizeInBytes);
            this.fileReadStatus = FileReadStatus.HASDATA;
        }

        public override void Close()
        {
            base.Close();
        }

        public void Dispose()
        {
            if (this.currentFileStream != null)
            {
                this.currentFileStream.Close();
                this.currentFileStream = null;
            }
        }
    }
}
