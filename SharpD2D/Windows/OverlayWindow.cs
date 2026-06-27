using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using DirectN;
using SharpD2D.Drawing;

namespace SharpD2D.Windows
{
    public class OverlayWindow : Canvas, IDisposable
    {
        public delegate void WmDelegate(uint msg, IntPtr wParam, IntPtr lParam);

        private readonly Thread _windowThread;
        private readonly WNDPROC _wndProc;

        public readonly string ClassName;
        public readonly string MenuName;

        public OverlayWindow(Graphics gfx = null) : this(Rectangle.Create(0, 0, 800, 600), gfx) { }

        public OverlayWindow(Rectangle rect, Graphics gfx = null, string className = null, string title = null)
            : base(default, gfx)
        {
            ClassName = className;
            MenuName = WindowHelper.GenerateRandomTitle();
            if (string.IsNullOrEmpty(ClassName)) ClassName = WindowHelper.GenerateRandomClass();
            unsafe
            {
                _wndProc = WindowProcedure;
                fixed (char* lpMenu = MenuName, lpCLass = ClassName)
                {
                    var wc = new WNDCLASSEXW
                    {
                        cbSize = Marshal.SizeOf(typeof(WNDCLASSEXW)),
                        style = 0,
                        lpfnWndProc = _wndProc,
                        lpszMenuName = lpMenu,
                        lpszClassName = lpCLass
                    };
                    if (User32Native.RegisterClassExW(ref wc) == 0)
                        throw new Exception("Failed to register window class");
                }
            }

            InstantiateNewWindow(rect, title);
            IsVisible = true;
            WindowHelper.ExtendFrameIntoClientArea(Handle);
            _windowThread = Thread.CurrentThread;
        }

        public WINDOW_STYLE Style
        {
            get => (WINDOW_STYLE)Functions.GetWindowLongW(new HWND(Handle), WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            set => Functions.SetWindowLongW(new HWND(Handle), WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)value);
        }

        public WINDOW_EX_STYLE StyleEx
        {
            get => (WINDOW_EX_STYLE)Functions.GetWindowLongW(new HWND(Handle), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            set => Functions.SetWindowLongW(new HWND(Handle), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)value);
        }

        public bool IsTopmost
        {
            get => StyleEx.HasFlag(WINDOW_EX_STYLE.WS_EX_TOPMOST);
            set
            {
                if (value) WindowHelper.MakeTopmost(Handle);
                else WindowHelper.RemoveTopmost(Handle);
            }
        }

        public bool IsVisible
        {
            get => Handle != default && Functions.IsWindowVisible(new HWND(Handle));
            set
            {
                if (Handle != default)
                    Functions.ShowWindow(new HWND(Handle), value ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_HIDE);
            }
        }

        public string Title
        {
            get
            {
                var len = User32Native.GetWindowTextLengthW(new HWND(Handle));
                if (len == 0) return string.Empty;
                unsafe
                {
                    var chars = stackalloc char[len + 1];
                    User32Native.GetWindowTextW(new HWND(Handle), chars, len + 1);
                    return new string(chars, 0, len);
                }
            }
            set => User32Native.SetWindowTextW(new HWND(Handle), value);
        }

        public event WmDelegate WindowMessageReceived;

        ~OverlayWindow() { Dispose(false); }

        private void DestroyWindow()
        {
            lock (this)
            {
                if (Handle == default) return;
                var hWnd = new HWND(Handle);
                Handle = default;
                if (Functions.IsWindow(hWnd))
                    Functions.DestroyWindow(hWnd);
            }
        }

        private void InstantiateNewWindow(Rectangle rect, string title)
        {
            var styleEx = WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_TOPMOST |
                          WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
            var style = WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE;
            Handle = Functions.CreateWindowExW(
                styleEx,
                PWSTR.From(ClassName),
                PWSTR.From(title),
                style,
                rect.Left, rect.Top, rect.Width, rect.Height,
                default, default, default, 0);

            X = rect.Left;
            Y = rect.Top;
            Width = rect.Width;
            Height = rect.Height;

            User32Native.SetLayeredWindowAttributes(new HWND(Handle), 0, 255, 0x2);
            Functions.UpdateWindow(new HWND(Handle));
        }

        private LRESULT WindowProcedure(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            WindowMessageReceived?.Invoke(msg, (nint)wParam.Value, lParam.Value);
            switch (msg)
            {
                case 0x0014: // WM_ERASEBKGND
                    Functions.SendMessageW(hWnd, 0x000F, 0, 0); // WM_PAINT
                    break;
                case 0x0290: case 0x0291: case 0x0112: // WM_IME_KEYUP/DOWN, WM_SYSCOMMAND
                case 0x0104: case 0x0105: case 0x02E0: // WM_SYSKEYDOWN/UP, WM_DPICHANGED
                case 0x0085: case 0x000F: // WM_NCPAINT, WM_PAINT
                    return 0;
                case 0x031E: // WM_DWMCOMPOSITIONCHANGED
                    WindowHelper.ExtendFrameIntoClientArea(hWnd);
                    return 0;
                case 0x0002: case 0x0082: // WM_DESTROY, WM_NCDESTROY
                    Functions.PostQuitMessage(0);
                    break;
            }

            return User32Native.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        public unsafe void MessageLoop()
        {
            if (!Functions.IsWindow(new HWND(Handle)))
                throw new InvalidOperationException("Invalid handle");
            if (Thread.CurrentThread != _windowThread)
                throw new InvalidOperationException("Must run in same thread");

            var watch = Stopwatch.StartNew();
            long lastFrameTicks = 0;
            int frameCount = 0;
            var targetTicks = Stopwatch.Frequency / 60;

            while (Handle != default)
            {
                var hasMsg = Functions.PeekMessageW(out var message, default, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE);
                if (hasMsg)
                {
                    if (message.message == 0x0012) break;
                    Functions.TranslateMessage(message);
                    Functions.DispatchMessageW(message);
                }

                if (Handle == default) break;

                var nowTicks = watch.ElapsedTicks;
                if (nowTicks - lastFrameTicks >= targetTicks)
                {
                    var prevTicks = lastFrameTicks;
                    lastFrameTicks = nowTicks;
                    var frameTimeMs = watch.ElapsedMilliseconds;
                    OnDrawGraphics(frameCount++, frameTimeMs, (nowTicks - prevTicks) * 1000 / Stopwatch.Frequency);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public override bool Equals(object obj)
        {
            return (obj as Canvas)?.Handle == Handle && (Handle != default || ReferenceEquals(this, obj));
        }

        public void FitTo(IntPtr windowHandle, bool attachToClientArea = false)
        {
            var hWnd = new HWND(windowHandle);
            if (attachToClientArea)
            {
                if (Functions.GetClientRect(hWnd, out var client))
                {
                    var cp = new POINT();
                    Functions.ClientToScreen(hWnd, ref cp);
                    Functions.MoveWindow(new HWND(Handle), cp.x, cp.y, client.Width, client.Height, false);
                    X = cp.x; Y = cp.y;
                    Width = client.Width; Height = client.Height;
                }
            }
            else if (Functions.GetWindowRect(hWnd, out var rect))
            {
                Functions.MoveWindow(new HWND(Handle), rect.left, rect.top, rect.Width, rect.Height, false);
                X = rect.left; Y = rect.top;
                Width = rect.Width; Height = rect.Height;
            }
        }

        public override int GetHashCode() => Handle.GetHashCode();

        public void PlaceAbove(IntPtr windowHandle)
        {
            var prev = Functions.GetWindow(new HWND(windowHandle), GET_WINDOW_CMD.GW_HWNDPREV);
            if (prev != Handle)
                Functions.SetWindowPos(new HWND(Handle), prev, 0, 0, 0, 0,
                    SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                    SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS);
        }

        public override string ToString()
        {
            return OverrideHelper.ToString(
                "Handle", Handle.ToString("X"),
                "IsVisible", IsVisible.ToString(),
                "IsTopmost", IsTopmost.ToString(),
                "X", X.ToString(), "Y", Y.ToString(),
                "Width", Width.ToString(), "Height", Height.ToString());
        }

        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (Handle != default) DestroyWindow();
                disposedValue = true;
            }
        }

        public new void Dispose()
        {
            base.Dispose(true);
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
