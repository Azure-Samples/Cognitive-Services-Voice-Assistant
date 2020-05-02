// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    /// <summary>
    /// A structured and serializable representation of the details associated with a keyword activation model used for
    /// initial keyword detection on the device.
    /// </summary>
    public class KeywordActivationModel
    {
        /// <summary>
        /// Gets or sets the display name associated with the keyword activation model.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the file path associated with the keyword activation model.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier associated with the keyword activation model.
        /// </summary>
        public string KeywordId { get; set; }

        /// <summary>
        /// Gets or sets the identifier associated with the keyword activation model's data type, often associated
        /// with a locale or language.
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// Gets or sets the model data format associated with the keyword activation model.
        /// </summary>
        public string ModelDataFormat { get; set; }
    }
}
