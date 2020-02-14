// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using Microsoft.Bot.Schema;
    using Microsoft.CognitiveServices.Speech;
    using NAudio.Wave;
    using Newtonsoft.Json;

    public enum ListenState
    {
        NotListening,
        Initiated,
        Listening,
    }

    public enum Sender
    {
        Bot,
        User,
        Channel,
    }

    public class RequestDetails
    {
        [JsonProperty("interactionId")]
        public string InteractionId { get; set; }
    }

    public class ConvAiData
    {
        [JsonProperty("requestInfo")]
        public RequestDetails RequestInfo { get; set; }
    }

    public class SpeechChannelData
    {
        [JsonProperty("conversationalAiData")]
        public ConvAiData ConversationalAiData { get; set; }
    }

    public class WavQueueEntry
    {
        public WavQueueEntry(string id, bool playInitiated, ProducerConsumerStream stream, RawSourceWaveStream reader) =>
            (this.Id, this.PlayInitiated, this.Stream, this.Reader) = (id, playInitiated, stream, reader);

        public string Id { get; }

        public bool PlayInitiated { get; set; } = false;

        public bool SynthesisFinished { get; set; } = false;

        public ProducerConsumerStream Stream { get; }

        public RawSourceWaveStream Reader { get; }
    }

    public class MessageDisplay
    {
        public MessageDisplay(string msg, Sender from, IEnumerable<FrameworkElement> cards = null) => (this.From, this.Message, this.AdaptiveCards) = (from, msg, cards);

        public Sender From { get; set; }

        public string Message { get; set; }

        public IEnumerable<FrameworkElement> AdaptiveCards { get; private set; }

        public override string ToString()
        {
            return $"{this.From}: {this.Message}";
        }
    }

    public class ActivityDisplay
    {
        public ActivityDisplay(string json, IActivity activity, DateTime time) => (this.Json, this.Activity, this.Time) = (json, activity, time);

        public ActivityDisplay(string json, IActivity activity, Sender sender) => (this.Json, this.Activity, this.From) = (json, activity, sender);

        public Sender From { get; set; }

        public IActivity Activity { get; set; }

        public string Json { get; set; }

        public DateTime Time { get; set; } = DateTime.Now;

        public string TypeSummary => this.Activity.Type == ActivityTypes.Event ? $"Event: {this.Activity.AsEventActivity().Name}" : this.Activity.Type;

        public override string ToString()
        {
            return $"{this.Time.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)}{Environment.NewLine}{this.Json}";
        }
    }

    public class CustomSpeechConfiguration
    {
        public CustomSpeechConfiguration(string endpointId)
        {
            if (endpointId == null)
            {
                endpointId = string.Empty;
            }

            this.EndpointId = endpointId;

            Guid parsedGuid = Guid.Empty;
            if (Guid.TryParse(this.EndpointId, out parsedGuid) &&
                parsedGuid.ToString("D", null).Equals(this.EndpointId, StringComparison.OrdinalIgnoreCase))
            {
                this.IsValid = true;
            }
            else
            {
                this.IsValid = false;
            }
        }

        public string EndpointId { get; private set; }

        public bool IsValid { get; private set; }
    }

    public class VoiceDeploymentConfiguration
    {
        public VoiceDeploymentConfiguration(string voiceDeploymentIds)
        {
            if (voiceDeploymentIds == null)
            {
                voiceDeploymentIds = string.Empty;
            }

            this.VoiceDeploymentIds = voiceDeploymentIds;

            // TODO: Change the code below to accept multiple GUIDs, separated by comma
            Guid parsedGuid = Guid.Empty;
            if (Guid.TryParse(this.VoiceDeploymentIds, out parsedGuid) &&
                parsedGuid.ToString("D", null).Equals(this.VoiceDeploymentIds, StringComparison.OrdinalIgnoreCase))
            {
                this.IsValid = true;
            }
            else
            {
                this.IsValid = false;
            }
        }

        public string VoiceDeploymentIds { get; private set; }

        public bool IsValid { get; private set; }
    }

    public class WakeWordConfiguration
    {
        public WakeWordConfiguration(string path)
        {
            if (path == null)
            {
                path = string.Empty;
            }

            this.Path = path;

            try
            {
                var fileInfo = new FileInfo(path);
                this.Name = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name);
                this.Name = this.Name.Replace('_', ' ');
                var textInfo = new CultureInfo("en-US").TextInfo;
                this.Name = textInfo.ToTitleCase(this.Name);
                this.WakeWordModel = KeywordRecognitionModel.FromFile(path);
                this.IsValid = true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Debug.WriteLine($"Bad path to WakeWordConfiguration: {ex.Message}");
                this.IsValid = false;
                this.Name = string.Empty;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        ~WakeWordConfiguration()
        {
            if (this.WakeWordModel != null)
            {
                this.WakeWordModel.Dispose();
                this.WakeWordModel = null;
            }
        }

        public string Path { get; private set; }

        public string Name { get; private set; }

        public bool IsValid { get; private set; }

        public KeywordRecognitionModel WakeWordModel { get; private set; }
    }
}
