// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Chat view Conversation model.
    /// </summary>
    public class Conversation : INotifyPropertyChanged
    {
        private string body;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the text of the conversation.
        /// </summary>
        public string Body
        {
            get
            {
                return this.body;
            }

            set
            {
                if (value != this.body)
                {
                    this.body = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the timestamp of the conversation.
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message was sent from user.
        /// </summary>
        public bool Sent { get; set; }

        /// <summary>
        /// Gets a value indicating whether the message was sent from a bot.
        /// </summary>
        public bool Received
        {
            get { return !this.Sent; }
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}