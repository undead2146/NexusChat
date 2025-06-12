using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;
namespace NexusChat.Views.Controls
{
    public partial class ThinkingIndicator : ContentView
    {
        private bool _isAnimating = false;
        private bool _isDisposed = false;
        private const int ANIMATION_DURATION = 500; // milliseconds

        // Primary property - use this as the main control mechanism
        public static readonly BindableProperty IsActiveProperty =
            BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(ThinkingIndicator), false,
                propertyChanged: OnIsActiveChanged);

        // Legacy property - keep for backward compatibility
        public static readonly BindableProperty IsAnimatingProperty =
            BindableProperty.Create(nameof(IsAnimating), typeof(bool), typeof(ThinkingIndicator), false,
                propertyChanged: OnIsAnimatingChanged);

        /// <summary>
        /// Gets or sets whether the indicator is active (primary property)
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the indicator is animating (alias for IsActive)
        /// </summary>
        /// <remarks>
        /// This property is provided for backward compatibility.
        /// Use <see cref="IsActive"/> for new code.
        /// </remarks>
        public bool IsAnimating
        {
            get => (bool)GetValue(IsAnimatingProperty);
            set => SetValue(IsAnimatingProperty, value);
        }

        public ThinkingIndicator()
        {
            try
            {
                InitializeComponent();
                
                // Start animation when control is created if it's active
                if (IsActive)
                {
                    StartAnimationLoop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ThinkingIndicator: {ex}");
            }
        }

        private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ThinkingIndicator control && oldValue != newValue)
            {
                var isActive = (bool)newValue;
                
                // Keep IsAnimating in sync with IsActive
                if (control.IsAnimating != isActive)
                {
                    control.IsAnimating = isActive;
                }
                
                MainThread.BeginInvokeOnMainThread(() => {
                    try
                    {
                        if (isActive)
                        {
                            control.StartAnimationLoop();
                        }
                        else
                        {
                            control.StopAnimationLoop();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error toggling animation: {ex}");
                    }
                });
            }
        }
        
        private static void OnIsAnimatingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ThinkingIndicator control && oldValue != newValue)
            {
                var isAnimating = (bool)newValue;
                
                // Keep IsActive in sync with IsAnimating
                if (control.IsActive != isAnimating)
                {
                    control.IsActive = isAnimating;
                }
                
                // The actual animation handling happens in OnIsActiveChanged
            }
        }
        
        private bool _animationRunning = false;
        
        public void StartAnimationLoop()
        {
            if (_animationRunning || _isDisposed)
                return;
                
            _animationRunning = true;
            _isAnimating = true;
            
            // Start animation loops for all dots
            AnimateDot(Dot1, 0);
            AnimateDot(Dot2, ANIMATION_DURATION / 3);
            AnimateDot(Dot3, (ANIMATION_DURATION / 3) * 2);
        }
        
        public void StopAnimationLoop()
        {
            _animationRunning = false;
            _isAnimating = false;
            
            // Reset all dots
            ResetDot(Dot1);
            ResetDot(Dot2);
            ResetDot(Dot3);
        }
        
        private async void AnimateDot(Ellipse dot, int delay)
        {
            if (_isDisposed) return;
            
            try
            {
                if (delay > 0)
                    await System.Threading.Tasks.Task.Delay(delay);

                while (_animationRunning && !_isDisposed)
                {
                    // Scale up
                    await dot.ScaleTo(1.5, ANIMATION_DURATION / 2, Easing.SinOut);
                    
                    // Scale down
                    await dot.ScaleTo(1.0, ANIMATION_DURATION / 2, Easing.SinIn);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }
        
        private void ResetDot(Ellipse dot)
        {
            try
            {
                dot.Scale = 1.0;
                dot.AbortAnimation("ScaleTo");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting dot: {ex.Message}");
            }
        }
        
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
            // Start animation if control is created in active state
            if (IsActive && Handler != null)
            {
                StartAnimationLoop();
            }
        }
        
        protected  void OnDisappearing()
        {
            _animationRunning = false;
        }
    }
}
