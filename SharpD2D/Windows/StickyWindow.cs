using System;
using DirectN;
using SharpD2D.Drawing;

namespace SharpD2D.Windows
{
    public class StickyWindow : OverlayWindow
    {
        private long _lastStick;

        public StickyWindow(IntPtr parentWindow, Graphics device = null) : base(device)
        {
            if (!Functions.IsWindow(new HWND(parentWindow)))
                throw new ArgumentException("Not a window", nameof(parentWindow));
            ParentWindowHandle = parentWindow;
        }

        public bool AttachToClientArea { get; set; }
        public bool BypassTopmost { get; set; }
        public IntPtr ParentWindowHandle { get; set; }

        protected override void OnDrawGraphics(int frameCount, long frameTime, long deltaTime)
        {
            if (frameTime - _lastStick > 34)
            {
                if (BypassTopmost) PlaceAbove(ParentWindowHandle);
                FitTo(ParentWindowHandle, AttachToClientArea);
                _lastStick = frameTime;
            }
            base.OnDrawGraphics(frameCount, frameTime, deltaTime);
        }
    }
}
