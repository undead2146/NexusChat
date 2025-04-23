using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Message class for model animations
    /// </summary>
    public class ModelAnimationMessage : ValueChangedMessage<int>
    {
        public ModelAnimationMessage(int value) : base(value) {}
    }

    public delegate void ModelAnimationHandler(ModelAnimationMessage message);

    /// <summary>
    /// Helper class for managing messaging center communications
    /// </summary>
    public static class MessagingHelper
    {
        // Message keys
        public const string AnimateDefaultStar = "AnimateDefaultStar";
        private const string FavoriteStarAnimationToken = "FavoriteStarAnimation";
        
        /// <summary>
        /// Sends a request to animate the default star for a model
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="modelId">ID of the model to animate</param>
        public static void RequestAnimateDefaultStar(object sender, int modelId)
        {
            MessagingCenter.Send(sender, AnimateDefaultStar, modelId);
        }
        
        /// <summary>
        /// Send message to animate the favorite star for a model
        /// </summary>
        /// <param name="modelId">The ID of the model to animate</param>
        public static void SendFavouriteStarAnimation(int modelId)
        {
            try
            {
                Debug.WriteLine($"Sending favourite star animation for model {modelId}");
                // Fix: Create a proper message instance
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(
                    new ModelAnimationMessage(modelId));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending favourite star animation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Subscribes to the animate default star message
        /// </summary>
        /// <typeparam name="TSender">Type of the sender</typeparam>
        /// <param name="subscriber">The subscribing object</param>
        /// <param name="callback">Callback to invoke when message is received</param>
        public static void SubscribeToAnimateDefaultStar<TSender>(object subscriber, Action<TSender, int> callback)
            where TSender : class
        {
            MessagingCenter.Subscribe<TSender, int>(subscriber, AnimateDefaultStar, callback);
        }
        
        /// <summary>
        /// Register for favorite star animation messages
        /// </summary>
        /// <param name="recipient">The recipient object</param>
        /// <param name="handler">The method to call when animation is needed</param>
        public static void RegisterForFavouriteStarAnimation(object recipient, ModelAnimationHandler handler)
        {
            try
            {
                // Fix: Use the correct method signature for Register
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Register<ModelAnimationMessage>(
                    recipient, 
                    (r, m) => handler(m));
                    
                Debug.WriteLine($"Registered {recipient} for favourite star animations");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering for favourite star animation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Unsubscribes from the animate default star message
        /// </summary>
        /// <typeparam name="TSender">Type of the sender</typeparam>
        /// <param name="subscriber">The subscribing object</param>
        public static void UnsubscribeFromAnimateDefaultStar<TSender>(object subscriber)
            where TSender : class
        {
            MessagingCenter.Unsubscribe<TSender, int>(subscriber, AnimateDefaultStar);
        }
        
        /// <summary>
        /// Unregister from favorite star animation messages
        /// </summary>
        /// <param name="recipient">The recipient object to unregister</param>
        public static void UnregisterFromFavouriteStarAnimation(object recipient)
        {
            try
            {
                // Fix: Use the correct method signature for Unregister
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Unregister<ModelAnimationMessage>(recipient);
                    
                Debug.WriteLine($"Unregistered {recipient} from favourite star animations");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unregistering from favourite star animation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Request animation for a favorite star
        /// This method is used by AIModelItemViewModel
        /// </summary>
        public static void RequestAnimateFavouriteStar(int modelId)
        {
            SendFavouriteStarAnimation(modelId);
        }
    }
}
