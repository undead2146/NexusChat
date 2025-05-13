namespace NexusChat.Core.Models
{
    /// <summary>
    /// Status of an AI model's availability
    /// </summary>
    public enum ModelStatus
    {
        /// <summary>
        /// Unknown status
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// Model is available and ready to use
        /// </summary>
        Available = 1,
        
        /// <summary>
        /// Model should be available but hasn't been tested
        /// </summary>
        PotentiallyAvailable = 2,
        
        /// <summary>
        /// Model requires an API key which is not set
        /// </summary>
        NoApiKey = 3,
        
        /// <summary>
        /// Model is unavailable (e.g., deprecated or requires special permissions)
        /// </summary>
        Unavailable = 4,
        
        /// <summary>
        /// Model could not be found
        /// </summary>
        NotFound = 5,
        
        /// <summary>
        /// Error occurred while checking status
        /// </summary>
        Error = 6
    }
}
