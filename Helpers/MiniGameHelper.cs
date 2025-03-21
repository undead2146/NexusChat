using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Diagnostics;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for a simple combo-based minigame
    /// </summary>
    public class MiniGameHelper : IDisposable
    {
        // Basic game state (mutable)
        private int _count, _combo, _maxCombo;
        private bool _isActive;
        private DateTime _lastClickTime;
        
        // UI elements (truly immutable after construction)
        private readonly Button _gameButton;
        private readonly Grid _parentGrid;
        
        // Mutable UI elements and controls
        private Grid _particleContainer;
        private Timer _comboTimer;
        private CancellationTokenSource _animationCts;
        
        // Constants
        private const int COMBO_TIMEOUT_MS = 1500;
        private const double MAX_SCALE = 1.25;
        private const int MAX_STARS = 5;
        private const string INITIAL_BUTTON_TEXT = "Click Me!";

        /// <summary>
        /// Initializes a new instance of the MiniGameHelper class
        /// </summary>
        public MiniGameHelper(Button button, Grid parentGrid)
        {
            try
            {
                // Store references first
                _gameButton = button;
                _parentGrid = parentGrid;
                InitializeGame();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize game: {ex.Message}");
                
                // Set a fallback state even if initialization fails
                _count = _combo = _maxCombo = 0;
                _isActive = false;
                _animationCts = new CancellationTokenSource();
            }
        }
        
        private void InitializeGame()
        {
            // Initialize fields
            _count = _combo = _maxCombo = 0;
            _isActive = false;
            _animationCts = new CancellationTokenSource();
            
            // Reset button appearance
            MainThread.BeginInvokeOnMainThread(() => {
                _gameButton.Text = INITIAL_BUTTON_TEXT;
                _gameButton.BackgroundColor = null;
                _gameButton.BorderColor = null;
                _gameButton.TextColor = null;
            });
            
            // Set up combo timer
            _comboTimer = new Timer(COMBO_TIMEOUT_MS) { AutoReset = false };
            _comboTimer.Elapsed += (_, _) => MainThread.BeginInvokeOnMainThread(ResetCombo);
            
            // Create and add particle container
            CreateParticleContainer();
        }
        
        private void CreateParticleContainer()
        {
            MainThread.BeginInvokeOnMainThread(() => {
                try
                {
                    // Remove any existing container first
                    if (_particleContainer != null && 
                        _parentGrid.Children.Contains(_particleContainer))
                    {
                        _parentGrid.Children.Remove(_particleContainer);
                    }
                    
                    // Create new container
                    _particleContainer = new Grid
                    {
                        InputTransparent = true,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill
                    };
                    
                    // Insert at the bottom of z-order
                    _parentGrid.Children.Insert(0, _particleContainer);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating particle container: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Handles button click for the minigame
        /// </summary>
        public async Task HandleClick()
        {
            try
            {
                // First click initializes game
                if (!_isActive)
                {
                    _isActive = true;
                    _count = _combo = _maxCombo = 0;
                    CreateParticleContainer(); // Ensure container exists
                }
                
                // Cancel any running animations
                _animationCts.Cancel();
                _animationCts = new CancellationTokenSource();
                
                // Update game state
                _count++;
                UpdateCombo();
                UpdateButtonText();
                
                // Visual effects
                if (_combo > 1) 
                    SpawnStars();
                
                // Button animations
                double scale = Math.Min(1.0 + (_combo * 0.03), MAX_SCALE);
                await _gameButton.ScaleTo(scale, 100, Easing.SpringOut);
                await _gameButton.ScaleTo(1.0, 200, Easing.SpringOut);
                
                // Button color
                _gameButton.BackgroundColor = _combo <= 1 ? 
                    Colors.DodgerBlue : 
                    HueShift(Colors.Purple, (_combo * 15) % 360);
                _gameButton.BorderColor = _gameButton.BackgroundColor;
                _gameButton.TextColor = Colors.White;
            }
            catch (TaskCanceledException)
            {
                // Expected during rapid clicks
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Game error: {ex.Message}");
            }
        }

        private void UpdateCombo()
        {
            // Calculate combo based on click speed
            DateTime now = DateTime.Now;
            bool quickClick = (now - _lastClickTime).TotalMilliseconds < 500;
            _lastClickTime = now;
            
            if (quickClick)
            {
                _combo++;
                _maxCombo = Math.Max(_maxCombo, _combo);
                _comboTimer.Stop();
                _comboTimer.Start();
            }
            else
            {
                _combo = 1;
                _comboTimer.Start();
            }
        }
        
        private void ResetCombo()
        {
            if (_combo > 0)
            {
                _combo = 0;
                UpdateButtonText();
                _gameButton.BackgroundColor = Colors.DodgerBlue;
            }
        }
        
        private void UpdateButtonText()
        {
            var text = $"{_count} clicks";
            if (_combo > 1) text += $" (x{_combo})";
            _gameButton.Text = text;
        }

        private async void SpawnStars()
        {
            try 
            {
                if (_particleContainer == null) return;
                
                // Get button center position
                var btnRect = GetElementBounds(_gameButton);
                var center = new Point(btnRect.X + btnRect.Width/2, btnRect.Y + btnRect.Height/2);
                
                // Limit stars based on combo
                int starCount = Math.Min(_combo, MAX_STARS);  // Fixed Math.min to Math.Min
                string[] starChars = { "★", "✨", "🌟" };
                
                for (int i = 0; i < starCount; i++)
                {
                    var star = CreateStarLabel(starChars[i % starChars.Length], center, i);
                    _particleContainer.Children.Add(star);
                    
                    // Animate and remove
                    await AnimateStarAndRemove(star, center);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Star effect error: {ex.Message}");
            }
        }
        
        private Label CreateStarLabel(string text, Point center, int index)
        {
            return new Label
            {
                Text = text,
                FontSize = 14 + (_combo * 0.3),
                TextColor = HueShift(Colors.Yellow, index * 30),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TranslationX = center.X,
                TranslationY = center.Y,
                Opacity = 0.8,
                Rotation = Random.Shared.Next(360)
            };
        }
        
        private async Task AnimateStarAndRemove(Label star, Point center)
        {
            try
            {
                // Calculate random trajectory
                double angle = Random.Shared.Next(360) * Math.PI / 180;
                double distance = Random.Shared.Next(50, 150);
                double destX = center.X + Math.Cos(angle) * distance;
                double destY = center.Y + Math.Sin(angle) * distance;
                uint duration = (uint)Random.Shared.Next(400, 700);
                
                // Animate
                await Task.WhenAll(
                    star.TranslateTo(destX, destY, duration, Easing.CubicOut),
                    star.ScaleTo(0, duration, Easing.CubicIn),
                    star.FadeTo(0, duration)
                );
                
                // Remove safely
                if (_particleContainer != null && 
                    _particleContainer.Children.Contains(star))
                {
                    _particleContainer.Children.Remove(star);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating star: {ex.Message}");
            }
        }
        
        // Helper method for getting element bounds
        private Rect GetElementBounds(VisualElement element)
        {
            double x = 0, y = 0;
            var current = element;
            
            while (current != null)
            {
                x += current.X;
                y += current.Y;
                
                if (current is Layout layout)
                {
                    x += layout.Padding.Left;
                    y += layout.Padding.Top;
                }
                
                current = current.Parent as VisualElement;
            }
            
            return new Rect(x, y, element.Width, element.Height);
        }

        // Helper method to shift color hue
        private Color HueShift(Color baseColor, double hueShiftDegrees)
        {
            float r = (float)baseColor.Red;
            float g = (float)baseColor.Green;
            float b = (float)baseColor.Blue;

            // Find min/max RGB components
            float max = Math.Max(Math.Max(r, g), b);
            float min = Math.Min(Math.Min(r, g), b);
            float h, s, l = (max + min) / 2;
            
            if (max == min)
            {
                h = s = 0; // achromatic (gray)
            }
            else
            {
                float d = max - min;
                s = l > 0.5f ? d / (2 - max - min) : d / (max + min);
                
                // Calculate hue
                if (max == r)
                    h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g)
                    h = (b - r) / d + 2;
                else
                    h = (r - g) / d + 4;
                
                h /= 6;
            }

            // Apply hue shift and wrap around 0-1
            h = (h + (float)(hueShiftDegrees / 360.0)) % 1.0f;
            
            // Convert back to RGB
            return Color.FromHsla(h, s, l);
        }
        
        /// <summary>
        /// Resets the game state completely
        /// </summary>
        public void Reset()
        {
            try
            {
                _count = _combo = _maxCombo = 0;
                _isActive = false;
                
                MainThread.BeginInvokeOnMainThread(() => {
                    _gameButton.Text = INITIAL_BUTTON_TEXT;
                    _gameButton.BackgroundColor = null;
                    _gameButton.BorderColor = null;
                    _gameButton.TextColor = null;
                    
                    // Clear any particles
                    if (_particleContainer != null)
                    {
                        _particleContainer.Children.Clear();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting game: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disposes resources used by the mini-game
        /// </summary>
        public void Dispose()
        {
            try
            {
                Reset();
                
                // Clean up timers
                _comboTimer?.Stop();
                _comboTimer?.Dispose();
                _comboTimer = null;
                
                // Clean up animations
                _animationCts?.Cancel();
                _animationCts?.Dispose();
                _animationCts = null;
                
                // Clean up UI
                MainThread.BeginInvokeOnMainThread(() => {
                    try
                    {
                        // Remove particle container
                        if (_particleContainer != null && 
                            _parentGrid != null && 
                            _parentGrid.Children != null &&
                            _parentGrid.Children.Contains(_particleContainer))
                        {
                            _parentGrid.Children.Remove(_particleContainer);
                        }
                        _particleContainer = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error removing particle container: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during MiniGameHelper disposal: {ex.Message}");
            }
        }
    }
}
