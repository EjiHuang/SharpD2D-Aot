using System;
using DirectN;
using SharpD2D.Drawing;

namespace SharpD2D.Windows
{
    public class Canvas : IDisposable
    {
        private Graphics _graphics;
        private bool _initialized;
        private int _pendingFps;

        public IntPtr Handle { get; protected set; }

        public Rectangle Rect
        {
            get => new Rectangle(X, Y, Width, Height);
            set { X = value.Left; Y = value.Top; Width = value.Width; Height = value.Height; }
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Graphics Graphics
        {
            get
            {
                if (_graphics == null && Handle != IntPtr.Zero)
                {
                    _graphics = new Graphics(Handle, Width, Height);
                    ApplyPendingState();
                }
                return _graphics;
            }
            set
            {
                if (_graphics != null)
                    _graphics.Dispose();
                _graphics = value;
            }
        }

        public int FPS
        {
            get => Graphics?.FPS ?? _pendingFps;
            set
            {
                _pendingFps = value;
                if (Graphics != null)
                    Graphics.MeasureFPS = value > 0;
            }
        }

        public event EventHandler<SetupGraphicsEventArgs> SetupGraphics;
        public event EventHandler<DrawGraphicsEventArgs> DrawGraphics;
        public event EventHandler<DestroyGraphicsEventArgs> DestroyGraphics;

        protected Canvas() { }

        public Canvas(IntPtr handle, Graphics gfx = null)
        {
            Handle = handle;
            Graphics = gfx;
        }

        private void ApplyPendingState()
        {
            if (_graphics != null && _pendingFps > 0)
                _graphics.MeasureFPS = true;
        }

        public void Initialize()
        {
            if (_initialized) return;

            if (Width <= 0) Width = 800;
            if (Height <= 0) Height = 600;

            if (Graphics == null)
                Graphics = new Graphics(Handle, Width, Height);
            else if (Graphics.IsInitialized)
                Graphics.Recreate(Handle);
            else
            {
                Graphics.WindowHandle = Handle;
                Graphics.Width = Width;
                Graphics.Height = Height;
            }

            Graphics.Setup();

            // Forward device recreate events so brushes/fonts are recreated
            Graphics.RecreateResources += (_, args) =>
            {
                SetupGraphics?.Invoke(this, new SetupGraphicsEventArgs(args.Graphics, true));
            };

            SetupGraphics?.Invoke(this, new SetupGraphicsEventArgs(Graphics, false));
            _initialized = true;
        }

        protected virtual void OnDrawGraphics(int frameCount, long frameTime, long deltaTime)
        {
            var gfx = Graphics;
            if (gfx == null || !gfx.IsInitialized) return;

            if (gfx.Width != Width || gfx.Height != Height)
            {
                gfx.Resize(Width, Height);
            }

            DrawGraphics?.Invoke(this, new DrawGraphicsEventArgs
            {
                Graphics = gfx,
                FrameCount = frameCount,
                FrameTime = frameTime,
                DeltaTime = deltaTime
            });
        }

        /// <summary>
        ///     Triggers a single frame of rendering. Thread-safe.
        /// </summary>
        public void RenderFrame(int frameCount, long frameTime, long deltaTime)
        {
            OnDrawGraphics(frameCount, frameTime, deltaTime);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DestroyGraphics?.Invoke(this, new DestroyGraphicsEventArgs(Graphics));
                Graphics?.Dispose();
                Graphics = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
