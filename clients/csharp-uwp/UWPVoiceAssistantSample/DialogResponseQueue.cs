// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    /// Queue of dialog responses to be executed sequentially.
    /// </summary>
    public class DialogResponseQueue
    {
        private readonly object actionQueueLock;
        private readonly IDialogAudioOutputAdapter outputAdapter;
        private ConcurrentQueue<DialogResponse> dialogResponseQueue;
        private bool operationInProgress = false;
        private bool listeningToOutput = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogResponseQueue"/> class.
        /// </summary>
        /// <param name="outputAdapter">Output adapter to play audio from dialog responses.</param>
        public DialogResponseQueue(IDialogAudioOutputAdapter outputAdapter)
        {
            this.dialogResponseQueue = new ConcurrentQueue<DialogResponse>();
            this.outputAdapter = outputAdapter;
            this.actionQueueLock = new object();
        }

        /// <summary>
        /// Event that fires when a DialogResponse is being handled
        /// </summary>
        public event Action<DialogResponse> ExecutingResponse;

        /// <summary>
        /// Event that fires when a DialogResponse is being handled
        /// </summary>
        public event Action<DialogResponse> ResponseExecuted;

        /// <summary>
        /// Adds a new response to the dialog queue for processing.
        /// </summary>
        /// <param name="response"> The response to add to the queue. </param>
        public void Enqueue(DialogResponse response)
        {
            lock (this.actionQueueLock)
            {
                if (!this.listeningToOutput)
                {
                    this.listeningToOutput = true;
                    this.outputAdapter.OutputEnded += () =>
                    {
                        this.ResponseExecuted?.Invoke(response);
                        this.operationInProgress = false;
                        this.TryDequeue();
                    };
                }

                this.dialogResponseQueue.Enqueue(response);
                this.TryDequeue();
            }
        }

        /// <summary>
        /// Attempts to dequeue and execute the next item in the response queue.
        /// </summary>
        public void TryDequeue()
        {
            lock (this.actionQueueLock)
            {
                while (!this.dialogResponseQueue.IsEmpty && !this.operationInProgress)
                {
                    var success = this.dialogResponseQueue.TryDequeue(out var nextResponse);

                    if (!success || nextResponse == null)
                    {
                        return;
                    }

                    this.ExecutingResponse?.Invoke(nextResponse);

                    if (nextResponse.MessageMedia != null)
                    {
                        this.operationInProgress = true;
                        _ = this.outputAdapter.PlayAudioAsync(nextResponse.MessageMedia);
                    }
                    else
                    {
                        this.ResponseExecuted?.Invoke(nextResponse);
                    }
                }
            }
        }

        /// <summary>
        /// Cancels all pending response queue items and stops the audio playback associated with any in-progress
        /// responses.
        /// </summary>
        /// <returns> A task that completes once the queue is cleared and playback is stopped. </returns>
        public async Task AbortAsync()
        {
            this.dialogResponseQueue.Clear();
            await this.outputAdapter.StopPlaybackAsync();
        }
    }
}
