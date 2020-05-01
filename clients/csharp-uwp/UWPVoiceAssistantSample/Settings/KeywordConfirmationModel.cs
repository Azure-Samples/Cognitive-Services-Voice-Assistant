// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    /// <summary>
    /// A structured and serializable representation of the details associated with a keyword confirmation model used for
    /// confirming an activation signal generated via an activation model on the device.
    /// </summary>
    public class KeywordConfirmationModel
    {
        /// <summary>
        /// Gets or sets the file path associated with the keyword confirmation model.
        /// </summary>
        public string Path { get; set; }
    }
}
