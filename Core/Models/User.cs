using SQLite;
using System;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace NexusChat.Models
{
    /// <summary>
    /// Represents a user in the NexusChat application
    /// </summary>
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
        [NotNull, Indexed(Unique = true)]
        [SQLite.MaxLength(50)]
        public string Username { get; set; }

        /// <summary>
        /// Hashed password (BCrypt format)
        /// </summary>
        [NotNull]
        public string PasswordHash { get; set; }

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
            // Default constructor required by SQLite
        }

        /// <summary>
        /// Creates a new user with basic required properties
        /// </summary>
        public User(string username, string passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
            DisplayName = username; // Default display name to username
            DateCreated = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Creates a new user with all properties
        /// </summary>
        public User(string username, string passwordHash, string displayName, string avatarPath, 
                    string email, string preferredTheme, int? preferredModelId)
        {
            Username = username;
            PasswordHash = passwordHash;
            DisplayName = displayName ?? username;
            AvatarPath = avatarPath;
            Email = email;
            DateCreated = DateTime.UtcNow;
            PreferredTheme = preferredTheme ?? "System";
            PreferredModelId = preferredModelId;
            IsActive = true;
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
        /// Verifies if a given plain text password matches the stored hash
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        public bool VerifyPassword(string password)
        {
            // Uses BCrypt to verify the password against the stored hash
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
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
            return new User("testuser", BCrypt.Net.BCrypt.HashPassword("password123"))
            {
                DisplayName = "Test User",
                Email = "test@example.com",
                DateCreated = DateTime.UtcNow.AddDays(-30),
                LastLogin = DateTime.UtcNow.AddHours(-2),
                IsActive = true,
                PreferredTheme = "System"
            };
        }
    }
}
