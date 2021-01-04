// Copyright (c) Microsoft Corporation.
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

        public new IAgentSessionWrapper AgentSession
        {
            get => base.AgentSession;
            set => base.AgentSession = value;
        }

        public new AudioGraph InputGraph
        {
            get => base.InputGraph;
            set => base.InputGraph = value;
        }

        public new bool GraphRunning
        { 
            get => base.GraphRunning; 
            set => base.GraphRunning = value;
        }

        public new AudioEncodingProperties OutputEncoding
        {
            get => base.OutputEncoding;
            set => base.OutputEncoding = value;
        }

        public new AudioFrameOutputNode OutputNode
        {
            get => base.OutputNode;
            set => base.OutputNode = value;
        }

        public static async new Task<TestAgentAudioInputProvider> InitializeFromNowAsync()
        {
            var result = await InitializeFromAgentSessionAsync(null);
            return result;
        }

        public static async new Task<TestAgentAudioInputProvider> InitializeFromAgentSessionAsync(IAgentSessionWrapper session)
        {
            return await FromAgentSessionAsync(session, DefaultEncoding);
        }

        public static async Task<TestAgentAudioInputProvider> FromAgentSessionAsync(IAgentSessionWrapper session, AudioEncodingProperties properties)
        {
            var result = new TestAgentAudioInputProvider()
            {
                AgentSession = session,
                OutputEncoding = properties,
                GraphRunning = false,
            };

            await result.PerformAudioSetupAsync();
            return result;
        }

        private async Task PerformAudioSetupAsync()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Speech)
            {
                EncodingProperties = this.OutputEncoding,
            };

            var createGraph = await AudioGraph.CreateAsync(settings);
            if (createGraph.Status != AudioGraphCreationStatus.Success)
            {
                var message = $"Failed to initialize AudioGraph in TestAgentAudioProducer with creation status: {createGraph.Status.ToString()}";
                throw new InvalidOperationException(message, createGraph.ExtendedError);
            }

            this.InputGraph = createGraph.Graph;

            await this.CopyFile();

            //FileInputNode
            var folder = ApplicationData.Current.LocalFolder;
            IStorageFile item = await folder.GetFileAsync("ContosoTellMeAJoke.wav");
            var nodeResult = await this.InputGraph.CreateFileInputNodeAsync(item);

            if (nodeResult.Status != AudioFileNodeCreationStatus.Success)
            {
                var message = $"Failed to initialize FileInputNode in TestAgentAudioProducer with creation status: {nodeResult.Status.ToString()}";
                throw new InvalidOperationException(message, nodeResult.ExtendedError);
            }

            this.InputNode = nodeResult.FileInputNode;
            this.OutputNode = this.InputGraph.CreateFrameOutputNode();

            //FileInputNode
            this.InputNode.AddOutgoingConnection(this.OutputNode);
            this.InputGraph.QuantumStarted += this.OnQuantumStarted;
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
