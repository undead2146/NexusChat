using System;
using System.Diagnostics;
using NexusChat.Core.Models;

namespace NexusChat.Tests
{
    /// <summary>
    /// Unit tests for the Message model
    /// </summary>
    public class MessageTests
    {
        /// <summary>
        /// Runs all message tests
        /// </summary>
        public static void RunAllTests()
        {
            TestConstructors();
            TestMessageProperties();
            
            Debug.WriteLine("All Message tests passed!");
        }
        
        /// <summary>
        /// Test message constructors
        /// </summary>
        private static void TestConstructors()
        {
            // Test default constructor
            var message = new Message();
            Debug.Assert(message.Timestamp != default);
            
            // Test constructor with content and isAI
            var userMessage = new Message("Hello, world!", false);
            Debug.Assert(userMessage.Content == "Hello, world!");
            Debug.Assert(!userMessage.IsAI);
            Debug.Assert(userMessage.Timestamp != default);
            
            // Test constructor with conversationId, content, and isAI
            var aiMessage = new Message(1, "I am an AI response", true);
            Debug.Assert(aiMessage.ConversationId == 1);
            Debug.Assert(aiMessage.Content == "I am an AI response");
            Debug.Assert(aiMessage.IsAI);
        }
        
        /// <summary>
        /// Test message properties
        /// </summary>
        private static void TestMessageProperties()
        {
            var message = new Message
            {
                Id = 1,
                ConversationId = 2,
                Content = "Test content",
                IsAI = true,
                Timestamp = new DateTime(2023, 1, 1, 12, 0, 0),
                RawResponse = "{\"response\":\"test\"}",
                TokensUsed = 10,
                MessageType = "text",
                Status = "delivered"
            };
            
            Debug.Assert(message.Id == 1);
            Debug.Assert(message.ConversationId == 2);
            Debug.Assert(message.Content == "Test content");
            Debug.Assert(message.IsAI);
            Debug.Assert(message.Timestamp.Year == 2023);
            Debug.Assert(message.RawResponse == "{\"response\":\"test\"}");
            Debug.Assert(message.TokensUsed == 10);
            Debug.Assert(message.MessageType == "text");
            Debug.Assert(message.Status == "delivered");
        }
    }
}
