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
        /// Gets or sets the number of dialogs completed over the total number of dialogs in an Input File.
        /// </summary>
        public float DialogCompletionRate { get; set; }

        /// <summary>
        /// Gets or sets the name of the corresponding input file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the total the number of Dialogs marked completed in <see cref="FileName"/>.
        /// </summary>
        public int NumDialogCompleted { get; set; }

        /// <summary>
        /// Gets or sets the Total Number of Dialogs in <see cref="FileName"/>.
        /// </summary>
        public int TotalNumDialog { get; set; }

        /// <summary>
        /// Gets or sets the Dialog Results for each Dialog in the file.
        /// </summary>
        public List<DialogResult> DialogResults { get; set; }

        /// <summary>
        /// Computation of DialogCompletionRate. Dialogs compeleted divided by Total Number of Dialogs.
        /// </summary>
        public void ComputeTaskCompletionRate()
        {
            float pos = 0;
            foreach (var item in this.DialogResults)
            {
                if (item.DialogCompletionStatus)
                {
                    pos += 1;
                }
            }

            this.NumDialogCompleted = (int)pos;
            this.DialogCompletionRate = pos / this.TotalNumDialog;
        }
    }
}
