using NexusChat.Models;
using System;
using System.Text.Json;

namespace NexusChat.Tests
{
    /// <summary>
    /// Test class for Message model
    /// </summary>
    public static class MessageTests
    {
        /// <summary>
        /// Runs all tests for the Message model
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public static bool RunAllTests()
        {
            bool allTestsPassed = true;

            try
            {
                // Test basic message creation
                Console.WriteLine("Testing basic message creation...");
                TestMessageCreation();
                Console.WriteLine("✓ Basic message creation test passed");

                // Test message validation
                Console.WriteLine("Testing message validation...");
                TestMessageValidation();
                Console.WriteLine("✓ Message validation test passed");

                // Test token estimation and response parsing
                Console.WriteLine("Testing token estimation and response parsing...");
                TestTokenAndParsing();
                Console.WriteLine("✓ Token estimation and response parsing test passed");

                Console.WriteLine("All Message model tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                allTestsPassed = false;
            }

            return allTestsPassed;
        }

        private static void TestMessageCreation()
        {
            // 1. Test default constructor
            var message1 = new Message();
            if (message1 == null) throw new Exception("Default constructor failed");
            if (message1.Timestamp.Date != DateTime.UtcNow.Date)
                throw new Exception("Timestamp not initialized correctly");
                
            // 2. Test parameterized constructor
            int testConversationId = 42;
            string testContent = "Test message content";
            bool testIsAI = true;
            
            var message2 = new Message(testConversationId, testContent, testIsAI);
            
            if (message2.ConversationId != testConversationId)
                throw new Exception("ConversationId was not set correctly in constructor");
                
            if (message2.Content != testContent)
                throw new Exception("Content was not set correctly in constructor");
                
            if (message2.IsAI != testIsAI)
                throw new Exception("IsAI was not set correctly in constructor");
                
            if (message2.Timestamp.Date != DateTime.UtcNow.Date)
                throw new Exception("Timestamp was not set to current date");
                
            if (message2.Status != "delivered")
                throw new Exception("Status was not set to 'delivered' by default");
            
            // 3. Test CreateTestMessage
            var userMessage = Message.CreateTestMessage(1, false);
            if (userMessage.IsAI)
                throw new Exception("Test user message should not be AI");
            if (string.IsNullOrEmpty(userMessage.Content))
                throw new Exception("Test user message should have content");
            if (userMessage.RawResponse != null)
                throw new Exception("User message should not have raw response");
                
            var aiMessage = Message.CreateTestMessage(1, true);
            if (!aiMessage.IsAI)
                throw new Exception("Test AI message should be AI");
            if (string.IsNullOrEmpty(aiMessage.RawResponse))
                throw new Exception("AI message should have raw response");
            if (aiMessage.TokensUsed == 0)
                throw new Exception("AI message should have token usage");
                
            // 4. Test CreateSystemMessage
            string systemContent = "Conversation started";
            var systemMessage = Message.CreateSystemMessage(1, systemContent);
            if (systemMessage.MessageType != "system")
                throw new Exception("System message type not set correctly");
            if (systemMessage.Content != systemContent)
                throw new Exception("System message content not set correctly");
        }

        private static void TestMessageValidation()
        {
            // 1. Valid message
            var validMessage = new Message(1, "Valid content");
            if (!validMessage.Validate(out string error1))
                throw new Exception($"Valid message failed validation: {error1}");
            
            // 2. Invalid ConversationId
            var invalidConvoMessage = new Message { Content = "Invalid conversation" };
            if (invalidConvoMessage.Validate(out string error2))
                throw new Exception("Message with invalid ConversationId passed validation");
            if (!error2.Contains("conversation ID"))
                throw new Exception("Wrong error message for invalid ConversationId");
            
            // 3. Missing content
            var noContentMessage = new Message(1, "");
            if (noContentMessage.Validate(out string error3))
                throw new Exception("Message with empty content passed validation");
            if (!error3.Contains("content is required"))
                throw new Exception("Wrong error message for empty content");
            
            // 4. MessageType too long
            var longTypeMessage = new Message(1, "Valid content") 
                { MessageType = new string('A', 21) };
            if (longTypeMessage.Validate(out string error4))
                throw new Exception("Message with too long MessageType passed validation");
            if (!error4.Contains("type cannot exceed"))
                throw new Exception("Wrong error message for too long MessageType");
            
            // 5. Status too long
            var longStatusMessage = new Message(1, "Valid content") 
                { Status = new string('A', 21) };
            if (longStatusMessage.Validate(out string error5))
                throw new Exception("Message with too long Status passed validation");
            if (!error5.Contains("Status cannot exceed"))
                throw new Exception("Wrong error message for too long Status");
        }

        private static void TestTokenAndParsing()
        {
            // Test token estimation
            var shortMessage = new Message(1, "Short message");
            int shortTokens = shortMessage.EstimateTokens();
            if (shortTokens == 0)
                throw new Exception("Token estimation should not be zero for non-empty content");
                
            var longMessage = new Message(1, new string('A', 100));
            int longTokens = longMessage.EstimateTokens();
            if (longTokens <= shortTokens)
                throw new Exception("Longer message should have higher token estimation");
            
            // Test UpdateTokens
            int newTokens = 42;
            int difference = longMessage.UpdateTokens(newTokens);
            if (longMessage.TokensUsed != newTokens)
                throw new Exception("UpdateTokens did not set the correct token count");
                
            // Test ExtractContentFromRawResponse
            string content = "This is extracted content";
            string openAIRawResponse = $"{{\"choices\":[{{\"message\":{{\"content\":\"{content}\"}}}}]}}";
            var messageWithRaw = new Message(1, "Original content") { RawResponse = openAIRawResponse };
            string extracted = messageWithRaw.ExtractContentFromRawResponse();
            if (extracted != content)
                throw new Exception("Failed to extract content from raw JSON response");
            
            // Test invalid raw response
            messageWithRaw.RawResponse = "Not a valid JSON";
            extracted = messageWithRaw.ExtractContentFromRawResponse();
            if (extracted != null)
                throw new Exception("Should return null for invalid JSON");
        }
    }
}
