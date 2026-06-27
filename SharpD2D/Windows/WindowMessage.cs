namespace SharpD2D.Windows
{
    internal static class WindowMessage
    {
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_NCDESTROY = 0x0082;
        public const uint WM_ERASEBKGND = 0x0014;
        public const uint WM_PAINT = 0x000F;
        public const uint WM_NCPAINT = 0x0085;
        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint WM_SYSKEYDOWN = 0x0104;
        public const uint WM_SYSKEYUP = 0x0105;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_IME_KEYDOWN = 0x0290;
        public const uint WM_IME_KEYUP = 0x0291;
        public const uint WM_DPICHANGED = 0x02E0;
        public const uint WM_DWMCOMPOSITIONCHANGED = 0x031E;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_MOVE = 0x0003;
    }
}
