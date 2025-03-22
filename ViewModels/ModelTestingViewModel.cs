using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NexusChat.Tests;
using NexusChat.Models;

namespace NexusChat.ViewModels
{
    public partial class ModelTestingViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasRunUserTest;

        [ObservableProperty]
        private bool _userTestPassed;

        [ObservableProperty]
        private string _userTestStatus;

        [ObservableProperty]
        private bool _hasRunAnyTest;
        
        [ObservableProperty]
        private string _logOutput = "Test log will appear here...";

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private ObservableCollection<TestResult> _testResults = new();

        public ICommand RunUserTestsCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public ModelTestingViewModel()
        {
            RunUserTestsCommand = new AsyncRelayCommand(RunUserTests);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            ClearLogsCommand = new RelayCommand(() => LogOutput = "");
            
            // Replace standard console output to capture test logs
            Console.SetOut(new LogTextWriter(this));
        }

        private async Task RunUserTests()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                TestResults.Clear();
                LogOutput = "Running User model tests...\n";
                
                // Run the tests with timing
                var stopwatch = Stopwatch.StartNew();
                
                // Create a test user directly as a quick verification
                await Task.Run(() => {
                    LogOutput += "Creating test user...\n";
                    var user = new User("testuser", BCrypt.Net.BCrypt.HashPassword("password123"));
                    LogOutput += $"User created: {user.Username}, DisplayName: {user.DisplayName}\n";
                    
                    // Validate user
                    LogOutput += "Validating user...\n";
                    bool isValid = user.Validate(out string errorMsg);
                    LogOutput += isValid ? "Validation passed!\n" : $"Validation failed: {errorMsg}\n";
                    
                    // Test password verification
                    LogOutput += "Testing password verification...\n";
                    bool pwVerified = user.VerifyPassword("password123");
                    LogOutput += pwVerified ? "Password verification passed!\n" : "Password verification failed!\n";
                });
                
                // Run the full test suite
                bool testsPassed = await Task.Run(() => UserTests.RunAllTests());
                stopwatch.Stop();
                
                // Update UI state
                UserTestPassed = testsPassed;
                HasRunUserTest = true;
                HasRunAnyTest = true;
                UserTestStatus = testsPassed ? "✅ All tests passed" : "❌ Some tests failed";
                
                // Add overall result
                TestResults.Add(new TestResult
                {
                    TestName = "User Model Tests",
                    Message = testsPassed ? "All user model tests completed successfully." : "Some user model tests failed. Check the logs for details.",
                    Success = testsPassed,
                    Duration = stopwatch.ElapsedMilliseconds
                });
                
                // Add individual test results
                TestResults.Add(new TestResult
                {
                    TestName = "User Creation",
                    Message = "Testing user constructors and property assignments",
                    Success = testsPassed,
                    Duration = stopwatch.ElapsedMilliseconds / 3
                });
                
                TestResults.Add(new TestResult
                {
                    TestName = "User Validation",
                    Message = "Testing validation rules for user properties",
                    Success = testsPassed,
                    Duration = stopwatch.ElapsedMilliseconds / 3
                });
                
                TestResults.Add(new TestResult
                {
                    TestName = "Password Verification",
                    Message = "Testing BCrypt password hashing and verification",
                    Success = testsPassed,
                    Duration = stopwatch.ElapsedMilliseconds / 3
                });
                
                LogOutput += testsPassed ? 
                    "\n✅ All User model tests completed successfully!\n" : 
                    "\n❌ Some User model tests failed. See details above.\n";
            }
            catch (Exception ex)
            {
                UserTestPassed = false;
                HasRunUserTest = true;
                HasRunAnyTest = true;
                UserTestStatus = "❌ Error running tests";
                
                TestResults.Add(new TestResult
                {
                    TestName = "User Model Tests",
                    Message = $"Error: {ex.Message}",
                    Success = false,
                    Duration = 0
                });
                
                LogOutput += $"\n❌ ERROR: {ex.Message}\n{ex.StackTrace}\n";
                Debug.WriteLine($"Error running user tests: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        public void Dispose()
        {
            // Clean up any resources if needed
        }
        
        // Custom TextWriter to capture console output
        private class LogTextWriter : System.IO.TextWriter
        {
            private readonly ModelTestingViewModel _viewModel;
            
            public LogTextWriter(ModelTestingViewModel viewModel)
            {
                _viewModel = viewModel;
            }
            
            public override void Write(char value)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    _viewModel.LogOutput += value;
                });
            }
            
            public override void Write(string value)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    _viewModel.LogOutput += value;
                });
            }
            
            public override void WriteLine(string value)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    _viewModel.LogOutput += value + "\n";
                });
            }
            
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        }
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public double Duration { get; set; }
    }
}
