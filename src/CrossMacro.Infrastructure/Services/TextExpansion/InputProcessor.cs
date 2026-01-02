using System;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Core.Services.TextExpansion;
using Serilog;

namespace CrossMacro.Infrastructure.Services.TextExpansion
{
    public class InputProcessor : IInputProcessor
    {
        private readonly IKeyboardLayoutService _layoutService;
        
        // Modifier state
        private bool _isLeftShiftPressed;
        private bool _isRightShiftPressed;
        private bool _isRightAltPressed; // AltGr
        private bool _isLeftAltPressed;
        private bool _isLeftCtrlPressed;
        private bool _isRightCtrlPressed; // Not used for char mapping directly usually, but good to track
        private bool _isCapsLockOn;
        private bool _isAltGrPressed; // Computed

        // Debouncing state
        private int _lastKey;
        private long _lastPressTime;
        private const long DebounceTicks = 20 * 10000; // 20ms in ticks

        public event Action<char>? CharacterReceived;
        public event Action<int>? SpecialKeyReceived;

        public bool AreModifiersPressed => 
            _isLeftShiftPressed || _isRightShiftPressed || 
            _isLeftAltPressed || _isRightAltPressed ||
            _isLeftCtrlPressed || _isRightCtrlPressed;

        public InputProcessor(IKeyboardLayoutService layoutService)
        {
            _layoutService = layoutService;
        }

        public void ProcessEvent(InputCaptureEventArgs e)
        {
            // Only process key events
            if (e.Type != InputEventType.Key) return;

            // Update Modifier State
            UpdateModifiers(e);

            // Toggle CapsLock on Press
            if (e.Code == 58 && e.Value == 1) // Caps Lock Press
            {
                _isCapsLockOn = !_isCapsLockOn;
                return;
            }

            // Only process key PRESS (value == 1) for actual typing logic
            if (e.Value != 1) return;

            // Debouncing check
            long now = DateTime.UtcNow.Ticks;
            if (e.Code == _lastKey && (now - _lastPressTime) < DebounceTicks)
            {
                return;
            }
            _lastKey = e.Code;
            _lastPressTime = now;

            // Check for Special Keys first
            if (e.Code == 14) // Backspace
            {
                SpecialKeyReceived?.Invoke(e.Code);
                return;
            }
            if (e.Code == 28) // Enter
            {
                SpecialKeyReceived?.Invoke(e.Code);
                // Enter might also produce a char (newline), but usually clears buffer
                return; 
            }
            
            // Map key to char
            var charValue = _layoutService.GetCharFromKeyCode(e.Code,
                _isLeftShiftPressed, _isRightShiftPressed,
                _isRightAltPressed, _isLeftAltPressed, _isLeftCtrlPressed, _isCapsLockOn);

            if (charValue.HasValue)
            {
                CharacterReceived?.Invoke(charValue.Value);
            }
            else if (e.Code == 57) // Space
            {
                // Explicitly handle space if layout service returns null for it (it shouldn't, but safe fallback)
                // Or if layout service handles it, the above block catches it. 
                // Let's assume layout service handles space, but if not:
                 var spaceChar = _layoutService.GetCharFromKeyCode(57, false, false, false, false, false, false);
                 if (spaceChar.HasValue)
                 {
                     CharacterReceived?.Invoke(spaceChar.Value);
                 }
            }
        }

        private void UpdateModifiers(InputCaptureEventArgs e)
        {
            // Value 1 = Press, 2 = Repeat, 0 = Release.
            // We consider it pressed if 1 or 2.
            bool isPressed = e.Value == 1 || e.Value == 2;

            switch (e.Code)
            {
                case 42: _isLeftShiftPressed = isPressed; break;
                case 54: _isRightShiftPressed = isPressed; break;
                case 100: 
                    _isRightAltPressed = isPressed;
                    _isAltGrPressed = _isRightAltPressed; // Treat Right Alt as AltGr
                    break;
                case 56: _isLeftAltPressed = isPressed; break;
                case 29: _isLeftCtrlPressed = isPressed; break;
                case 97: _isRightCtrlPressed = isPressed; break;
            }
        }

        public void Reset()
        {
            _isLeftShiftPressed = false;
            _isRightShiftPressed = false;
            _isRightAltPressed = false;
            _isLeftAltPressed = false;
            _isLeftCtrlPressed = false;
            _isRightCtrlPressed = false;
            _isAltGrPressed = false;
            // _isCapsLockOn ? Usually persistent, don't reset caps lock
            _lastKey = 0;
            _lastPressTime = 0;
        }
    }
}
