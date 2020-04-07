﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AgentAudioInputProviderTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Media.Audio;
    using Windows.Media.MediaProperties;
    using Windows.Media.Render;
    using Windows.Storage;
    using UWPVoiceAssistantSample;

    public class TestAgentAudioInputProvider : AgentAudioInputProvider, IDialogAudioInputProvider<List<byte>>
    {
        public static AudioEncodingProperties DefaultEncodingProp { get => DefaultEncoding; }
        public AudioFileInputNode InputNode;

        public AudioGraph InputGraph { get => inputGraph; }
        public AudioEncodingProperties OutputEncoding { get => outputEncoding; }
        public IAgentSessionWrapper AgentSession { get => agentSession; }
        public AudioFrameOutputNode OutputNode { get => outputNode; }
        public bool GraphRunning { get => graphRunning; }
        public bool Disposed { get => disposed; }
        public SemaphoreSlim DebugAudioOutputFileSemaphore { get => debugAudioOutputFileSemaphore; }
        public Stream DebugAudioOutputFileStream { get => debugAudioOutputFileStream; }

        public static async Task<TestAgentAudioInputProvider> InitializeFromNowAsync()
        {
            var result = await InitializeFromAgentSessionAsync(null);
            return result;
        }

        public static async Task<TestAgentAudioInputProvider> InitializeFromAgentSessionAsync(IAgentSessionWrapper session)
        {
            return await FromAgentSessionAsync(session, DefaultEncoding);
        }

        public static async Task<TestAgentAudioInputProvider> FromAgentSessionAsync(IAgentSessionWrapper session, AudioEncodingProperties properties)
        {
            var result = new TestAgentAudioInputProvider()
            {
                agentSession = session,
                outputEncoding = properties,
                graphRunning = false,
                debugAudioOutputFileSemaphore = new SemaphoreSlim(1, 1)
            };

            await result.PerformAudioSetupAsync();
            return result;
        }

        private async Task PerformAudioSetupAsync()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Speech)
            {
                EncodingProperties = outputEncoding
            };

            var createGraph = await AudioGraph.CreateAsync(settings);
            if (createGraph.Status != AudioGraphCreationStatus.Success)
            {
                var message = $"Failed to initialize AudioGraph in TestAgentAudioProducer with creation status: {createGraph.Status.ToString()}";
                throw new InvalidOperationException(message, createGraph.ExtendedError);
            }

            this.inputGraph = createGraph.Graph;

            await this.CopyFile();

            //FileInputNode
            var folder = ApplicationData.Current.LocalFolder;
            IStorageFile item = await folder.GetFileAsync("ContosoTellMeAJoke.wav");
            var nodeResult = await inputGraph.CreateFileInputNodeAsync(item);

            if (nodeResult.Status != AudioFileNodeCreationStatus.Success)
            {
                var message = $"Failed to initialize FileInputNode in TestAgentAudioProducer with creation status: {nodeResult.Status.ToString()}";
                throw new InvalidOperationException(message, nodeResult.ExtendedError);
            }

            this.InputNode = nodeResult.FileInputNode;
            this.outputNode = inputGraph.CreateFrameOutputNode();

            //FileInputNode
            this.InputNode.AddOutgoingConnection(this.outputNode);
            this.inputGraph.QuantumStarted += this.OnQuantumStarted;

            this.DataAvailable += async (bytes) =>
            {
                using (await this.debugAudioOutputFileSemaphore.AutoReleaseWaitAsync())
                {
                    this.debugAudioOutputFileStream?.Write(bytes.ToArray(), 0, bytes.Count);
                }
            };
        }

        public void DisposeAgentAudioProducer()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task CopyFile()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///ContosoTellMeAJoke.wav"));
            var localStateFolder = ApplicationData.Current.LocalFolder.GetFileAsync("ContosoTellMeAJoke.wav").Status;
            if (file != null && localStateFolder == Windows.Foundation.AsyncStatus.Error)
            {
                await file.CopyAsync(ApplicationData.Current.LocalFolder);
                string wavFile = ApplicationData.Current.LocalFolder.Path + "/ContosoTellMeAJoke.wav";
                Debug.WriteLine(wavFile);
            }
        }
    }
}
