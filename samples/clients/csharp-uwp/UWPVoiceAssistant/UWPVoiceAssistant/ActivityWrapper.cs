// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class determines the activity received from the Bot and deserializes the response.
    /// </summary>
    internal class ActivityWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityWrapper"/> class.
        /// </summary>
        /// <param name="activityJson">Parses the JSON Activity response from the Bot.</param>
        public ActivityWrapper(string activityJson)
        {
            var activityObj = JObject.Parse(activityJson);

            switch (activityObj["type"]?.ToString())
            {
                case "trace":
                    this.Type = ActivityType.Trace;
                    break;
                case "message":
                    this.Type = ActivityType.Message;
                    break;
                default:
                    this.Type = ActivityType.Unrecognized;
                    break;
            }

            switch (activityObj["inputHint"]?.ToString())
            {
                case "IgnoringInput":
                    this.InputHint = InputHintType.IgnoringInput;
                    break;
                case "AcceptingInput":
                    this.InputHint = InputHintType.AcceptingInput;
                    break;
                case "ExpectingInput":
                    this.InputHint = InputHintType.ExpectingInput;
                    break;
                default:
                    this.InputHint = InputHintType.Undefined;
                    break;
            }

            this.Message = activityObj["text"]?.ToString();
        }

        /// <summary>
        /// Types of Activities in JSON response.
        /// </summary>
        public enum ActivityType
        {
            /// <summary>
            /// If Activity Type is unrecognized.
            /// </summary>
            Unrecognized,

            /// <summary>
            /// Message Key in Bot Activty JSON.
            /// </summary>
            Message,

            /// <summary>
            /// Trace Key is LUIS Activity JSON.
            /// </summary>
            Trace,
        }

        /// <summary>
        /// InputHint values in JSON response.
        /// https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-add-input-hints?view=azure-bot-service-3.0.
        /// </summary>
        public enum InputHintType
        {
            /// <summary>
            /// InputHint is Undefined.
            /// </summary>
            Undefined,

            /// <summary>
            /// InputHint is ignoringInput.
            /// </summary>
            IgnoringInput,

            /// <summary>
            /// InputHint is acceptingInput.
            /// </summary>
            AcceptingInput,

            /// <summary>
            /// InputHint is expectingInput.
            /// </summary>
            ExpectingInput,
        }

        /// <summary>
        /// Gets ts the value for ActivityType.
        /// Message is returned from the Bot.
        /// Trace is returned from LUIS model.
        /// </summary>
        public ActivityType Type { get; private set; }

        /// <summary>
        /// Gets ts the value for InputHint.
        /// AcceptingInput, ExpectingInput, or IgnoringInput.
        /// </summary>
        public InputHintType InputHint { get; private set; }

        /// <summary>
        /// Gets ts the value of Bot response.
        /// </summary>
        public string Message { get; private set; }
    }
}
