using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;

namespace NexusChat.Views.Controls
{
    public partial class ThinkingIndicator : ContentView
    {
        private bool _isAnimating = false;
        private bool _isDisposed = false;
        private const int ANIMATION_DURATION = 500; // milliseconds

        public static readonly BindableProperty IsActiveProperty =
            BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(ThinkingIndicator), false,
                propertyChanged: OnIsActiveChanged);

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
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

        private void StartAnimationLoop()
        {
            if (_isAnimating || _isDisposed) return;
            
            _isAnimating = true;
            Debug.WriteLine("Starting thinking animation");

            // Use a more efficient single animation approach
            MainThread.BeginInvokeOnMainThread(async () => {
                try
                {
                    // Keep track of animation state in a local variable
                    bool isActive = IsActive;
                    bool localAnimating = _isAnimating;
                    
                    // Stop if we're no longer animating or active
                    if (!isActive || !localAnimating || _isDisposed) return;
                    
                    // Animation cycle
                    while (isActive && localAnimating && !_isDisposed && IsActive && _isAnimating)
                    {
                        // Animate each dot with a time offset
                        AnimateDotOnce(Dot1, 0);
                        AnimateDotOnce(Dot2, 150);
                        AnimateDotOnce(Dot3, 300);
                        
                        // Wait for one complete cycle
                        await Task.Delay(750);
                        
                        // Update our tracking variables
                        isActive = IsActive;
                        localAnimating = _isAnimating;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in animation loop: {ex.Message}");
                    _isAnimating = false;
                }
            });
        }

        private async void AnimateDotOnce(Ellipse dot, int delayMs)
        {
            if (dot == null || _isDisposed) return;
            
            try
            {
                // Initial delay
                if (delayMs > 0)
                    await Task.Delay(delayMs);
                
                if (_isDisposed || !_isAnimating || !IsActive) return;
                
                // Simple up and down animation 
                await dot.TranslateTo(0, -5, 250, Easing.SinOut);
                
                if (_isDisposed || !_isAnimating || !IsActive) return;
                
                await dot.TranslateTo(0, 0, 250, Easing.SinIn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AnimateDotOnce error: {ex.Message}");
            }
        }
        
        private void StopAnimationLoop()
        {
            _isAnimating = false;
            Debug.WriteLine("Stopping thinking animation");
            
            // Reset dots to original position immediately
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (!_isDisposed)
                    {
                        if (Dot1 != null) 
                        {
                            Dot1.CancelAnimations();
                            Dot1.TranslationY = 0;
                        }
                        
                        if (Dot2 != null) 
                        {
                            Dot2.CancelAnimations();
                            Dot2.TranslationY = 0;
                        }
                        
                        if (Dot3 != null) 
                        {
                            Dot3.CancelAnimations();
                            Dot3.TranslationY = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error resetting dots: {ex.Message}");
                }
            });
        }
        
        protected override void OnParentSet()
        {
            base.OnParentSet();
            
            // Start animation if we're active and parent is set
            if (Parent != null && IsActive && !_isAnimating)
            {
                StartAnimationLoop();
            }
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);
            
            if (args.NewHandler == null)
            {
                Cleanup();
            }
        }
        
        private void Cleanup()
        {
            _isDisposed = true;
            _isAnimating = false;
            
            // Explicitly reset all animations
            MainThread.BeginInvokeOnMainThread(() => {
                try
                {
                    Dot1?.CancelAnimations();
                    Dot2?.CancelAnimations();
                    Dot3?.CancelAnimations();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning up animations: {ex.Message}");
                }
            });
        }
    }
}
