using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SQLite;
using MaxLengthAttribute = SQLite.MaxLengthAttribute;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents a user in the application
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
        /// Username for authentication
        /// </summary>
        [MaxLength(50), Unique]
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }
        
        /// <summary>
        /// Hashed password
        /// </summary>
        public string PasswordHash { get; set; }
        
        /// <summary>
        /// User's display name
        /// </summary>
        [MaxLength(100)]
        public string DisplayName { get; set; }
        
        /// <summary>
        /// User's email address
        /// </summary>
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; }
        
        /// <summary>
        /// Path to the user's avatar image
        /// </summary>
        [MaxLength(255)]
        public string AvatarPath { get; set; }
        
        /// <summary>
        /// Preferred theme for the user interface
        /// </summary>
        public string PreferredTheme { get; set; } = "System";
        
        /// <summary>
        /// Preferred AI model ID
        /// </summary>
        public int PreferredModelId { get; set; }
        
        /// <summary>
        /// When the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Alias for CreatedAt to maintain backward compatibility
        /// </summary>
        public DateTime DateCreated 
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }
        
        /// <summary>
        /// When the user last logged in
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public User() { }
        
        /// <summary>
        /// Constructor with basic fields
        /// </summary>
        public User(string username, string displayName = null)
        {
            Username = username;
            DisplayName = displayName ?? username;
            CreatedAt = DateTime.UtcNow;
        }
        

        
        /// <summary>
        /// Extended constructor for all fields
        /// </summary>
        public User(
            string username, 
            string displayName, 
            string email, 
            string avatarPath, 
            string preferredTheme, 
            int preferredModelId, 
            DateTime? createdAt = null)
        {
            Username = username;
            DisplayName = displayName ?? username;
            Email = email;
            AvatarPath = avatarPath;
            PreferredTheme = preferredTheme ?? "System";
            PreferredModelId = preferredModelId;
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }
        
        /// <summary>
        /// Extended constructor for all fields - matches test expectations
        /// </summary>
        public User(
            string username, 
            string passwordHash, 
            string displayName, 
            string avatarPath, 
            string email, 
            string preferredTheme, 
            int preferredModelId, 
            DateTime? createdAt = null)
        {
            Username = username;
            PasswordHash = passwordHash;
            DisplayName = displayName ?? username;
            AvatarPath = avatarPath;
            Email = email;
            PreferredTheme = preferredTheme ?? "System";
            PreferredModelId = preferredModelId;
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }
        
        /// <summary>
        /// Validates user properties
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate()
        {
            return Validate(out _);
        }
        
        /// <summary>
        /// Validates user properties with detailed error message
        /// </summary>
        /// <param name="errorMessage">Output error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out string errorMessage)
        {
            errorMessage = null;
            
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
            
            if (Username.Length > 50)
            {
                errorMessage = "Username cannot exceed 50 characters";
                return false;
            }
            
            // Check for valid characters in username
            foreach (char c in Username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                {
                    errorMessage = "Username can only contain letters, numbers, underscores, hyphens, and periods";
                    return false;
                }
            }
            
            if (!string.IsNullOrEmpty(Email) && !IsValidEmail(Email))
            {
                errorMessage = "Invalid email address format";
                return false;
            }
            
            // Validate theme
            if (!string.IsNullOrEmpty(PreferredTheme) && 
                PreferredTheme != "Light" && 
                PreferredTheme != "Dark" && 
                PreferredTheme != "System")
            {
                errorMessage = "Theme must be Light, Dark, or System";
                return false;
            }
                
            return true;
        }
        
        /// <summary>
        /// Verifies a password against the stored hash
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(PasswordHash))
                return false;
                
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }
        
        /// <summary>
        /// Sets the password for this user by hashing it
        /// </summary>
        /// <param name="password">Plain text password to hash and store</param>
        public void SetPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));
                
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
        
        /// <summary>
        /// Validates an email address format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Creates a test user with pre-hashed password
        /// </summary>
        /// <returns>A test user instance</returns>
        public static User CreateTestUser()
        {
            return new User
            {
                Username = "testuser",
                DisplayName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                PreferredTheme = "System",
                AvatarPath = "/images/default-avatar.png",
                PreferredModelId = 1
            };
        }
        
        /// <summary>
        /// Creates multiple test users for development purposes
        /// </summary>
        /// <param name="count">Number of test users to create</param>
        /// <returns>List of test users</returns>
        public static List<User> CreateTestUsers(int count = 5)
        {
            var users = new List<User>();
            
            for (int i = 0; i < count; i++)
            {
                users.Add(new User
                {
                    Username = $"testuser{i+1}",
                    DisplayName = $"Test User {i+1}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword($"password{i+1}"),
                    Email = $"test{i+1}@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    LastLoginAt = i % 2 == 0 ? DateTime.UtcNow.AddHours(-i) : null,
                    PreferredTheme = "System",
                    AvatarPath = $"/images/avatar{i+1}.png",
                    PreferredModelId = i + 1
                });
            }
            
            return users;
        }
    }
}
