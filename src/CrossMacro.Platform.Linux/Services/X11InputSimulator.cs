using System;
using System.Runtime.InteropServices;
using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux.Native.X11;
using Serilog;

namespace CrossMacro.Platform.Linux.Services
{
    public class X11InputSimulator : IInputSimulator
    {
        private IntPtr _display;
        private bool _disposed;
        private bool _isSupported;
        private int _screen;

        public string ProviderName => "X11 (XTest)";

        public bool IsSupported => _isSupported;

        public X11InputSimulator()
        {
            try
            {
                _display = X11Native.XOpenDisplay(null);
                if (_display == IntPtr.Zero)
                {
                    Log.Warning("[X11InputSimulator] Failed to open X Display");
                    return;
                }

                if (X11Native.XTestQueryExtension(_display, out _, out _, out int major, out int minor))
                {
                    _isSupported = true;
                    _screen = X11Native.XDefaultScreen(_display);
                    Log.Information("[X11InputSimulator] XTest extension available (v{Major}.{Minor})", major, minor);
                }
                else
                {
                    Log.Warning("[X11InputSimulator] XTest extension NOT installed on this system. Simulation disabled.");
                    _isSupported = false;
                }
            }
            catch (DllNotFoundException dllEx)
            {
                Log.Warning("[X11InputSimulator] XTest library not found (Simulation disabled): {Message}", dllEx.Message);
                _isSupported = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[X11InputSimulator] Error during initialization");
                _isSupported = false;
            }
        }

        public void Initialize(int screenWidth = 0, int screenHeight = 0)
        {
        }

        public void MoveAbsolute(int x, int y)
        {
            if (!_isSupported) return;

            X11Native.XTestFakeMotionEvent(_display, -1, x, y, 0);
            X11Native.XFlush(_display);
        }

        public void MoveRelative(int dx, int dy)
        {
            if (!_isSupported) return;

            // XTest requires absolute coordinates, so we query the current position and apply the delta.
            var root = X11Native.XDefaultRootWindow(_display);
            if (X11Native.XQueryPointer(_display, root, out _, out _, out int rx, out int ry, out _, out _, out _))
            {
                MoveAbsolute(rx + dx, ry + dy);
            }
        }

        public void MouseButton(int button, bool pressed)
        {
            if (!_isSupported) return;

            // Map Linux kernel keycodes to X11 button indices
            uint x11Button = 0;
            switch(button)
            {
                case 272: x11Button = 1; break; // Left
                case 273: x11Button = 3; break; // Right
                case 274: x11Button = 2; break; // Middle
                case 275: x11Button = 8; break; // Side/Back
                case 276: x11Button = 9; break; // Extra/Forward
                default: 
                    // Unknown button codes are ignored
                    break;
            }

            if (x11Button > 0)
            {
                X11Native.XTestFakeButtonEvent(_display, x11Button, pressed, 0);
                X11Native.XFlush(_display);
            }
        }

        public void Scroll(int delta)
        {
            if (!_isSupported) return;

            // X11 represents scroll as buttons 4 (up) and 5 (down)
            uint button = delta > 0 ? 4u : 5u;
            
            X11Native.XTestFakeButtonEvent(_display, button, true, 0);
            X11Native.XTestFakeButtonEvent(_display, button, false, 0);
            X11Native.XFlush(_display);
        }

        public void KeyPress(int keyCode, bool pressed)
        {
            if (!_isSupported) return;

            // Convert Linux Kernel Keycode to X11 Keycode (+8 offset)
            uint x11Keycode = (uint)keyCode + 8;
            X11Native.XTestFakeKeyEvent(_display, x11Keycode, pressed, 0);
            X11Native.XFlush(_display);
        }

        public void Sync()
        {
            if (_isSupported)
                X11Native.XFlush(_display);
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            if (_display != IntPtr.Zero)
            {
                X11Native.XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}
