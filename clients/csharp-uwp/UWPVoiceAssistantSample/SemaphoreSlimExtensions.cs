// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods to the SemaphoreSlim class that include:
    ///
    /// AutoReleaseWaitAsync: helper to simplify safe use of SemaphoreSlim
    ///   Proper use of the SemaphoreSlim class requires acquiring the Semaphore via WaitAsync,
    ///   wrapping the code to be protected by the semaphore in a try {} block, and then releasing
    ///   the semaphore within a finally {} block. This is cumbersome.
    ///
    ///   This allows the pattern to be simplified to:
    ///     using (await semaphore.AutoReleaseWaitAsync())
    ///     {
    ///       // code to execute
    ///     }
    ///    The above will still safely release the semaphore, but without an additional block.
    /// </summary>
    public static class SemaphoreSlimExtensions
    {
        /// <summary>
        /// Asynchronously acquires the provided SemaphoreSlim and returns an IDisposable interface
        /// that, when disposed, will release the underlying semaphore.
        /// </summary>
        /// <param name="semaphore"> The semaphore to acquire and release. </param>
        /// <returns> A task that completes once the semaphore has been acquired. </returns>
        public static async Task<IDisposable> AutoReleaseWaitAsync(
            this SemaphoreSlim semaphore)
        {
            Contract.Requires(semaphore != null);
            var wrapper = new ReleaseableSemaphoreSlimWrapper(semaphore);
            await semaphore.WaitAsync();
            return wrapper;
        }

        private class ReleaseableSemaphoreSlimWrapper
            : IDisposable
        {
            private readonly SemaphoreSlim semaphore;
            private bool alreadyDisposed = false;

            public ReleaseableSemaphoreSlimWrapper(SemaphoreSlim semaphore)
                => this.semaphore = semaphore;

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposeActuallyCalled)
            {
                if (!this.alreadyDisposed)
                {
                    if (disposeActuallyCalled)
                    {
                        this.semaphore.Release();
                    }

                    this.alreadyDisposed = true;
                }
            }
        }
    }
}
