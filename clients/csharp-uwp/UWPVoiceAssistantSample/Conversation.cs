namespace UWPVoiceAssistantSample
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Chat view Conversation model.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// List of conversations in a session.
        /// </summary>
        public ObservableCollection<Conversation> conversations = new ObservableCollection<Conversation>();

        /// <summary>
        /// Gets or sets the text of the conversation.
        /// </summary>
        public string Body { get; set; }

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
        public bool Received { get { return !this.Sent; } }
    }
}
