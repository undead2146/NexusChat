using NexusChat.Core.Models;
using System;

namespace NexusChat.Tests
{
    /// <summary>
    /// Test class for User model
    /// </summary>
    public static class UserTests
    {
        /// <summary>
        /// Runs all tests for the User model
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public static bool RunAllTests()
        {
            bool allTestsPassed = true;

            try
            {
                // Test basic user creation
                Console.WriteLine("Testing basic user creation...");
                TestBasicUserCreation();
                Console.WriteLine("✓ Basic user creation test passed");

                // Test user validation
                Console.WriteLine("Testing user validation...");
                TestUserValidation();
                Console.WriteLine("✓ User validation test passed");

                // Test password verification
                Console.WriteLine("Testing password verification...");
                TestPasswordVerification();
                Console.WriteLine("✓ Password verification test passed");

                Console.WriteLine("All User model tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                allTestsPassed = false;
            }

            return allTestsPassed;
        }

        private static void TestBasicUserCreation()
        {
            // 1. Test default constructor
            var user1 = new User();
            if (user1 == null) throw new Exception("Default constructor failed");

            // 2. Test parameterized constructor
            string testUsername = "johndoe";
            string testPasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            var user2 = new User(testUsername, testPasswordHash);
            
            if (user2.Username != testUsername)
                throw new Exception("Username was not set correctly in constructor");
            
            if (user2.PasswordHash != testPasswordHash)
                throw new Exception("PasswordHash was not set correctly in constructor");
            
            if (user2.DisplayName != testUsername)
                throw new Exception("DisplayName was not defaulted to username");
            
            if (user2.DateCreated.Date != DateTime.UtcNow.Date)
                throw new Exception("DateCreated was not set to current date");

            // 3. Test full constructor
            string testDisplayName = "John Doe";
            string testAvatarPath = "avatars/johndoe.jpg";
            string testEmail = "john@example.com";
            string testTheme = "Dark";
            int? testModelId = 2;
            
            var user3 = new User(testUsername, testPasswordHash, testDisplayName, testAvatarPath, 
                testEmail, testTheme, testModelId);
            
            if (user3.DisplayName != testDisplayName)
                throw new Exception("DisplayName was not set correctly");
            
            if (user3.AvatarPath != testAvatarPath)
                throw new Exception("AvatarPath was not set correctly");
            
            if (user3.Email != testEmail)
                throw new Exception("Email was not set correctly");
            
            if (user3.PreferredTheme != testTheme)
                throw new Exception("PreferredTheme was not set correctly");
            
            if (user3.PreferredModelId != testModelId)
                throw new Exception("PreferredModelId was not set correctly");
        }

        private static void TestUserValidation()
        {
            // 1. Test valid user
            var validUser = new User("validuser", BCrypt.Net.BCrypt.HashPassword("validpass"))
            {
                Email = "valid@example.com",
                PreferredTheme = "Light"
            };
            
            if (!validUser.Validate(out string error1))
                throw new Exception($"Valid user failed validation: {error1}");

            // 2. Test invalid username (too short)
            var shortUsernameUser = new User("ab", BCrypt.Net.BCrypt.HashPassword("validpass"));
            if (shortUsernameUser.Validate(out string error2))
                throw new Exception("User with too short username passed validation");
            if (!error2.Contains("at least 3 characters"))
                throw new Exception("Wrong error message for short username");

            // 3. Test invalid username (invalid characters)
            var invalidUsernameUser = new User("user*name", BCrypt.Net.BCrypt.HashPassword("validpass"));
            if (invalidUsernameUser.Validate(out string error3))
                throw new Exception("User with invalid username characters passed validation");
            if (!error3.Contains("can only contain"))
                throw new Exception("Wrong error message for invalid username characters");

            // 4. Test invalid email
            var invalidEmailUser = new User("validuser", BCrypt.Net.BCrypt.HashPassword("validpass"))
            {
                Email = "not-an-email"
            };
            if (invalidEmailUser.Validate(out string error4))
                throw new Exception("User with invalid email passed validation");
            if (!error4.Contains("Invalid email"))
                throw new Exception("Wrong error message for invalid email");

            // 5. Test invalid theme
            var invalidThemeUser = new User("validuser", BCrypt.Net.BCrypt.HashPassword("validpass"))
            {
                PreferredTheme = "InvalidTheme"
            };
            if (invalidThemeUser.Validate(out string error5))
                throw new Exception("User with invalid theme passed validation");
            if (!error5.Contains("must be Light, Dark, or System"))
                throw new Exception("Wrong error message for invalid theme");
        }

        private static void TestPasswordVerification()
        {
            string plainPassword = "SecretP@ss123";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            
            var user = new User("testuser", passwordHash);
            
            // Test correct password
            if (!user.VerifyPassword(plainPassword))
                throw new Exception("Password verification failed for correct password");
            
            // Test incorrect password
            if (user.VerifyPassword("WrongPassword"))
                throw new Exception("Password verification succeeded for incorrect password");
        }
    }
}
