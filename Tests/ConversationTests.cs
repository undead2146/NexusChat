using NexusChat.Models;
using System;

namespace NexusChat.Tests
{
    /// <summary>
    /// Test class for Conversation model
    /// </summary>
    public static class ConversationTests
    {
        /// <summary>
        /// Runs all tests for the Conversation model
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public static bool RunAllTests()
        {
            bool allTestsPassed = true;

            try
            {
                // Test basic conversation creation
                Console.WriteLine("Testing basic conversation creation...");
                TestConversationCreation();
                Console.WriteLine("✓ Basic conversation creation test passed");

                // Test conversation validation
                Console.WriteLine("Testing conversation validation...");
                TestConversationValidation();
                Console.WriteLine("✓ Conversation validation test passed");

                // Test conversation methods
                Console.WriteLine("Testing conversation methods...");
                TestConversationMethods();
                Console.WriteLine("✓ Conversation methods test passed");

                Console.WriteLine("All Conversation model tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                allTestsPassed = false;
            }

            return allTestsPassed;
        }

        private static void TestConversationCreation()
        {
            // 1. Test default constructor
            var conversation1 = new Conversation();
            if (conversation1 == null) throw new Exception("Default constructor failed");
            if (conversation1.Messages == null) throw new Exception("Messages collection not initialized");
            
            // 2. Test parameterized constructor
            int testUserId = 42;
            string testTitle = "Test Chat";
            int? testModelId = 7;
            
            var conversation2 = new Conversation(testUserId, testTitle, testModelId);
            
            if (conversation2.UserId != testUserId)
                throw new Exception("UserId was not set correctly in constructor");
                
            if (conversation2.Title != testTitle)
                throw new Exception("Title was not set correctly in constructor");
                
            if (conversation2.ModelId != testModelId)
                throw new Exception("ModelId was not set correctly in constructor");
                
            if (conversation2.CreatedAt.Date != DateTime.UtcNow.Date)
                throw new Exception("CreatedAt was not set to current date");
                
            if (conversation2.UpdatedAt.Date != DateTime.UtcNow.Date)
                throw new Exception("UpdatedAt was not set to current date");
                
            // 3. Test auto title when null provided
            var conversation3 = new Conversation(testUserId);
            if (conversation3.Title != "New Conversation")
                throw new Exception("Default title was not set correctly");
                
            // 4. Test CreateTestConversation
            var testConversation = Conversation.CreateTestConversation();
            if (testConversation.Title != "Test Conversation")
                throw new Exception("Test conversation title incorrect");
            if (!testConversation.IsFavorite)
                throw new Exception("Test conversation should be favorite");
            if (testConversation.Category != "Testing")
                throw new Exception("Test conversation category incorrect");
        }

        private static void TestConversationValidation()
        {
            // 1. Valid conversation
            var validConversation = new Conversation(1, "Valid Conversation");
            if (!validConversation.Validate(out string error1))
                throw new Exception($"Valid conversation failed validation: {error1}");
            
            // 2. Invalid UserId
            var invalidUserIdConversation = new Conversation { Title = "Invalid User" };
            if (invalidUserIdConversation.Validate(out string error2))
                throw new Exception("Conversation with invalid UserId passed validation");
            if (!error2.Contains("user ID"))
                throw new Exception("Wrong error message for invalid UserId");
            
            // 3. Missing title
            var noTitleConversation = new Conversation(1, "");
            if (noTitleConversation.Validate(out string error3))
                throw new Exception("Conversation with empty title passed validation");
            if (!error3.Contains("title is required"))
                throw new Exception("Wrong error message for empty title");
            
            // 4. Title too long
            var longTitleConversation = new Conversation(1, new string('A', 101));
            if (longTitleConversation.Validate(out string error4))
                throw new Exception("Conversation with too long title passed validation");
            if (!error4.Contains("cannot exceed 100"))
                throw new Exception("Wrong error message for too long title");
            
            // 5. Category too long
            var longCategoryConversation = new Conversation(1, "Valid Title") { Category = new string('A', 51) };
            if (longCategoryConversation.Validate(out string error5))
                throw new Exception("Conversation with too long category passed validation");
            if (!error5.Contains("Category cannot exceed"))
                throw new Exception("Wrong error message for too long category");
        }

        private static void TestConversationMethods()
        {
            // Test Touch method
            var conversation = new Conversation(1, "Test Touch");
            DateTime originalUpdateTime = conversation.UpdatedAt;
            
            // Wait at least 1ms to ensure timestamp changes
            System.Threading.Thread.Sleep(1);
            
            conversation.Touch();
            if (conversation.UpdatedAt <= originalUpdateTime)
                throw new Exception("Touch method didn't update the timestamp");
            
            // Test AddTokens method
            int originalTokens = conversation.TotalTokensUsed;
            int tokensToAdd = 42;
            
            conversation.AddTokens(tokensToAdd);
            if (conversation.TotalTokensUsed != originalTokens + tokensToAdd)
                throw new Exception("AddTokens method didn't add tokens correctly");
            
            // Test adding 0 or negative tokens
            conversation.AddTokens(0);
            if (conversation.TotalTokensUsed != originalTokens + tokensToAdd)
                throw new Exception("AddTokens should ignore zero tokens");
            
            conversation.AddTokens(-1);
            if (conversation.TotalTokensUsed != originalTokens + tokensToAdd)
                throw new Exception("AddTokens should ignore negative tokens");
            
            // Test ToggleFavorite method
            var conversation2 = new Conversation(1, "Test Favorites") { IsFavorite = false };
            
            bool newStatus = conversation2.ToggleFavorite();
            if (!conversation2.IsFavorite || !newStatus)
                throw new Exception("ToggleFavorite didn't change value to true");
            
            newStatus = conversation2.ToggleFavorite();
            if (conversation2.IsFavorite || newStatus)
                throw new Exception("ToggleFavorite didn't change value back to false");
        }
    }
}
