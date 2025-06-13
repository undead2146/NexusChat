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
}
