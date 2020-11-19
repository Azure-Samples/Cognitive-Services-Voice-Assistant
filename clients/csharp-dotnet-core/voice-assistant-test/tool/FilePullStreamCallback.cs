// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VoiceAssistantTest
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.CognitiveServices.Speech.Audio;

    /// <summary>
    /// This class works as a pull stream for the speech SDK that you can feed files to. When the speech SDK is looking for
    /// audio data, it will call this read function. The read function is responsible for writing bytes to the passed in buffer
    /// and returning the number of valid bytes in that buffer.
    ///
    /// When ReadFile is called, it will setup a filestream that will be read from on the next Read call. When the filestream is empty,
    /// an array of 0's will be returned to simulate silence.
    ///
    /// If realTime is set to true, this class will sleep for the appropriate amount of time for each audio clip.
    /// </summary>
    public class FilePullStreamCallback : PullAudioInputStreamCallback
    {
        private const int WavHeaderSizeInBytes = 44;
        private bool realTime = false;
        private FileReadStatus fileReadStatus = FileReadStatus.NODATA;
        private FileStream currentFileStream = null;
        private int mBytesPerSecond = 32000;

        private enum FileReadStatus
        {
            HASDATA,
            NODATA,
        }

        /// <summary>
        /// Gets or sets a value indicating whether the pullStream should sleep based
        /// on the number of bytes read to simulate real time audio. The SDK has a similar feature. This one is not exposed.
        /// </summary>
        public bool RealTime { get => this.realTime; set => this.realTime = value; }

        /// <summary>
        /// This function is called by the speech SDK when audio is required. It will fill the parameter buffer with any audio file data
        /// available. If no data is available it will write 0's to simulate silence.
        /// </summary>
        /// <param name="dataBuffer">The buffer to place the data in.</param>
        /// <param name="size">The size of the buffer.</param>
        /// <returns>An int indicating the number of bytes placed in the buffer.</returns>
        public override int Read(byte[] dataBuffer, uint size)
        {
            switch (this.fileReadStatus)
            {
                case FileReadStatus.HASDATA:
                    // read the data from the file
                    var readTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    int readBytes = 0;
                    if (this.currentFileStream != null)
                    {
                        readBytes = this.currentFileStream.Read(dataBuffer, 0, (int)size);
                    }

                    readTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - readTime;
                    if (readBytes == 0)
                    {
                        // we are at end of file
                        this.fileReadStatus = FileReadStatus.NODATA;
                        Array.Clear(dataBuffer, 0, (int)size);
                        this.currentFileStream.Close();
                        this.currentFileStream.Dispose();
                        this.currentFileStream = null;
                        Thread.Sleep(100);
                        return (int)size;
                    }
                    else
                    {
                        if (this.RealTime)
                        {
                            int timeToSleep = (int)Math.Round((1000 * ((double)readBytes / this.mBytesPerSecond)) - readTime);
                            Thread.Sleep(timeToSleep);
                        }

                        return readBytes;
                    }

                case FileReadStatus.NODATA:
                default:
                    Array.Clear(dataBuffer, 0, (int)size);
                    Thread.Sleep(100);
                    return (int)size;
            }
        }

        /// <summary>
        /// This function will read the wav header off of the file and setup the file stream for the SpeechSDK to read it.
        /// </summary>
        /// <param name="filepath">The absolute path to the file to read.</param>
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

        /// <summary>
        /// Disposes the underlying resources.
        /// </summary>
        public new void Dispose()
        {
            if (this.currentFileStream != null)
            {
                this.currentFileStream.Close();
                this.currentFileStream = null;
            }

            base.Dispose();
        }
    }
}
