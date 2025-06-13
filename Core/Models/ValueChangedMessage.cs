using CommunityToolkit.Mvvm.Messaging.Messages;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Message sent when favorite models have changed and UI needs to refresh
    /// </summary>
    public class FavoritesChangedMessage : ValueChangedMessage<bool>
    {
        public FavoritesChangedMessage() : base(true)
        {
        }

        public string Provider { get; set; } = string.Empty;
        public string Reason { get; set; } = "Favorites updated";
    }

    public class ConversationsChangedMessage : ValueChangedMessage<bool>
    {
        public ConversationsChangedMessage() : base(true)
        {
        }

        public string Reason { get; set; } = "Conversations updated";
    }

    
    public class CurrentModelChangedMessage : ValueChangedMessage<AIModel?>
    {
        public CurrentModelChangedMessage(AIModel? newModel) : base(newModel)
        {
        }
    }
}
