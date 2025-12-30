using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux.Native.X11;
using Serilog;

namespace CrossMacro.Platform.Linux.Services
{
    public class X11InputCapture : IInputCapture
    {
        private IntPtr _display;
        private IntPtr _rootWindow;
        private Thread? _captureThread;
        private volatile bool _isRunning;
        private bool _disposed;
        
        private bool _captureMouse;
        private bool _captureKeyboard;
        
        private double _lastX = -1;
        private double _lastY = -1;
        private double _residualX = 0;
        private double _residualY = 0;

        public string ProviderName => "X11 (XInput2)";

        public bool IsSupported
        {
            get
            {
                try
                {
                    var dpy = X11Native.XOpenDisplay(null);
                    if (dpy == IntPtr.Zero) return false;
                    
                    int major = 2, minor = 2;
                    int res = X11Native.XIQueryVersion(dpy, ref major, ref minor);
                    X11Native.XCloseDisplay(dpy);
                    
                    return res == 0; // Success
                }
                catch
                {
                    return false;
                }
            }
        }

        public event EventHandler<InputCaptureEventArgs>? InputReceived;
        public event EventHandler<string>? Error;

        public void Configure(bool captureMouse, bool captureKeyboard)
        {
            _captureMouse = captureMouse;
            _captureKeyboard = captureKeyboard;
        }

        public IReadOnlyList<InputDeviceInfo> GetAvailableDevices()
        {
            return new List<InputDeviceInfo>
            {
                new() { Name = "X11 Virtual Core Pointer", IsMouse = true, Path = "x11-pointer" },
                new() { Name = "X11 Virtual Core Keyboard", IsKeyboard = true, Path = "x11-keyboard" }
            };
        }

        public Task StartAsync(CancellationToken ct)
        {
            if (_isRunning)
            {
                Log.Warning("[X11InputCapture] Already running");
                return Task.CompletedTask;
            }

            _isRunning = true;
            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "X11InputCapture"
            };
            _captureThread.Start();


            ct.Register(Stop);

            return Task.CompletedTask;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void CaptureLoop()
        {
            try
            {
                _display = X11Native.XOpenDisplay(null);
                if (_display == IntPtr.Zero)
                {
                    Error?.Invoke(this, "Failed to open X Display");
                    return;
                }

                _rootWindow = X11Native.XDefaultRootWindow(_display);

                // Init XI2
                int major = 2, minor = 2;
                if (X11Native.XIQueryVersion(_display, ref major, ref minor) != 0)
                {
                    Error?.Invoke(this, "XInput2 extension not available");
                    return;
                }

                // Select Events - 4 bytes covers up to event type 31
                var maskBytes = new byte[4];

                if (_captureKeyboard)
                {
                    XInput2Consts.SetMask(maskBytes, XInput2Consts.XI_RawKeyPress);
                    XInput2Consts.SetMask(maskBytes, XInput2Consts.XI_RawKeyRelease);
                }

                if (_captureMouse)
                {
                    XInput2Consts.SetMask(maskBytes, XInput2Consts.XI_RawButtonPress);
                    XInput2Consts.SetMask(maskBytes, XInput2Consts.XI_RawButtonRelease);
                    XInput2Consts.SetMask(maskBytes, XInput2Consts.XI_RawMotion);
                }


                IntPtr maskPtr = Marshal.AllocHGlobal(maskBytes.Length);
                try
                {
                    Marshal.Copy(maskBytes, 0, maskPtr, maskBytes.Length);

                    var mask = new XIEventMask
                    {
                        DeviceId = XInput2Consts.XIAllMasterDevices,
                        MaskLen = maskBytes.Length,
                        Mask = maskPtr
                    };

                    int result = X11Native.XISelectEvents(_display, _rootWindow, ref mask, 1);
                    if (result != 0)
                    {
                        Error?.Invoke(this, $"XISelectEvents failed: {result}");
                        return;
                    }
                    X11Native.XFlush(_display);
                }
                finally
                {
                    Marshal.FreeHGlobal(maskPtr);
                }

                Log.Information("[X11InputCapture] Started capturing (Mouse={M}, Kbd={K})", _captureMouse, _captureKeyboard);
                
                if (X11Native.XQueryPointer(_display, _rootWindow, out _, out _, out int rootX, out int rootY, out _, out _, out _))
                {
                    _lastX = rootX;
                    _lastY = rootY;
                    Log.Debug("[X11InputCapture] Initialized start position: ({X}, {Y})", rootX, rootY);
                }
                else
                {
                    Log.Warning("[X11InputCapture] Failed to query initial pointer position");
                }

                IntPtr eventPtr = Marshal.AllocHGlobal(192);
                try 
                {
                    while (_isRunning)
                    {
                        // Non-blocking check to allow exit
                        if (X11Native.XPending(_display) == 0)
                        {
                            Thread.Sleep(1);
                            continue;
                        }

                        X11Native.XNextEvent(_display, eventPtr);
                        var xEvent = Marshal.PtrToStructure<XEvent>(eventPtr);

                        if (xEvent.xcookie.type == XInput2Consts.GenericEvent && 
                            X11Native.XGetEventData(_display, eventPtr))
                        {
                            try
                            {
                                var cookie = Marshal.PtrToStructure<XGenericEventCookie>(eventPtr);
                                ProcessGenericEvent(cookie);
                            }
                            finally
                            {
                                X11Native.XFreeEventData(_display, eventPtr);
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(eventPtr);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[X11InputCapture] Error in capture loop");
                Error?.Invoke(this, ex.Message);
            }
            finally
            {
                if (_display != IntPtr.Zero)
                {
                    X11Native.XCloseDisplay(_display);
                    _display = IntPtr.Zero;
                }
            }
        }

        private void ProcessGenericEvent(XGenericEventCookie cookie)
        {
            var rawEvent = Marshal.PtrToStructure<XIRawEvent>(cookie.data);

            // Keyboard
            if (cookie.evtype == XInput2Consts.XI_RawKeyPress || cookie.evtype == XInput2Consts.XI_RawKeyRelease)
            {
               int code = rawEvent.detail - 8;
               int value = (cookie.evtype == XInput2Consts.XI_RawKeyPress) ? 1 : 0;
               
               var args = new InputCaptureEventArgs
               {
                   Type = InputEventType.Key,
                   Code = (ushort)code,
                   Value = value,
                   Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                   DeviceName = "X11 Device"
               };
               InputReceived?.Invoke(this, args);
               return;
            }

            if (cookie.evtype == XInput2Consts.XI_RawMotion)
            {
                if (!X11Native.XQueryPointer(_display, _rootWindow, out _, out _, out int rootX, out int rootY, out _, out _, out _))
                {
                    return;
                }

                if (_lastX < 0)
                {
                    _lastX = rootX;
                    _lastY = rootY;
                    Log.Warning("[X11InputCapture] Late position initialization: ({X}, {Y})", rootX, rootY);
                    return;
                }

                double dx = rootX - _lastX;
                double dy = rootY - _lastY;
                
                _lastX = rootX;
                _lastY = rootY;



                _residualX += dx;
                _residualY += dy;

                int moveX = (int)_residualX;
                int moveY = (int)_residualY;

                if (moveX == 0 && moveY == 0) return;

                if (moveX != 0)
                {
                    var argsX = new InputCaptureEventArgs
                    {
                        Type = InputEventType.MouseMove,
                        Code = 0,
                        Value = moveX,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        DeviceName = "X11 Device"
                    };
                    InputReceived?.Invoke(this, argsX);
                    _residualX -= moveX;
                }

                if (moveY != 0)
                {
                    var argsY = new InputCaptureEventArgs
                    {
                        Type = InputEventType.MouseMove,
                        Code = 1,
                        Value = moveY,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        DeviceName = "X11 Device"
                    };
                    InputReceived?.Invoke(this, argsY);
                    _residualY -= moveY;
                }
                
                // SYNC
                var argsSync = new InputCaptureEventArgs
                {
                    Type = InputEventType.Sync,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    DeviceName = "X11 Device"
                };
                InputReceived?.Invoke(this, argsSync);
                return;
            }

            // Mouse Buttons
            if (cookie.evtype == XInput2Consts.XI_RawButtonPress || cookie.evtype == XInput2Consts.XI_RawButtonRelease)
            {
                int code = rawEvent.detail;
                int value = (cookie.evtype == XInput2Consts.XI_RawButtonPress) ? 1 : 0;
                InputEventType type = InputEventType.MouseButton;

                // Handle Scroll (buttons 4/5)
                if (code == 4 || code == 5)
                {
                    if (value == 0) return;
                    type = InputEventType.MouseScroll;
                    value = (code == 4) ? 120 : -120;
                    code = 0;
                }
                else
                {
                    code = MapX11ButtonToLinux(code);
                }

                var args = new InputCaptureEventArgs
                {
                    Type = type,
                    Code = (ushort)code,
                    Value = value,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    DeviceName = "X11 Device"
                };
                InputReceived?.Invoke(this, args);
            }
        }

        private int MapX11ButtonToLinux(int x11Btn)
        {
            return x11Btn switch
            {
                1 => 272, // Left
                2 => 274, // Middle
                3 => 273, // Right
                8 => 275, // Back
                9 => 276, // Forward
                _ => x11Btn // Unknown
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
        }
    }
}
