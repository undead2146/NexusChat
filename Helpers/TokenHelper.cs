using System;
using System.Text.RegularExpressions;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for token-related operations
    /// </summary>
    public static class TokenHelper
    {
        // Simple approximation: ~4 characters per token on average
        private const int CHARS_PER_TOKEN = 4;
        
        /// <summary>
        /// Estimates token count for a string using simple character count approximation
        /// </summary>
        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            return text.Length / CHARS_PER_TOKEN + 1;
        }
        
        /// <summary>
        /// More accurate token estimation for English text
        /// </summary>
        public static int EstimateTokensAccurate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Count words (split on whitespace and punctuation)
            var wordCount = Regex.Split(text.Trim(), @"\s+").Length;
            
            // Count special tokens like punctuation
            var punctuationCount = Regex.Matches(text, @"[,.!?;:]").Count;
            
            // Estimate tokens based on words, punctuation, and some extra for special characters
            return wordCount + punctuationCount / 2 + 1;
        }
        
        /// <summary>
        /// Estimates token count for a model with a specific context window
        /// </summary>
        public static bool WouldExceedContextWindow(int currentTokens, string newText, int maxContextWindow)
        {
            int estimatedNewTokens = EstimateTokens(newText);
            return (currentTokens + estimatedNewTokens) > maxContextWindow;
        }
    }
}
