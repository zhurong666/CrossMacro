using System;
using System.Runtime.InteropServices;

namespace CrossMacro.Platform.Linux.Native.X11
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable IdentifierTypo
    
    [StructLayout(LayoutKind.Sequential)]
    public struct XIEventMask
    {
        public int DeviceId;
        public int MaskLen;
        public IntPtr Mask;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct XEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(0)] public XGenericEventCookie xcookie;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XGenericEventCookie
    {
        public int type;
        public IntPtr serial;
        [MarshalAs(UnmanagedType.Bool)] public bool send_event;
        public IntPtr display;
        public int extension;
        public int evtype;
        public int cookie;
        public IntPtr data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XIRawEvent
    {
        public int type;
        public IntPtr serial;
        [MarshalAs(UnmanagedType.Bool)] public bool send_event;
        public IntPtr display;
        public int extension;
        public int evtype;
        public IntPtr time;
        public int deviceid;
        public int sourceid;
        public int detail;
        public int flags;
        public XIValuatorState valuators;
        public IntPtr raw_values;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XIValuatorState
    {
        public int mask_len;
        public IntPtr mask;
    }

    public static class XInput2Consts
    {
        public const int GenericEvent = 35;
        public const int XIAllMasterDevices = 1;

        public const int XI_RawKeyPress = 13;
        public const int XI_RawKeyRelease = 14;
        public const int XI_RawButtonPress = 15;
        public const int XI_RawButtonRelease = 16;
        public const int XI_RawMotion = 17;

        public static void SetMask(byte[] mask, int eventType)
        {
            mask[eventType >> 3] |= (byte)(1 << (eventType & 7));
        }
    }
}
