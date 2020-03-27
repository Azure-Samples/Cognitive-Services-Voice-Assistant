// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Jot.DefaultInitializer;

    public class AppSettings
    {
        public AppSettings()
        {
            this.DisplaySettings = new DisplaySettings();
            this.RuntimeSettings = new RuntimeSettings();
        }

        [Trackable]
        public DisplaySettings DisplaySettings { get; set; }

        [Trackable]
        public RuntimeSettings RuntimeSettings { get; set; }
    }
}
