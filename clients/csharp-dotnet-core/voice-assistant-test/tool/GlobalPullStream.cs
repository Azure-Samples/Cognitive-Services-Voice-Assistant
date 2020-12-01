// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VoiceAssistantTest
{
    /// <summary>
    /// This class provides a static access to a single FilePullStreamCallback.
    /// </summary>
    public static class GlobalPullStream
    {
        private static FilePullStreamCallback filePullStreamCallback = new FilePullStreamCallback();

        /// <summary>
        /// Gets the object referring to the global FilePullStreamCallback.
        /// </summary>
        public static FilePullStreamCallback FilePullStreamCallback { get => filePullStreamCallback; }
    }
}
