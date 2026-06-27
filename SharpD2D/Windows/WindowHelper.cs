using System;
using System.Collections.Generic;
using DirectN;

namespace SharpD2D.Windows
{
    public static class WindowHelper
    {
        private const int MaxRandomStringLen = 16;
        private const int MinRandomStringLen = 8;
        private static readonly object _blacklistLock = new();
        private static readonly Random _random = new();
        private static readonly List<string> _windowClassesBlacklist = new();

        private static string GenerateRandomAsciiString(int minLength, int maxLength)
        {
            var length = _random.Next(minLength, maxLength);
            var chars = new char[length];
            for (var i = 0; i < chars.Length; i++) chars[i] = (char)_random.Next(97, 123);
            return new string(chars);
        }

        public static void DisableScalingGlobal() => User32Native.SetProcessDPIAware();

        public static void EnableBlurBehind(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            ExtendFrameIntoClientArea(hwnd);
        }

        public static void ExtendFrameIntoClientArea(IntPtr hwnd)
        {
            var margin = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyBottomHeight = -1, cyTopHeight = -1 };
            Functions.DwmExtendFrameIntoClientArea(new HWND(hwnd), margin);
        }

        public static IntPtr FindChildWindow(IntPtr parentWindow, string childWindowName = null,
            string childClassName = null, IntPtr childAfter = default)
        {
            if (string.IsNullOrEmpty(childWindowName)) childWindowName = null;
            if (string.IsNullOrEmpty(childClassName)) childClassName = null;
            return User32Native.FindWindowExW(new HWND(parentWindow), new HWND(childAfter), childClassName, childWindowName);
        }

        public static IntPtr FindWindow(string title, string className = null)
        {
            return User32Native.FindWindowExW(default, default, className, title);
        }

        public static string GenerateRandomClass()
        {
            lock (_blacklistLock)
            {
                while (true)
                {
                    var name = GenerateRandomAsciiString(MinRandomStringLen, MaxRandomStringLen);
                    if (!_windowClassesBlacklist.Contains(name))
                    { _windowClassesBlacklist.Add(name); return name; }
                }
            }
        }

        public static string GenerateRandomTitle() => GenerateRandomAsciiString(MinRandomStringLen, MaxRandomStringLen);

        public static void MakeTopmost(IntPtr hwnd)
        {
            Functions.SetWindowPos(new HWND(hwnd), new HWND(-1), 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }

        public static void RemoveTopmost(IntPtr hwnd)
        {
            Functions.SetWindowPos(new HWND(hwnd), new HWND(-2), 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }
    }
}
