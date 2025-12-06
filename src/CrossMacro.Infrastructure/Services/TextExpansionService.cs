using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Infrastructure.Helpers;
using CrossMacro.Native.Evdev;
using CrossMacro.Native.UInput;
using Serilog;

namespace CrossMacro.Infrastructure.Services;

/// <summary>
/// Service for monitoring keystrokes and performing text expansion
/// </summary>
public class TextExpansionService : ITextExpansionService
{
    private readonly IKeyboardLayoutService _layoutService;
    private readonly ISettingsService _settingsService;
    private readonly IClipboardService _clipboardService;
    private readonly TextExpansionStorageService _storageService;
    private readonly List<EvdevReader> _readers = new();
    private readonly object _lock; // Use monitor for synchronous operations
    private bool _isRunning;
    
    // Buffer for tracking typed characters
    private readonly StringBuilder _buffer;
    private const int MaxBufferLength = 50; // Max supported trigger length
    
    // UInput device for injecting keystrokes
    private UInputDevice? _outputDevice;
    private readonly SemaphoreSlim _outputLock; // Use SemaphoreSlim for async operations

    // Modifier state
    private bool _isShiftPressed;
    private bool _isAltGrPressed;
    private bool _isCapsLockOn;

    public bool IsRunning => _isRunning;

    public TextExpansionService(
        ISettingsService settingsService, 
        IClipboardService clipboardService, 
        TextExpansionStorageService storageService,
        IKeyboardLayoutService layoutService)
    {
        _settingsService = settingsService;
        _clipboardService = clipboardService;
        _storageService = storageService;
        _layoutService = layoutService;
        _buffer = new StringBuilder();
        _lock = new object();
        _outputLock = new SemaphoreSlim(1, 1);
    }


    public void Start()
    {
        // ... (Start implementation remains same) ...
        // Only start if enabled in settings
        if (!_settingsService.Current.EnableTextExpansion)
        {
            Log.Information("[TextExpansionService] Not starting because feature is disabled in settings");
            return;
        }

        lock (_lock)
        {
            if (_isRunning) return;

            try
            {
                // Auto-detect keyboards
                var devices = InputDeviceHelper.GetAvailableDevices();
                var keyboards = devices.Where(d => d.IsKeyboard).ToList();

                if (keyboards.Count == 0)
                {
                    Log.Warning("[TextExpansionService] No keyboards found, cannot start monitoring");
                    return;
                }

                foreach (var kbd in keyboards)
                {
                    try
                    {
                        var reader = new EvdevReader(kbd.Path, kbd.Name);
                        reader.EventReceived += OnEventReceived;
                        reader.Start();
                        _readers.Add(reader);
                        Log.Information("[TextExpansionService] Monitoring {Name} for text expansion", kbd.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[TextExpansionService] Failed to open keyboard {Name}", kbd.Name);
                    }
                }

                if (_readers.Count > 0)
                {
                    _isRunning = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TextExpansionService] Failed to start service");
            }
        }
    }

    // ... Stop/Dispose ...

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            foreach (var reader in _readers)
            {
                reader.Stop();
                reader.Dispose();
            }
            _readers.Clear();
            
            try 
            {
                _outputLock.Wait();
                _outputDevice?.Dispose();
                _outputDevice = null;
            }
            finally
            {
                _outputLock.Release();
            }

            _isRunning = false;
            Log.Information("[TextExpansionService] Stopped");
        }
    }

    public void Dispose()
    {
        Stop();
        _outputLock.Dispose();
    }

    // Debouncing state
    private int _lastKey;
    private long _lastPressTime;
    private const long DebounceTicks = 20 * 10000; // 20ms in ticks

    private void OnEventReceived(EvdevReader sender, UInputNative.input_event ev)
    {
        // Only process key events
        if (ev.type != UInputNative.EV_KEY) return;

        lock (_lock)
        {
            // Update shift state
            if (ev.code == 42 || ev.code == 54) // Left/Right Shift
            {
                _isShiftPressed = ev.value == 1 || ev.value == 2;
                return;
            }
            // Update AltGr state (Right Alt = 100)
            if (ev.code == 100)
            {
                _isAltGrPressed = ev.value == 1 || ev.value == 2;
                return;
            }
            // Update CapsLock state (Toggle on Press)
            if (ev.code == 58 && ev.value == 1) // Caps Lock Press
            {
                _isCapsLockOn = !_isCapsLockOn;
                return;
            }

            // Only process key PRESS (value == 1)
            // Ignore Repeat (2) and Release (0)
            if (ev.value != 1) return;

            // Debouncing check
            // If same key is pressed very quickly, assume it's a duplicate event from dual interfaces
            long now = DateTime.UtcNow.Ticks;
            if (ev.code == _lastKey && (now - _lastPressTime) < DebounceTicks)
            {
               // Duplicate event, ignore
               return;
            }
            _lastKey = ev.code;
            _lastPressTime = now;

            // Map key to char using IKeyboardLayoutService (XKB aware)
            var charValue = _layoutService.GetCharFromKeyCode(ev.code, _isShiftPressed, _isAltGrPressed, _isCapsLockOn);

            // Manage buffer
            if (charValue.HasValue)
            {
                AppendToBuffer(charValue.Value);
                CheckForExpansion();
            }
            else if (ev.code == 14) // Backspace
            {
                // Remove last char from buffer if possible
                if (_buffer.Length > 0) _buffer.Length--;
            }
            else if (ev.code == 28 || ev.code == 57) // Enter or Space
            {
                 // Reset usage on Enter logic can stay
                 if (ev.code == 28) _buffer.Clear();
                 // Space (57) special handling
                 if (ev.code == 57)
                 {
                    var spaceChar = _layoutService.GetCharFromKeyCode(57, false, false, false);
                    if (spaceChar.HasValue) 
                    {
                        AppendToBuffer(spaceChar.Value);
                        CheckForExpansion();
                    }
                 }
            }
        }
    }

    private void AppendToBuffer(char c)
    {
        // Append char
        _buffer.Append(c);

        // Keep buffer size checked
        if (_buffer.Length > MaxBufferLength)
        {
            _buffer.Remove(0, _buffer.Length - MaxBufferLength);
        }
    }

    private void CheckForExpansion()
    {
        var currentText = _buffer.ToString();
        var expansions = _storageService.GetCurrent()
            .Where(e => e.IsEnabled && !string.IsNullOrEmpty(e.Trigger))
            .ToList();

        foreach (var expansion in expansions)
        {
            if (currentText.EndsWith(expansion.Trigger))
            {
                Log.Information("[TextExpansionService] Trigger detected: {Trigger} -> {Replacement}", expansion.Trigger, expansion.Replacement);
                
                // Perform expansion
                // 1. We must run this on a separate task to not block the input reader callback
                Task.Run(() => PerformExpansionAsync(expansion));
                
                // Clear buffer to prevent double triggering or recursive triggering
                _buffer.Clear();
                return; 
            }
        }
    }


    private async Task PerformExpansionAsync(TextExpansion expansion)
    {
        try
        {
            await _outputLock.WaitAsync();
            try
            {
                if (_outputDevice == null)
                {
                    _outputDevice = new UInputDevice();
                    _outputDevice.CreateVirtualInputDevice();
                    await Task.Delay(200);
                }

                // 0. Wait for Modifiers (AltGr AND Shift) to be released
                // If user is holding AltGr (e.g. for '}') or Shift (e.g. for '!'), Ctrl+V might fail or behave differently (e.g. Shift+Ctrl+V is terminal paste).
                int timeoutMs = 2000;
                int elapsed = 0;
                while ((_isAltGrPressed || _isShiftPressed) && elapsed < timeoutMs)
                {
                    await Task.Delay(50);
                    elapsed += 50;
                }

                // 1. Backspace the trigger
                Log.Debug("Backspacing {Length} chars", expansion.Trigger.Length);
                for (int i = 0; i < expansion.Trigger.Length; i++)
                {
                    SendKey(14); // Backspace
                }

                // 2. Paste the replacement (Universal Language Support)
                Log.Debug("Pasting replacement: {Replacement}", expansion.Replacement);
                
                // Backup clipboard
                var oldClipboard = await _clipboardService.GetTextAsync();
                
                // Set new text
                await _clipboardService.SetTextAsync(expansion.Replacement);
                await Task.Delay(50); // Wait for clipboard to accept
                
                // Send Ctrl+V
                SendKey(47, shift: false, altGr: false, ctrl: true); // 47 = v, ctrl=true
                
                // Wait for paste to complete
                await Task.Delay(150);
                
                // Restore clipboard
                if (!string.IsNullOrEmpty(oldClipboard))
                {
                    await _clipboardService.SetTextAsync(oldClipboard);
                }
            }
            finally
            {
                _outputLock.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error performing expansion");
        }
    }

    private void SendKey(int keyCode, bool shift = false, bool altGr = false, bool ctrl = false)
    {
        if (_outputDevice == null) return;

        // Modifiers Down
        if (ctrl)
        {
            _outputDevice.SendEvent(UInputNative.EV_KEY, 29, 1); // Left Ctrl
            _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        }
        if (shift) 
        {
            _outputDevice.SendEvent(UInputNative.EV_KEY, 42, 1); // Left Shift
            _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        }
        if (altGr)
        {
            _outputDevice.SendEvent(UInputNative.EV_KEY, 100, 1); // Right Alt
            _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        }

        // Key Press
        _outputDevice.SendEvent(UInputNative.EV_KEY, (ushort)keyCode, 1);
        _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        
        _outputDevice.SendEvent(UInputNative.EV_KEY, (ushort)keyCode, 0);
        _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);

        // Modifiers Up (Reverse Order)
        if (altGr)
        {
            _outputDevice.SendEvent(UInputNative.EV_KEY, 100, 0);
            _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        }
        if (shift) 
        {
            _outputDevice.SendEvent(UInputNative.EV_KEY, 42, 0);
            _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        }
        if (ctrl)
        {
            _outputDevice.SendEvent(UInputNative.EV_KEY, 29, 0);
            _outputDevice.SendEvent(UInputNative.EV_SYN, UInputNative.SYN_REPORT, 0);
        }
    }
}
