// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System.Collections.Generic;

    /// <summary>
    /// Aggregates test results per input test file.
    /// </summary>
    internal class TestReport
    {
        /// <summary>
        /// Gets or sets the name of the test file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the total number of dialogs in <see cref="FileName"/>.
        /// </summary>
        public int DialogCount { get; set; }

        /// <summary>
        /// Gets or sets the total the number of dialogs test that have passed in <see cref="FileName"/>.
        /// </summary>
        public int DialogPassCount { get; set; }

        /// <summary>
        /// Gets or sets the dialogs pass rate, which is the number of dialog tests that passed divided by the total number of dialogs in <see cref="FileName"/>.
        /// </summary>
        public float DialogPassRate { get; set; }

        /// <summary>
        /// Gets or sets the Dialog Results for each Dialog in the file.
        /// </summary>
        public List<DialogResult> DialogResults { get; set; }

        /// <summary>
        /// Computation of DialogPassRate - The number of dialog tests that passed divided by the total number of dialogs in the test.
        /// </summary>
        public void ComputeDialogPassRate()
        {
            int pos = 0;
            foreach (DialogResult item in this.DialogResults)
            {
                if (item.DialogPass)
                {
                    pos += 1;
                }
            }

            this.DialogPassCount = pos;
            this.DialogPassRate = (float)pos / this.DialogCount;
        }
    }
}
