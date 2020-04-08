// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Linq;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Background;

    /// <summary>
    /// A collection of static helpers to abstract the operations needed to manage registration
    /// with the Windows Conversational Agent (MVA) Platform.
    /// </summary>
    public static class MVARegistrationHelpers
    {
        /// <summary>
        /// The name associated with the background trigger for conversational agent (MVA)
        /// platform activation, used to check for the presence of and then register the
        /// appropriate background task.
        /// </summary>
        public const string BackgroundTriggerName = "AgentBackgroundTrigger";

        /// <summary>
        /// Gets or sets a value indicating whether the background task needed for conversational
        /// agent activations via keyword is currently registered for this application. Requires
        /// first unlocking the conversational agent limited access feature and will throw an
        /// exception if it has not yet been acquired.
        /// </summary>
        public static bool IsBackgroundTaskRegistered
        {
            get => MVARegistrationHelpers.GetIsTaskRegistered();
            set
            {
                if (value && !MVARegistrationHelpers.GetIsTaskRegistered())
                {
                    var builder = new BackgroundTaskBuilder()
                    {
                        Name = MVARegistrationHelpers.BackgroundTriggerName,
                    };
                    builder.SetTrigger(new ConversationalAgentTrigger());
                    builder.Register();
                }
                else if (!value && MVARegistrationHelpers.GetIsTaskRegistered())
                {
                    BackgroundTaskRegistration.AllTasks.Values
                        .Where(task => task.Name == MVARegistrationHelpers.BackgroundTriggerName)
                        .ToList()
                        .ForEach(task => task.Unregister(true));
                }
            }
        }

        /// <summary>
        /// Attempts to unlock the Limited Access Feature needed to use the
        /// Windows.ApplicationModel.ConversationalAgent and related APIs. This requires the use of
        /// a token that is acquired from conversation with Microsoft and associated with the
        /// specific publisher and package family name for your application. Changes to the
        /// package identity of your application may require obtaining a new limited access
        /// feature key.
        /// </summary>
        public static void UnlockLimitedAccessFeature()
        {
            var access = LimitedAccessFeatures.TryUnlockFeature(
                "com.microsoft.windows.applicationmodel.conversationalagent_v1",
                "whvvNXgJDOtLXr7ZYd2a1Q==", // app-specific token obtained from Microsoft
                "8wekyb3d8bbwe has registered their use of com.microsoft.windows.applicationmodel.conversationalagent_v1 with Microsoft and agrees to the terms of use.");

            var success = access.Status == LimitedAccessFeatureStatus.Available
                || access.Status == LimitedAccessFeatureStatus.AvailableWithoutToken;

            if (!success)
            {
                const string message = "Failed to unlock the conversational agent limited access "
                    + "feature needed for the Windows.ApplicationModel.ConversationalAgent "
                    + "namespace. Please verify that your package identity matches what was "
                    + "provided to Microsoft when generating your key and request a new key "
                    + "if this identity has changed.";
                throw new MethodAccessException(message);
            }
        }

        private static bool GetIsTaskRegistered() => BackgroundTaskRegistration.AllTasks.Values
            .Any(task => task.Name == MVARegistrationHelpers.BackgroundTriggerName);
    }
}
