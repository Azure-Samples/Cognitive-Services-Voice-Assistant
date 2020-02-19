// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    /// <summary>
    /// Provides a dialog-backend-independent generalization of error information from a backend.
    /// </summary>
    public class DialogErrorInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogErrorInformation"/> class.
        /// </summary>
        /// <param name="code"> The numeric error code associated with the error. </param>
        /// <param name="details"> A string representation of the error. </param>
        public DialogErrorInformation(int code, string details)
        {
            this.ErrorCode = code;
            this.ErrorDetails = details;
        }

        /// <summary>
        /// Gets a numeric code associated with an error condition, e.g. an HTTP status.
        /// </summary>
        public int ErrorCode { get; private set; }

        /// <summary>
        /// Gets a detailed string representation of an error with additional information for
        /// diagnosis.
        /// </summary>
        public string ErrorDetails { get; private set; }
    }
}
