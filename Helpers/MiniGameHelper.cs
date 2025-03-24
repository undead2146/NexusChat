using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Diagnostics;
using Microsoft.Maui.Layouts;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for a simple combo-based minigame
    /// </summary>
    public class MiniGameHelper : IDisposable
    {
        // Event to notify when combo changes
        public event Action<int> OnComboChanged;

        // Basic game state (mutable)
        private int _count, _combo, _maxCombo;
        private bool _isActive;
        private DateTime _lastClickTime;
        
        // UI elements (truly immutable after construction)
        private readonly Button _gameButton;
        private readonly Grid _parentGrid;
        
        // Mutable UI elements and controls
        private AbsoluteLayout _particleContainer;
        private Timer _comboTimer;
        private CancellationTokenSource _animationCts;
        
        // Constants
        private const int COMBO_TIMEOUT_MS = 1500;
        private const double MAX_SCALE = 2.25;
        private const int MAX_STARS = 10;
        private const string INITIAL_BUTTON_TEXT = "Click Me!";
        private int _highScore;
        private double _comboMultiplier = 1.0;
        private readonly Random _random = new Random();
        private readonly string[] _encouragements = new[] {
            "Nice!", "Great!", "Awesome!", "Perfect!", "Amazing!", 
            "Fantastic!", "Incredible!", "Unstoppable!", "On Fire!", "Legendary!"
        };

        /// <summary>
        /// Initializes a new instance of the MiniGameHelper class
        /// </summary>
        public MiniGameHelper(Button button, Grid parentGrid)
        {
            try
            {
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
                    
                    // Create new container as AbsoluteLayout to allow precise positioning
                    _particleContainer = new AbsoluteLayout
                    {
                        InputTransparent = true,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        ZIndex = 999
                    };
                    
                    // Add the particle container to the grid and ensure it spans all rows/columns
                    Grid.SetColumnSpan(_particleContainer, _parentGrid.ColumnDefinitions.Count > 0 ? 
                        _parentGrid.ColumnDefinitions.Count : 1);
                    Grid.SetRowSpan(_particleContainer, _parentGrid.RowDefinitions.Count > 0 ? 
                        _parentGrid.RowDefinitions.Count : 1);
                    Grid.SetRow(_particleContainer, 0);
                    Grid.SetRowSpan(_particleContainer, 3); // Make sure it covers the entire grid
                    
                    // Insert above content so particles are visible
                    _parentGrid.Children.Add(_particleContainer);
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
                
                // Button color with more vibrant colors based on combo
                Color buttonColor;
                if (_combo <= 1)
                    buttonColor = Colors.DodgerBlue;
                else if (_combo <= 5)
                    buttonColor = HueShift(Colors.Purple, (_combo * 15) % 360);
                else if (_combo <= 10)
                    buttonColor = HueShift(Colors.Orange, (_combo * 20) % 360);
                else
                    buttonColor = HueShift(Colors.Red, (_combo * 25) % 360);
                
                _gameButton.BackgroundColor = buttonColor;
                _gameButton.BorderColor = buttonColor;
                _gameButton.TextColor = Colors.White;

                // Add shadow effect for higher combos
                if (_combo > 5)
                {
                    var shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(buttonColor),
                        Offset = new Point(0, 2),
                        Opacity = 0.7f,
                        Radius = Math.Min(5 + (_combo / 2), 15)
                    };
                    _gameButton.Shadow = shadow;
                }
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
                _comboMultiplier = Math.Min(1 + (_combo * 0.1), 3.0); // Max 3x multiplier
                
                // Notify subscribers of combo change
                OnComboChanged?.Invoke(_maxCombo);
                
                _comboTimer.Stop();
                _comboTimer.Start();
            }
            else
            {
                _combo = 1;
                _comboMultiplier = 1.0;
                _comboTimer.Start();
            }
        }
        
        private void ResetCombo()
        {
            if (_combo > 0)
            {
                _combo = 0;
                _comboMultiplier = 1.0;
                UpdateButtonText();
                _gameButton.BackgroundColor = Colors.DodgerBlue;
                _gameButton.Shadow = null;
            }
        }
        
        private void UpdateButtonText()
        {
            var text = $"{_count} clicks";
            
            // Add combo display with multiplier for higher combos
            if (_combo > 1)
            {
                text += $" (x{_combo})";
                
                // Show multiplier for higher combos
                if (_combo >= 5)
                {
                    text += $"\n{_comboMultiplier:F1}x points";
                }
            }
            
            _gameButton.Text = text;
        }

        private async void SpawnStars()
        {
            try 
            {
                if (_particleContainer == null) return;
                
                // Get the absolute position of the button in the window
                var buttonBounds = GetElementBounds(_gameButton);
                // Calculate center point of the button
                Point center = new Point(
                    buttonBounds.X + (buttonBounds.Width / 2),
                    buttonBounds.Y + (buttonBounds.Height / 2)
                );
                
                // Limit stars based on combo
                int starCount = Math.Min(_combo, MAX_STARS);
                string[] starChars = { "★", "✨", "🌟" };
                
                // Add more star variety for higher combos
                if (_combo > 5) 
                {
                    starChars = new[] { "★", "✨", "🌟", "💫", "⭐" };
                }
                
                for (int i = 0; i < starCount; i++)
                {
                    // Create star at button's current location
                    var star = CreateStarLabel(starChars[i % starChars.Length], center, i);
                    
                    // Add to particle container with improved positioning
                    AbsoluteLayout.SetLayoutFlags(star, AbsoluteLayoutFlags.None);
                    AbsoluteLayout.SetLayoutBounds(star, new Rect(
                        center.X - (star.FontSize / 2), // Center based on font size
                        center.Y - (star.FontSize / 2),
                        star.FontSize * 1.5, // Make hitbox larger than font
                        star.FontSize * 1.5
                    ));
                        
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

        private Rect GetElementBounds(VisualElement element)
        {
            try
            {
                // Get the element's bounds within its parent
                double x = element.X;
                double y = element.Y;
                
                // Get element's dimensions
                double width = element.Width;
                double height = element.Height;
                
                // Traverse up the visual tree to get absolute coordinates
                VisualElement current = element;
                while (current.Parent is VisualElement parent && parent is not Page)
                {
                    // Add parent position
                    x += parent.X;
                    y += parent.Y;
                    
                    // Account for padding/margin where appropriate
                    if (parent is Layout layout)
                    {
                        x += layout.Padding.Left;
                        y += layout.Padding.Top;
                    }
                    
                    current = parent;
                }
                
                return new Rect(x, y, width, height);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting element bounds: {ex.Message}");
                // Return approximate position as fallback
                return new Rect(element.X, element.Y, element.Width, element.Height);
            }
        }

        private Label CreateStarLabel(string text, Point center, int index)
        {
            // Vary the color based on combo and index
            Color starColor;
            if (_combo > 10)
                starColor = HueShift(Colors.Gold, index * 20);
            else if (_combo > 5)
                starColor = HueShift(Colors.Orange, index * 25);
            else
                starColor = HueShift(Colors.Yellow, index * 30);
                
            return new Label
            {
                Text = text,
                FontSize = 24 + (_combo * 0.5), // Make stars larger with higher combo
                TextColor = starColor,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Opacity = 0.9,
                Scale = 0.1, // Start small
                Rotation = Random.Shared.Next(360),
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent,
                // Add shadow for better visibility
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Offset = new Point(0, 1),
                    Radius = 2,
                    Opacity = 0.3f
                }
            };
        }

        private async Task AnimateStarAndRemove(Label star, Point center)
        {
            try
            {
                // Random trajectory with more interesting patterns
                double angle = Random.Shared.Next(360) * Math.PI / 180;
                double distance = Random.Shared.Next(100, 300);
                double destX = Math.Cos(angle) * distance;
                double destY = Math.Sin(angle) * distance;
                uint duration = (uint)Random.Shared.Next(800, 1200);
                
                // Improved animation sequence
                await star.ScaleTo(1.5, 150, Easing.SpringOut);
                
                // Add rotation for higher combos
                if (_combo > 5)
                {
                    _ = star.RotateTo(Random.Shared.Next(-360, 360), duration, Easing.CubicInOut);
                }
                
                // Animate outward with better easing
                Easing moveEasing = _combo > 8 ? Easing.BounceOut : Easing.CubicOut;
                
                await Task.WhenAll(
                    star.TranslateTo(destX, destY, duration, moveEasing),
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

        // Color utility for visual effects
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
                _count = _combo = 0;
                _isActive = false;
                
                MainThread.BeginInvokeOnMainThread(() => {
                    _gameButton.Text = INITIAL_BUTTON_TEXT;
                    _gameButton.BackgroundColor = null;
                    _gameButton.BorderColor = null;
                    _gameButton.TextColor = null;
                    _gameButton.Shadow = null;
                    
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
