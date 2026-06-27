using System.Runtime.InteropServices;
using DirectN;

namespace SharpD2D.Windows
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WNDCLASSEXW
    {
        public int cbSize;
        public uint style;
        public WNDPROC lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public HINSTANCE hInstance;
        public HICON hIcon;
        public HCURSOR hCursor;
        public HBRUSH hbrBackground;
        public unsafe char* lpszMenuName;
        public unsafe char* lpszClassName;
        public HICON hIconSm;
    }

    internal static class User32Native
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);

        [DllImport("user32.dll")]
        public static extern BOOL SetLayeredWindowAttributes(HWND hwnd, COLORREF crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern BOOL WaitMessage();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLengthW(HWND hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern unsafe int GetWindowTextW(HWND hWnd, char* lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern BOOL SetWindowTextW(HWND hWnd, string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern HWND FindWindowExW(HWND hWndParent, HWND hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern LRESULT DefWindowProcW(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

        [DllImport("user32.dll")]
        public static extern BOOL SetProcessDPIAware();
    }
}
