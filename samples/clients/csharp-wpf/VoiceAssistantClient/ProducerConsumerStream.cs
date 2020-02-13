// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient
{
    using System;
    using System.IO;

    public class ProducerConsumerStream : Stream
    {
        private readonly MemoryStream innerStream = new MemoryStream();
        private readonly object lockable = new object();

        private bool disposed = false;

        private long readPosition = 0;

        private long writePosition = 0;

        public ProducerConsumerStream()
        {
        }

        ~ProducerConsumerStream()
        {
            this.Dispose(false);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get
            {
                lock (this.lockable)
                {
                    return this.innerStream.Length;
                }
            }
        }

        public override long Position
        {
            get
            {
                lock (this.lockable)
                {
                    return this.innerStream.Position;
                }
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            lock (this.lockable)
            {
                this.innerStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (this.lockable)
            {
                this.innerStream.Position = this.readPosition;
                int red = this.innerStream.Read(buffer, offset, count);
                this.readPosition = this.innerStream.Position;

                return red;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // Seek is for read only
            return this.readPosition;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this.lockable)
            {
                this.innerStream.Position = this.writePosition;
                this.innerStream.Write(buffer, offset, count);
                this.writePosition = this.innerStream.Position;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free managed objects help by this instance
                if (this.innerStream != null)
                {
                    this.innerStream.Dispose();
                }
            }

            // Free any unmanaged objects here.

            this.disposed = true;

            // Call the base class implementation.
            base.Dispose(disposing);
        }
    }
}
