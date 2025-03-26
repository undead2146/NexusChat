using SQLite;
using System;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents a user in the NexusChat application
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Username for login (must be unique)
        /// </summary>
        [Unique, NotNull, SQLite.MaxLength(50)]
        public string Username { get; set; }

        /// <summary>
        /// Hashed password (BCrypt format)
        /// </summary>
        [NotNull]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Salt used for password hashing
        /// </summary>
        [NotNull]
        public string Salt { get; set; }

        /// <summary>
        /// User's display name shown in the application
        /// </summary>
        [SQLite.MaxLength(100)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Path to user's avatar image
        /// </summary>
        public string AvatarPath { get; set; }

        /// <summary>
        /// Date and time when the user account was created
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Date and time of the user's last login
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// User's preferred theme (Light, Dark, System)
        /// </summary>
        public string PreferredTheme { get; set; } = "System";

        /// <summary>
        /// ID of user's preferred AI model
        /// </summary>
        public int? PreferredModelId { get; set; }

        /// <summary>
        /// Email address for account recovery (optional)
        /// </summary>
        [SQLite.MaxLength(100), EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Indicates if the user's email has been verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Indicates if the user account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Default constructor required by SQLite
        /// </summary>
        public User()
        {
            DateCreated = DateTime.UtcNow;
            IsActive = true;
            
            // Initialize salt to prevent null constraint violations
            Salt = GenerateRandomSalt();
        }

        /// <summary>
        /// Creates a new user with basic required properties
        /// </summary>
        public User(string username, string password)
        {
            Username = username;
            SetPassword(password);
            DisplayName = username; // Default display name to username
            DateCreated = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Creates a new user with all properties
        /// </summary>
        public User(string username, string password, string displayName, string avatarPath, 
                    string email, string preferredTheme, int? preferredModelId)
        {
            Username = username;
            SetPassword(password);
            DisplayName = displayName ?? username;
            AvatarPath = avatarPath;
            Email = email;
            DateCreated = DateTime.UtcNow;
            PreferredTheme = preferredTheme ?? "System";
            PreferredModelId = preferredModelId;
            IsActive = true;
        }

        /// <summary>
        /// Sets a new password for the user
        /// </summary>
        /// <param name="password">New password in plain text</param>
        public void SetPassword(string password)
        {
            // Generate a random salt
            Salt = GenerateRandomSalt();

            // Hash the password with the salt
            PasswordHash = HashPassword(password, Salt);
        }
        
        /// <summary>
        /// Generates a random salt string
        /// </summary>
        private string GenerateRandomSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Verifies if a given plain text password matches the stored hash
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        public bool VerifyPassword(string password)
        {
            var hash = HashPassword(password, Salt);
            return hash == PasswordHash;
        }

        /// <summary>
        /// Helper method to hash a password with a salt
        /// </summary>
        private string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);

            // Combine password and salt
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combinedBytes = new byte[passwordBytes.Length + saltBytes.Length];

            Buffer.BlockCopy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, combinedBytes, passwordBytes.Length, saltBytes.Length);

            // Hash with SHA256
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Validates the user model
        /// </summary>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out string errorMessage)
        {
            // Username validation
            if (string.IsNullOrWhiteSpace(Username))
            {
                errorMessage = "Username is required";
                return false;
            }

            if (Username.Length < 3)
            {
                errorMessage = "Username must be at least 3 characters";
                return false;
            }

            // Only alphanumeric characters and underscore allowed
            if (!Regex.IsMatch(Username, @"^[a-zA-Z0-9_]+$"))
            {
                errorMessage = "Username can only contain letters, numbers, and underscore";
                return false;
            }

            // Password hash validation
            if (string.IsNullOrWhiteSpace(PasswordHash))
            {
                errorMessage = "Password hash is required";
                return false;
            }

            // Email validation (if provided)
            if (!string.IsNullOrWhiteSpace(Email))
            {
                // Simple regex for email validation
                if (!Regex.IsMatch(Email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,}$"))
                {
                    errorMessage = "Invalid email format";
                    return false;
                }
            }

            // PreferredTheme validation
            if (PreferredTheme != "Light" && PreferredTheme != "Dark" && PreferredTheme != "System")
            {
                errorMessage = "Theme must be Light, Dark, or System";
                return false;
            }

            // All validations passed
            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Updates the last login time to current UTC time
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLogin = DateTime.UtcNow;
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"User: {Username} (ID: {Id}, DisplayName: {DisplayName})";
        }

        /// <summary>
        /// Creates a test user for development purposes
        /// </summary>
        public static User CreateTestUser()
        {
            var user = new User
            {
                Username = "testuser",
                DisplayName = "Test User",
                Email = "test@example.com",
                DateCreated = DateTime.UtcNow,
                IsActive = true
            };

            user.SetPassword("password123");
            return user;
        }
    }
}
