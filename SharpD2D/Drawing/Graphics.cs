using System;
using System.Diagnostics;
using System.IO;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Encapsulates a Direct2D drawing surface.
    /// </summary>
    public class Graphics : IDisposable
    {
        private readonly Stopwatch _watch;
        private IComObject<ID2D1HwndRenderTarget> _device;
        private IComObject<ID2D1Factory> _factory;
        private IComObject<IDWriteFactory> _fontFactory;

        private volatile int _fpsCount;
        private volatile bool _resize;
        private volatile int _resizeHeight;
        private volatile int _resizeWidth;
        private IComObject<ID2D1StrokeStyle> _strokeStyle;

        /// <summary>
        ///     Initializes a new Graphics surface.
        /// </summary>
        public Graphics()
        {
            _watch = new Stopwatch();

            PerPrimitiveAntiAliasing = false;
            TextAntiAliasing = true;
            VSync = false;
            UseMultiThreadedFactories = false;
        }

        /// <summary>
        ///     Initializes a new Graphics surface using a window handle.
        /// </summary>
        /// <param name="windowHandle">A handle to the window used as a surface.</param>
        public Graphics(IntPtr windowHandle) : this()
        {
            WindowHandle = windowHandle;
        }

        /// <summary>
        ///     Initializes a new Graphics surface using a window handle and its width and height.
        /// </summary>
        /// <param name="windowHandle">A handle to the window used as a surface.</param>
        /// <param name="width">A value indicating the width of the surface.</param>
        /// <param name="height">A value indicating the height of the surface.</param>
        public Graphics(IntPtr windowHandle, int width, int height) : this()
        {
            WindowHandle = windowHandle;
            Width = width;
            Height = height;
        }

        #region PROPERTIES



        /// <summary>
        ///     Specifies the images per second in which this graphics device redraws.
        /// </summary>
        public int FPS { get; private set; }

        /// <summary>
        ///     Gets or sets the width of this Graphics surface.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        ///     Indicates whether this Graphics surface is currently drawing on a Scene.
        /// </summary>
        public bool IsDrawing { get; private set; }

        /// <summary>
        ///     Indicates whether this Graphics surface is initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     Indicates whether this Graphics surface will change its size on the next Scene.
        /// </summary>
        public bool IsResizing => _resize;

        /// <summary>
        ///     Determines whether this Graphics device will measure the resulting frames per second.
        /// </summary>
        public bool MeasureFPS { get; set; }

        /// <summary>
        ///     Determines whether Anti-Aliasing for each primitive (Line, RectangleF, Circle, Geometry) is enabled.
        /// </summary>
        public bool PerPrimitiveAntiAliasing { get; set; }

        /// <summary>
        ///     Determines whether Anti-Aliasing for Text is enabled.
        /// </summary>
        public bool TextAntiAliasing { get; set; }

        /// <summary>
        ///     Determines whether factories (Font, Geometry, Brush) will be used in a multi-threaded environment.
        /// </summary>
        public bool UseMultiThreadedFactories { get; set; }

        /// <summary>
        ///     Determines whether this Graphics surface will be locked to the monitors refresh rate.
        /// </summary>
        public bool VSync { get; set; }

        /// <summary>
        ///     Gets or sets the width of this Graphics surface.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        ///     Gets or sets the window handle of the Graphics surface.
        /// </summary>
        public IntPtr WindowHandle { get; set; }

        /// <summary>
        ///     Occurs when the underlying device was recreated and resources need to be recreated.
        /// </summary>
        public event EventHandler<RecreateResourcesEventArgs> RecreateResources;

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>

        #endregion
        ~Graphics()
        {
            Dispose(false);
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized) throw new InvalidOperationException("The Direct2D device is not initialized");
        }

        private void CreateDeviceForCurrentSurface()
        {
            var hwndProps = new D2D1_HWND_RENDER_TARGET_PROPERTIES
            {
                hwnd = new HWND(WindowHandle),
                pixelSize = new D2D_SIZE_U((uint)Width, (uint)Height),
                presentOptions = VSync ? D2D1_PRESENT_OPTIONS.D2D1_PRESENT_OPTIONS_NONE : D2D1_PRESENT_OPTIONS.D2D1_PRESENT_OPTIONS_IMMEDIATELY
            };

            var renderProperties = new D2D1_RENDER_TARGET_PROPERTIES
            {
                type = D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
                pixelFormat = new D2D1_PIXEL_FORMAT { format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED },
                dpiX = 96.0f,
                dpiY = 96.0f,
                usage = D2D1_RENDER_TARGET_USAGE.D2D1_RENDER_TARGET_USAGE_NONE,
                minLevel = D2D1_FEATURE_LEVEL.D2D1_FEATURE_LEVEL_DEFAULT
            };

            try
            {
                _device = _factory.CreateHwndRenderTarget(hwndProps, renderProperties);
            }
            catch
            {
                try
                {
                    renderProperties.pixelFormat = new D2D1_PIXEL_FORMAT { format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED };
                    _device = _factory.CreateHwndRenderTarget(hwndProps, renderProperties);
                }
                catch
                {
                    renderProperties.pixelFormat = new D2D1_PIXEL_FORMAT { format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED };
                    _device = _factory.CreateHwndRenderTarget(hwndProps, renderProperties);

                    throw;
                }
            }

            _device.SetAntialiasMode(
                PerPrimitiveAntiAliasing
                    ? D2D1_ANTIALIAS_MODE.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE
                    : D2D1_ANTIALIAS_MODE.D2D1_ANTIALIAS_MODE_ALIASED);
            _device.SetTextAntialiasMode(
                TextAntiAliasing
                    ? D2D1_TEXT_ANTIALIAS_MODE.D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE
                    : D2D1_TEXT_ANTIALIAS_MODE.D2D1_TEXT_ANTIALIAS_MODE_ALIASED);
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of Graphics represent the same value.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public static bool Equals(Graphics left, Graphics right)
        {
            return left?.Equals(right) == true;
        }

        /// <summary>
        ///     Starts a new Scene (Frame).
        /// </summary>
        public void BeginScene()
            {
                if (!IsInitialized) throw ThrowHelper.DeviceNotInitialized();
                if (IsDrawing) return;

                if (_resize)
                    try
                    {
                        _device.Resize(new D2D_SIZE_U((uint)_resizeWidth, (uint)_resizeHeight));
                        Width = _resizeWidth;
                        Height = _resizeHeight;
                        _resize = false;
                    }
                    catch { }

                if (MeasureFPS && !_watch.IsRunning) _watch.Restart();

                _device.BeginDraw();
                IsDrawing = true;
            }

        /// <summary>
        ///     Clears the current Scene (Frame) using a transparent background color.
        /// </summary>
        public void ClearScene()
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.Clear(null);
        }

        /// <summary>
        ///     Clears the current Scene (Frame) using the given background color.
        /// </summary>
        /// <param name="color">The background color of this Scene.</param>
        public void ClearScene(Color color)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.Clear(color);
        }

        /// <summary>
        ///     Clears the current Scene (Frame) using the given brush.
        /// </summary>
        /// <param name="brush">The brush used to draw the background of this Scene.</param>
        public void ClearScene(SolidBrush brush)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.Clear(brush.Color);
        }

        /// <summary>
        ///     Creates a new Font by using the given font family, size and styles.
        /// </summary>
        /// <param name="fontFamilyName">The name of any installed font family.</param>
        /// <param name="size">A value indicating the size of a font in pixels.</param>
        /// <param name="bold">A Boolean determining whether this font is bold.</param>
        /// <param name="italic">A Boolean determining whether this font is italic.</param>
        /// <param name="wordWrapping">A Boolean determining whether this font uses word wrapping.</param>
        /// <returns></returns>
        public Font CreateFont(string fontFamilyName, float size, bool bold = false, bool italic = false,
            bool wordWrapping = false)
        {
            ThrowIfNotInitialized();

            return new Font(_fontFactory, fontFamilyName, size, bold, italic, wordWrapping);
        }

        /// <summary>
        ///     Creates a new Geometry used to draw complex figures.
        /// </summary>
        /// <returns>The Geometry this method creates.</returns>
        public Geometry CreateGeometry()
        {
            return new Geometry(this);
        }

        /// <summary>
        ///     Creates a new Image by using the given bytes.
        /// </summary>
        /// <param name="bytes">An image loaded into a byte array.</param>
        /// <returns>The Image this method creates.</returns>
        public Image CreateImage(byte[] bytes)
        {
            ThrowIfNotInitialized();

            return new Image(_device, bytes);
        }

        /// <summary>
        ///     Creates a new Image from an image file on the disk.
        /// </summary>
        /// <param name="path">The path to an image file.</param>
        /// <returns>The Image this method creates.</returns>
        public Image CreateImage(string path)
        {
            ThrowIfNotInitialized();

            return new Image(_device, path);
        }

        /// <summary>
        /// Create an empty image
        /// </summary>
        /// <returns></returns>
        public Image CreateImage()
        {
            ThrowIfNotInitialized();

            return new Image(_device);
        }

        /// <summary>
        /// Create and empty image with specified size and format
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="pitch"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public Image CreateImage(int width, int height, D2D1_PIXEL_FORMAT format)
        {
            ThrowIfNotInitialized();

            return new Image(_device, new D2D_SIZE_U((uint)width, (uint)height), format);
        }

        /// <summary>
        ///     Creates a new SolidBrush by using the given color components.
        /// </summary>
        /// <param name="r">The red component value of this color.</param>
        /// <param name="g">The green component value of this color.</param>
        /// <param name="b">The blue component value of this color.</param>
        /// <param name="a">The alpha component value of this color.</param>
        /// <returns>The SolidBrush this method creates.</returns>
        public SolidBrush CreateSolidBrush(float r, float g, float b, float a = 1.0f)
        {
            ThrowIfNotInitialized();

            return new SolidBrush(_device, new Color(r, g, b, a));
        }

        /// <summary>
        ///     Creates a new SolidBrush by using the given color components.
        /// </summary>
        /// <param name="r">The red component value of this color.</param>
        /// <param name="g">The green component value of this color.</param>
        /// <param name="b">The blue component value of this color.</param>
        /// <param name="a">The alpha component value of this color.</param>
        /// <returns>The SolidBrush this method creates.</returns>
        public SolidBrush CreateSolidBrush(int r, int g, int b, int a = 255)
        {
            ThrowIfNotInitialized();

            return new SolidBrush(_device, new Color(r, g, b, a));
        }

        /// <summary>
        ///     Creates a new SolidBrush by using the given color structure.
        /// </summary>
        /// <param name="color">A value representing the ARGB components used to create a SolidBrush.</param>
        /// <returns>The SolidBrush this method creates.</returns>
        public SolidBrush CreateSolidBrush(Color color)
        {
            ThrowIfNotInitialized();

            return new SolidBrush(_device, color);
        }

        /// <summary>
        ///     Specifies a matrix to which all subsequent drawing operations are transformed.
        /// </summary>
        /// <param name="matrix">The matrix used for the transformation.</param>
        public void TransformStart(TransformationMatrix matrix)
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.SetTransform((D2D_MATRIX_3X2_F)matrix);
        }

        /// <summary>
        ///     Removes the transformation matrix.
        /// </summary>
        public void TransformEnd()
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.SetTransform((D2D_MATRIX_3X2_F)TransformationMatrix.Identity);
        }

        /// <summary>
        ///     Specifies a rectangle to which all subsequent drawing operations are clipped.
        /// </summary>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        public void ClipRegionStart(float left, float top, float right, float bottom)
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.Object.PushAxisAlignedClip(new D2D_RECT_F(left, top, right, bottom),
                PerPrimitiveAntiAliasing ? D2D1_ANTIALIAS_MODE.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE : D2D1_ANTIALIAS_MODE.D2D1_ANTIALIAS_MODE_ALIASED);
        }

        /// <summary>
        ///     Specifies a rectangle to which all subsequent drawing operations are clipped.
        /// </summary>
        /// <param name="region">A RectangleF representing the size and position of the clipping area.</param>
        public void ClipRegionStart(RectangleF region)
        {
            ClipRegionStart(region.Left, region.Top, region.Right, region.Bottom);
        }

        /// <summary>
        ///     Removes the last clip from the render target. After this method is called, the clip is no longer applied to
        ///     subsequent drawing operations.
        /// </summary>
        public void ClipRegionEnd()
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.Object.PopAxisAlignedClip();
        }

        /// <summary>
        ///     Draws a circle with a dashed line by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="x">The x-coordinate of the center of the circle.</param>
        /// <param name="y">The y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DashedCircle(IBrush brush, float x, float y, float radius, float stroke)
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.Object.DrawEllipse(new D2D1_ELLIPSE(x, y, radius, radius), brush.NativeBrush.Object,
                stroke, _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws a circle with a dashed line by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DashedCircle(IBrush brush, PointF location, float radius, float stroke)
        {
            DashedCircle(brush, location.X, location.Y, radius, stroke);
        }

        /// <summary>
        ///     Draws a circle with a dashed line by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="circle">A Circle structure which includes the dimension of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DashedCircle(IBrush brush, Circle circle, float stroke)
        {
            DashedCircle(brush, circle.Location.X, circle.Location.Y, circle.Radius, stroke);
        }

        /// <summary>
        ///     Draws an ellipse with a dashed line by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="x">The x-coordinate of the center of the ellipse.</param>
        /// <param name="y">The y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void DashedEllipse(IBrush brush, float x, float y, float radiusX, float radiusY, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawEllipse(new D2D1_ELLIPSE(x, y, radiusX, radiusY), brush.NativeBrush.Object,
                stroke, _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws an ellipse with a dashed line by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void DashedEllipse(IBrush brush, PointF location, float radiusX, float radiusY, float stroke)
        {
            DashedEllipse(brush, location.X, location.Y, radiusX, radiusY, stroke);
        }

        /// <summary>
        ///     Draws an ellipse with a dashed line by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="ellipse">An Ellipse structure which includes the dimension of the ellipse.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void DashedEllipse(IBrush brush, Ellipse ellipse, float stroke)
        {
            DashedEllipse(brush, ellipse.Location.X, ellipse.Location.Y, ellipse.RadiusX, ellipse.RadiusY, stroke);
        }

        /// <summary>
        ///     Draws a Geometry with dashed lines using the given brush and thickness.
        /// </summary>
        /// <param name="geometry">The Geometry to be drawn.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="stroke">A value that determines the width/thickness of the lines.</param>
        public void DashedGeometry(Geometry geometry, IBrush brush, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawGeometry(geometry.NativeGeometry.Object, brush.NativeBrush.Object, stroke, _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws a dashed line at the given start and end point.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the line.</param>
        /// <param name="startX">The start position of the line on the x-axis</param>
        /// <param name="startY">The start position of the line on the y-axis</param>
        /// <param name="endX">The end position of the line on the x-axis</param>
        /// <param name="endY">The end position of the line on the y-axis</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedLine(IBrush brush, float startX, float startY, float endX, float endY, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawLine(new D2D_POINT_2F(startX, startY), new D2D_POINT_2F(endX, endY), brush.NativeBrush.Object, stroke,
                _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws a dashed line at the given start and end point.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the line.</param>
        /// <param name="start">A PointF structure including the start position of the line.</param>
        /// <param name="end">A PointF structure including the end position of the line.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedLine(IBrush brush, PointF start, PointF end, float stroke)
        {
            DashedLine(brush, start.X, start.Y, end.X, end.Y, stroke);
        }

        /// <summary>
        ///     Draws a dashed line at the given start and end point.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the line.</param>
        /// <param name="line">A Line structure including the start and end PointF of the line.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedLine(IBrush brush, Line line, float stroke)
        {
            DashedLine(brush, line.Start.X, line.Start.Y, line.End.X, line.End.Y, stroke);
        }

        /// <summary>
        ///     Draws a rectangle with dashed lines by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedRectangle(IBrush brush, float left, float top, float right, float bottom, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawRectangle(new D2D_RECT_F(left, top, right, bottom), brush.NativeBrush.Object, stroke, _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws a rectangle with dashed lines by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure that determines the boundaries of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedRectangle(IBrush brush, RectangleF rectangle, float stroke)
        {
            DashedRectangle(brush, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, stroke);
        }

        /// <summary>
        ///     Draws a rectangle with rounded edges and dashed lines by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="radius">A value that determines radius of corners.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedRoundedRectangle(IBrush brush, float left, float top, float right, float bottom, float radius,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var rect = new D2D1_ROUNDED_RECT
            {
                radiusX = radius,
                radiusY = radius,
                rect = new D2D_RECT_F(left, top, right, bottom)
            };

            _device.Object.DrawRoundedRectangle(rect, brush.NativeBrush.Object, stroke, _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws a rectangle with rounded edges and dashed lines by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RoundedRectangle structure including the dimension of the rounded rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedRoundedRectangle(IBrush brush, RoundedRectangle rectangle, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawRoundedRectangle(rectangle, brush.NativeBrush.Object, stroke, _strokeStyle?.Object);
        }

        /// <summary>
        ///     Draws a triangle with dashed lines using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="aX">The x-coordinate lower-left corner of the triangle.</param>
        /// <param name="aY">The y-coordinate lower-left corner of the triangle.</param>
        /// <param name="bX">The x-coordinate lower-right corner of the triangle.</param>
        /// <param name="bY">The y-coordinate lower-right corner of the triangle.</param>
        /// <param name="cX">The x-coordinate upper-center corner of the triangle.</param>
        /// <param name="cY">The y-coordinate upper-center corner of the triangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedTriangle(IBrush brush, float aX, float aY, float bX, float bY, float cX, float cY,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            sink.BeginFigure(new D2D_POINT_2F(aX, aY), D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_HOLLOW);
            sink.AddLine(new D2D_POINT_2F(bX, bY));
            sink.AddLine(new D2D_POINT_2F(cX, cY));
            sink.EndFigure(D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED);

            sink.Close();

            _device.Object.DrawGeometry(geometry.Object, brush.NativeBrush.Object, stroke, _strokeStyle?.Object);

            sink.Dispose();
            geometry.Dispose();
        }

        /// <summary>
        ///     Draws a triangle with dashed lines using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="a">A PointF structure including the coordinates of the lower-left corner of the triangle.</param>
        /// <param name="b">A PointF structure including the coordinates of the lower-right corner of the triangle.</param>
        /// <param name="c">A PointF structure including the coordinates of the upper-center corner of the triangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedTriangle(IBrush brush, PointF a, PointF b, PointF c, float stroke)
        {
            DashedTriangle(brush, a.X, a.Y, b.X, b.Y, c.X, c.Y, stroke);
        }

        /// <summary>
        ///     Draws a triangle with dashed lines using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="triangle">A Triangle structure including the dimension of the triangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DashedTriangle(IBrush brush, Triangle triangle, float stroke)
        {
            DashedTriangle(brush, triangle.A.X, triangle.A.Y, triangle.B.X, triangle.B.Y, triangle.C.X, triangle.C.Y,
                stroke);
        }

        /// <summary>
        ///     Destroys an already initialized Graphics surface and frees its resources.
        /// </summary>
        public void Destroy()
        {
            if (!IsInitialized) throw new InvalidOperationException("D2DDevice needs to be initialized first");

            try
            {
                _strokeStyle?.Dispose();
                _fontFactory?.Dispose();
                _factory?.Dispose();
                _device?.Dispose();
            }
            catch
            {
            }

            IsInitialized = false;
        }

        /// <summary>
        ///     Draws a pointed line using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the arrow line.</param>
        /// <param name="startX">The x-coordinate of the start of the arrow line. (the direction it points to)</param>
        /// <param name="startY">The y-coordinate of the start of the arrow line. (the direction it points to)</param>
        /// <param name="endX">The x-coordinate of the end of the arrow line.</param>
        /// <param name="endY">The y-coordinate of the end of the arrow line.</param>
        /// <param name="size">A value determining the size of the arrow line.</param>
        public void DrawArrowLine(IBrush brush, float startX, float startY, float endX, float endY, float size)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var deltaX = endX >= startX ? endX - startX : startX - endX;
            var deltaY = endY >= startY ? endY - startY : startY - endY;

            var length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            var xm = length - size;
            var xn = xm;

            var ym = size;
            var yn = -ym;

            var sin = deltaY / length;
            var cos = deltaX / length;

            var x = xm * cos - ym * sin + endX;
            ym = xm * sin + ym * cos + endY;
            xm = x;

            x = xn * cos - yn * sin + endX;
            yn = xn * sin + yn * cos + endY;
            xn = x;

            FillTriangle(brush, startX, startY, xm, ym, xn, yn);
        }

        /// <summary>
        ///     Draws a pointed line using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the arrow line.</param>
        /// <param name="start">A PointF structure including the start position of the arrow line. (the direction it points to)</param>
        /// <param name="end">A PointF structure including the end position of the arrow line. (the direction it points to)</param>
        /// <param name="size">A value determining the size of the arrow line.</param>
        public void DrawArrowLine(IBrush brush, PointF start, PointF end, float size)
        {
            DrawArrowLine(brush, start.X, start.Y, end.X, end.Y, size);
        }

        /// <summary>
        ///     Draws a pointed line using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the arrow line.</param>
        /// <param name="line">A Line structure including the start (direction) and end point of the arrow line.</param>
        /// <param name="size">A value determining the size of the arrow line.</param>
        public void DrawArrowLine(IBrush brush, Line line, float size)
        {
            DrawArrowLine(brush, line.Start.X, line.Start.Y, line.End.X, line.End.Y, size);
        }

        /// <summary>
        ///     Draws a 2D Box with an outline using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawBox2D(IBrush outline, IBrush fill, float left, float top, float right, float bottom,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var width = right - left;
            var height = bottom - top;

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            sink.BeginFigure(new D2D_POINT_2F(left, top), D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_FILLED);
            sink.AddLine(new D2D_POINT_2F(left + width, top));
            sink.AddLine(new D2D_POINT_2F(left + width, top + height));
            sink.AddLine(new D2D_POINT_2F(left, top + height));
            sink.EndFigure(D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED);

            sink.Close();
            _device.Object.DrawGeometry(geometry.Object, outline.NativeBrush.Object, stroke, null);
            _device.Object.FillGeometry(geometry.Object, fill.NativeBrush.Object, null);

            sink.Dispose();
            geometry.Dispose();
        }

        /// <summary>
        ///     Draws a 2D Box with an outline using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure including the dimension of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawBox2D(IBrush outline, IBrush fill, RectangleF rectangle, float stroke)
        {
            DrawBox2D(outline, fill, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, stroke);
        }

        /// <summary>
        ///     Draws a circle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="x">The x-coordinate of the center of the circle.</param>
        /// <param name="y">The y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DrawCircle(IBrush brush, float x, float y, float radius, float stroke)
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.Object.DrawEllipse(new D2D1_ELLIPSE(x, y, radius, radius), brush.NativeBrush.Object,
                stroke);
        }

        /// <summary>
        ///     Draws a circle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DrawCircle(IBrush brush, PointF location, float radius, float stroke)
        {
            DrawCircle(brush, location.X, location.Y, radius, stroke);
        }

        /// <summary>
        ///     Draws a circle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="circle">A Circle structure which includes the dimension of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DrawCircle(IBrush brush, Circle circle, float stroke)
        {
            DrawCircle(brush, circle.Location.X, circle.Location.Y, circle.Radius, stroke);
        }

        /// <summary>
        ///     Draws a crosshair by using the given brush and style.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the crosshair.</param>
        /// <param name="x">The x-coordinate of the center of the crosshair.</param>
        /// <param name="y">The y-coordinate of the center of the crosshair.</param>
        /// <param name="size">The size of the crosshair in pixels.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        /// <param name="style">A value that determines the appearance of the crosshair.</param>
        public void DrawCrosshair(IBrush brush, float x, float y, float size, float stroke, CrosshairStyle style)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            if (style == CrosshairStyle.Dot)
            {
                FillCircle(brush, x, y, size);
            }
            else if (style == CrosshairStyle.Plus)
            {
                DrawLine(brush, x - size, y, x + size, y, stroke);
                DrawLine(brush, x, y - size, x, y + size, stroke);
            }
            else if (style == CrosshairStyle.Cross)
            {
                DrawLine(brush, x - size, y - size, x + size, y + size, stroke);
                DrawLine(brush, x + size, y - size, x - size, y + size, stroke);
            }
            else if (style == CrosshairStyle.Gap)
            {
                DrawLine(brush, x - size - stroke, y, x - stroke, y, stroke);
                DrawLine(brush, x + size + stroke, y, x + stroke, y, stroke);

                DrawLine(brush, x, y - size - stroke, x, y - stroke, stroke);
                DrawLine(brush, x, y + size + stroke, x, y + stroke, stroke);
            }
            else if (style == CrosshairStyle.Diagonal)
            {
                DrawLine(brush, x - size, y - size, x + size, y + size, stroke);
                DrawLine(brush, x + size, y - size, x - size, y + size, stroke);
            }
        }

        /// <summary>
        ///     Draws a crosshair by using the given brush and style.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the crosshair.</param>
        /// <param name="location">A Location structure including the position of the crosshair.</param>
        /// <param name="size">The size of the crosshair in pixels.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        /// <param name="style">A value that determines the appearance of the crosshair.</param>
        public void DrawCrosshair(IBrush brush, PointF location, float size, float stroke, CrosshairStyle style)
        {
            DrawCrosshair(brush, location.X, location.Y, size, stroke, style);
        }

        /// <summary>
        ///     Draws an ellipse by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="x">The x-coordinate of the center of the ellipse.</param>
        /// <param name="y">The y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of this ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of this ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DrawEllipse(IBrush brush, float x, float y, float radiusX, float radiusY, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawEllipse(new D2D1_ELLIPSE(x, y, radiusX, radiusY), brush.NativeBrush.Object,
                stroke);
        }

        /// <summary>
        ///     Draws an ellipse by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of this ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of this ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DrawEllipse(IBrush brush, PointF location, float radiusX, float radiusY, float stroke)
        {
            DrawEllipse(brush, location.X, location.Y, radiusX, radiusY, stroke);
        }

        /// <summary>
        ///     Draws an ellipse by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="ellipse">An Ellipse structure which includes the dimension of the ellipse.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void DrawEllipse(IBrush brush, Ellipse ellipse, float stroke)
        {
            DrawEllipse(brush, ellipse.Location.X, ellipse.Location.Y, ellipse.RadiusX, ellipse.RadiusY, stroke);
        }

        /// <summary>
        ///     Draws a Geometry using the given brush and thickness.
        /// </summary>
        /// <param name="geometry">The Geometry to be drawn.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="stroke">A value that determines the width/thickness of the lines.</param>
        public void DrawGeometry(Geometry geometry, IBrush brush, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawGeometry(geometry.NativeGeometry.Object, brush.NativeBrush.Object, stroke, null);
        }

        /// <summary>
        ///     Draws a horizontal progrss bar using the given brush, dimension and percentage value.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the progress bar.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        /// <param name="percentage">A value indicating the progress in percent.</param>
        public void DrawHorizontalProgressBar(IBrush outline, IBrush fill, float left, float top, float right,
            float bottom, float stroke, float percentage)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var outer = new D2D_RECT_F(left, top, right, bottom);

            if (percentage < 1.0f)
            {
                _device.Object.DrawRectangle(outer, outline.NativeBrush.Object, stroke);
            }
            else
            {
                var height = bottom - top;
                var filledHeight = height / 100.0f * percentage;

                var halfStroke = stroke * 0.5f;

                var inner = new D2D_RECT_F(left + halfStroke, top + (height - filledHeight) + halfStroke,
                    right - halfStroke, bottom - halfStroke);

                _device.Object.FillRectangle(inner, fill.NativeBrush.Object);
                _device.Object.DrawRectangle(outer, outline.NativeBrush.Object, stroke);
            }
        }

        /// <summary>
        ///     Draws a horizontal progrss bar using the given brush, dimension and percentage value.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the progress bar.</param>
        /// <param name="rectangle">A RectangleF structure including the dimension of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        /// <param name="percentage">A value indicating the progress in percent.</param>
        public void DrawHorizontalProgressBar(IBrush outline, IBrush fill, RectangleF rectangle, float stroke,
            float percentage)
        {
            DrawHorizontalProgressBar(outline, fill, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom,
                stroke, percentage);
        }

        /// <summary>
        ///     Draws an image to the given position and optional applies an alpha value.
        /// </summary>
        /// <param name="image">The Image to be drawn.</param>
        /// <param name="location">A PointF structure including the position of the upper-left corner of the image.</param>
        /// <param name="scale">Scale of the image</param>
        /// <param name="opacity">A value indicating the opacity of the image. (alpha)</param>
        public void DrawImage(Image image, PointF location, float scale = 1.0f, float opacity = 1.0f)
        {
            var rect = RectangleF.Create(location.X,location.Y, scale * image.Width, scale * image.Height);
            DrawImage(image, rect, opacity);
        }

        /// <summary>
        ///     Draws an image to the given position, scales it and optional applies an alpha value.
        /// </summary>
        /// <param name="image">The Image to be drawn.</param>
        /// <param name="rectangle">A RectangleF structure inclduing the dimension of the image.</param>
        /// <param name="opacity">A value indicating the opacity of the image. (alpha)</param>
        /// <param name="linearScale">A Boolean indicating whether linear scaling should be applied</param>
        public void DrawImage(Image image, RectangleF rectangle, float opacity = 1.0f, bool linearScale = true)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var destRect = (D2D_RECT_F)rectangle;
            var srcRect = new D2D_RECT_F(0, 0, image.Bitmap.GetPixelSize().width, image.Bitmap.GetPixelSize().height);
            unsafe
            {
                _device.Object.DrawBitmap(
                    image.Bitmap?.Object!,
                    (nint)(&destRect),
                    opacity,
                    linearScale ? D2D1_BITMAP_INTERPOLATION_MODE.D2D1_BITMAP_INTERPOLATION_MODE_LINEAR : D2D1_BITMAP_INTERPOLATION_MODE.D2D1_BITMAP_INTERPOLATION_MODE_NEAREST_NEIGHBOR,
                    (nint)(&srcRect));
            }
        }

        /// <summary>
        ///     Draws an image to the given position, scales it and optional applies an alpha value.
        /// </summary>
        /// <param name="image">The Image to be drawn.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the image.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the image.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the image.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the image.</param>
        /// <param name="opacity">A value indicating the opacity of the image. (alpha)</param>
        /// <param name="linearScale">A Boolean indicating whether linear scaling should be applied</param>
        public void DrawImage(Image image, float left, float top, float right, float bottom, float opacity = 1.0f,
            bool linearScale = true)
        {
            DrawImage(image, new RectangleF(left, top, right, bottom), opacity, linearScale);
        }

        /// <summary>
        ///     Draws a line starting and ending at the given points.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the line.</param>
        /// <param name="startX">The start position of the line on the x-axis</param>
        /// <param name="startY">The start position of the line on the y-axis</param>
        /// <param name="endX">The end position of the line on the x-axis</param>
        /// <param name="endY">The end position of the line on the y-axis</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawLine(IBrush brush, float startX, float startY, float endX, float endY, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawLine(new D2D_POINT_2F(startX, startY), new D2D_POINT_2F(endX, endY), brush.NativeBrush.Object, stroke);
        }

        /// <summary>
        ///     Draws a line starting and ending at the given points.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the line.</param>
        /// <param name="start">A PointF structure including the start position of the line.</param>
        /// <param name="end">A PointF structure including the end position of the line.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawLine(IBrush brush, PointF start, PointF end, float stroke)
        {
            DrawLine(brush, start.X, start.Y, end.X, end.Y, stroke);
        }

        /// <summary>
        ///     Draws a line starting and ending at the given points.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the line.</param>
        /// <param name="line">A Line structure including the start and end PointF of the line.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawLine(IBrush brush, Line line, float stroke)
        {
            DrawLine(brush, line.Start.X, line.Start.Y, line.End.X, line.End.Y, stroke);
        }

        /// <summary>
        ///     Draws a rectangle by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawRectangle(IBrush brush, float left, float top, float right, float bottom, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawRectangle(new D2D_RECT_F(left, top, right, bottom), brush.NativeBrush.Object, stroke);
        }

        /// <summary>
        ///     Draws a rectangle by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure that determines the boundaries of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawRectangle(IBrush brush, RectangleF rectangle, float stroke)
        {
            DrawRectangle(brush, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, stroke);
        }

        /// <summary>
        ///     Draws the corners (edges) of a rectangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawRectangleEdges(IBrush brush, float left, float top, float right, float bottom, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var width = right - left;
            var height = bottom - top;

            var length = (int)((width + height) / 2.0f * 0.2f);

            var first = new D2D_POINT_2F(left, top);
            var second = new D2D_POINT_2F(left, top + length);
            var third = new D2D_POINT_2F(left + length, top);

            _device.Object.DrawLine(first, second, brush.NativeBrush.Object, stroke);
            _device.Object.DrawLine(first, third, brush.NativeBrush.Object, stroke);

            first.y += height;
            second.y = first.y - length;
            third.y = first.y;
            third.x = first.x + length;

            _device.Object.DrawLine(first, second, brush.NativeBrush.Object, stroke);
            _device.Object.DrawLine(first, third, brush.NativeBrush.Object, stroke);

            first.x = left + width;
            first.y = top;
            second.x = first.x - length;
            second.y = first.y;
            third.x = first.x;
            third.y = first.y + length;

            _device.Object.DrawLine(first, second, brush.NativeBrush.Object, stroke);
            _device.Object.DrawLine(first, third, brush.NativeBrush.Object, stroke);

            first.y += height;
            second.x += length;
            second.y = first.y - length;
            third.y = first.y;
            third.x = first.x - length;

            _device.Object.DrawLine(first, second, brush.NativeBrush.Object, stroke);
            _device.Object.DrawLine(first, third, brush.NativeBrush.Object, stroke);
        }

        /// <summary>
        ///     Draws the corners (edges) of a rectangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure including the dimension of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawRectangleEdges(IBrush brush, RectangleF rectangle, float stroke)
        {
            DrawRectangle(brush, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, stroke);
        }

        /// <summary>
        ///     Draws a rectangle with rounded edges by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="radius">A value that determines radius of corners.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawRoundedRectangle(IBrush brush, float left, float top, float right, float bottom, float radius,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var rect = new D2D1_ROUNDED_RECT
            {
                radiusX = radius,
                radiusY = radius,
                rect = new D2D_RECT_F(left, top, right, bottom)
            };

            _device.Object.DrawRoundedRectangle(rect, brush.NativeBrush.Object, stroke);
        }

        /// <summary>
        ///     Draws a rectangle with rounded edges by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RoundedRectangle structure including the dimension of the rounded rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawRoundedRectangle(IBrush brush, RoundedRectangle rectangle, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.DrawRoundedRectangle(rectangle, brush.NativeBrush.Object, stroke);
        }

        /// <summary>
        ///     Measures the specified string when drawn with the specified Font.
        /// </summary>
        /// <param name="font">Font that defines the text format of the string.</param>
        /// <param name="fontSize">The size of the Font. (does not need to be the same as in Font.FontSize)</param>
        /// <param name="text">String to measure.</param>
        /// <returns>This method returns a PointF containing the width (x) and height (y) of the given text.</returns>
        public PointF MeasureString(Font font, float fontSize, string text)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length == 0) return default;

            var layout = _fontFactory.CreateTextLayout(font.TextFormat, text, text.Length, Width, Height);

            if (fontSize != font.FontSize) layout.Object.SetFontSize(fontSize, new DWRITE_TEXT_RANGE(0, (uint)text.Length));

            layout.Object.GetMetrics(out var metrics);
            var result = new PointF(metrics.width, metrics.height);

            layout.Dispose();

            return result;
        }

        /// <summary>
        ///     Measures the specified string when drawn with the specified Font.
        /// </summary>
        /// <param name="font">Font that defines the text format of the string.</param>
        /// <param name="text">String to measure.</param>
        /// <returns>This method returns a PointF containing the width (x) and height (y) of the given text.</returns>
        public PointF MeasureString(Font font, string text)
        {
            return MeasureString(font, font.FontSize, text);
        }

        /// <summary>
        ///     Draws a string using the given font, size and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="fontSize">The size of the Font. (does not need to be the same as in Font.FontSize)</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="x">The x-coordinate of the starting position.</param>
        /// <param name="y">The y-coordinate of the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawText(Font font, float fontSize, IBrush brush, float x, float y, string text)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length == 0) return;

            var clippedWidth = x < 0 ? Width + x : Width - x;
            var clippedHeight = y < 0 ? Height + y : Height - y;

            if (clippedWidth <= fontSize) clippedWidth = Width;
            if (clippedHeight <= fontSize) clippedHeight = Height;

            var layout = _fontFactory.CreateTextLayout(font.TextFormat, text, text.Length, clippedWidth, clippedHeight);

            if (fontSize != font.FontSize) layout.Object.SetFontSize(fontSize, new DWRITE_TEXT_RANGE(0, (uint)text.Length));

            _device.Object.DrawTextLayout(new D2D_POINT_2F(x, y), layout.Object, brush.NativeBrush.Object, D2D1_DRAW_TEXT_OPTIONS.D2D1_DRAW_TEXT_OPTIONS_CLIP);

            layout.Dispose();
        }

        /// <summary>
        ///     Draws a string using the given font, size and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="fontSize">The size of the Font. (does not need to be the same as in Font.FontSize)</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="location">A PointF structure including the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawText(Font font, float fontSize, IBrush brush, PointF location, string text)
        {
            DrawText(font, fontSize, brush, location.X, location.Y, text);
        }

        /// <summary>
        ///     Draws a string using the given font and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="x">The x-coordinate of the starting position.</param>
        /// <param name="y">The y-coordinate of the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawText(Font font, IBrush brush, float x, float y, string text)
        {
            DrawText(font, font.FontSize, brush, x, y, text);
        }

        /// <summary>
        ///     Draws a string using the given font and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="location">A PointF structure including the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawText(Font font, IBrush brush, PointF location, string text)
        {
            DrawText(font, font.FontSize, brush, location.X, location.Y, text);
        }

        /// <summary>
        ///     Draws a string with a background box in behind using the given font, size and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="fontSize">The size of the Font. (does not need to be the same as in Font.FontSize)</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="background">A brush that determines the color of the background box.</param>
        /// <param name="x">The x-coordinate of the starting position.</param>
        /// <param name="y">The y-coordinate of the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawTextWithBackground(Font font, float fontSize, IBrush brush, IBrush background, float x, float y,
            string text)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length == 0) return;

            var clippedWidth = x < 0 ? Width + x : Width - x;
            var clippedHeight = y < 0 ? Height + y : Height - y;

            if (clippedWidth <= fontSize) clippedWidth = Width;
            if (clippedHeight <= fontSize) clippedHeight = Height;

            var layout = _fontFactory.CreateTextLayout(font.TextFormat, text, text.Length, clippedWidth, clippedHeight);

            if (fontSize != font.FontSize) layout.Object.SetFontSize(fontSize, new DWRITE_TEXT_RANGE(0, (uint)text.Length));

            var fontSizeValue = layout.Object.GetFontSize();
            var modifier = fontSizeValue * 0.25f;
            layout.Object.GetMetrics(out var layoutMetrics);
            var rectangle = new D2D_RECT_F(x - modifier, y - modifier, x + layoutMetrics.width + modifier,
                y + layoutMetrics.height + modifier);

            _device.Object.FillRectangle(rectangle, background.NativeBrush.Object);

            _device.Object.DrawTextLayout(new D2D_POINT_2F(x, y), layout.Object, brush.NativeBrush.Object, D2D1_DRAW_TEXT_OPTIONS.D2D1_DRAW_TEXT_OPTIONS_CLIP);

            layout.Dispose();
        }

        /// <summary>
        ///     Draws a string with a background box in behind using the given font, size and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="fontSize">The size of the Font. (does not need to be the same as in Font.FontSize)</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="background">A brush that determines the color of the background box.</param>
        /// <param name="location">A PointF structure including the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawTextWithBackground(Font font, float fontSize, IBrush brush, IBrush background, PointF location,
            string text)
        {
            DrawTextWithBackground(font, fontSize, brush, background, location.X, location.Y, text);
        }

        /// <summary>
        ///     Draws a string with a background box in behind using the given font, size and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="background">A brush that determines the color of the background box.</param>
        /// <param name="x">The x-coordinate of the starting position.</param>
        /// <param name="y">The y-coordinate of the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawTextWithBackground(Font font, IBrush brush, IBrush background, float x, float y, string text)
        {
            DrawTextWithBackground(font, font.FontSize, brush, background, x, y, text);
        }

        /// <summary>
        ///     Draws a string with a background box in behind using the given font, size and position.
        /// </summary>
        /// <param name="font">The Font to be used to draw the string.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        /// <param name="background">A brush that determines the color of the background box.</param>
        /// <param name="location">A PointF structure including the starting position.</param>
        /// <param name="text">The string to be drawn.</param>
        public void DrawTextWithBackground(Font font, IBrush brush, IBrush background, PointF location, string text)
        {
            DrawTextWithBackground(font, font.FontSize, brush, background, location.X, location.Y, text);
        }

        /// <summary>
        ///     Draws a triangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="aX">The x-coordinate lower-left corner of the triangle.</param>
        /// <param name="aY">The y-coordinate lower-left corner of the triangle.</param>
        /// <param name="bX">The x-coordinate lower-right corner of the triangle.</param>
        /// <param name="bY">The y-coordinate lower-right corner of the triangle.</param>
        /// <param name="cX">The x-coordinate upper-center corner of the triangle.</param>
        /// <param name="cY">The y-coordinate upper-center corner of the triangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawTriangle(IBrush brush, float aX, float aY, float bX, float bY, float cX, float cY, float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            sink.BeginFigure(new D2D_POINT_2F(aX, aY), D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_HOLLOW);
            sink.AddLine(new D2D_POINT_2F(bX, bY));
            sink.AddLine(new D2D_POINT_2F(cX, cY));
            sink.EndFigure(D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED);

            sink.Close();

            _device.Object.DrawGeometry(geometry.Object, brush.NativeBrush.Object, stroke, null);

            sink.Dispose();
            geometry.Dispose();
        }

        /// <summary>
        ///     Draws a triangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="a">A PointF structure including the coordinates of the lower-left corner of the triangle.</param>
        /// <param name="b">A PointF structure including the coordinates of the lower-right corner of the triangle.</param>
        /// <param name="c">A PointF structure including the coordinates of the upper-center corner of the triangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawTriangle(IBrush brush, PointF a, PointF b, PointF c, float stroke)
        {
            DrawTriangle(brush, a.X, a.Y, b.X, b.Y, c.X, c.Y, stroke);
        }

        /// <summary>
        ///     Draws a triangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="triangle">A Triangle structure including the dimension of the triangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void DrawTriangle(IBrush brush, Triangle triangle, float stroke)
        {
            DrawTriangle(brush, triangle.A.X, triangle.A.Y, triangle.B.X, triangle.B.Y, triangle.C.X, triangle.C.Y,
                stroke);
        }

        /// <summary>
        ///     Draws a vertical progrss bar using the given brush, dimension and percentage value.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the progress bar.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        /// <param name="percentage">A value indicating the progress in percent.</param>
        public void DrawVerticalProgressBar(IBrush outline, IBrush fill, float left, float top, float right,
            float bottom, float stroke, float percentage)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var outer = new D2D_RECT_F(left, top, right, bottom);

            if (percentage < 1.0f)
            {
                _device.Object.DrawRectangle(outer, outline.NativeBrush.Object, stroke);
            }
            else
            {
                var width = right - left;
                var filledWidth = width / 100.0f * percentage;

                var halfStroke = stroke * 0.5f;

                var inner = new D2D_RECT_F(left + halfStroke, top + halfStroke,
                    right - (width - filledWidth) - halfStroke, bottom - halfStroke);

                _device.Object.FillRectangle(inner, fill.NativeBrush.Object);
                _device.Object.DrawRectangle(outer, outline.NativeBrush.Object, stroke);
            }
        }

        /// <summary>
        ///     Draws a vertical progrss bar using the given brush, dimension and percentage value.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the progress bar.</param>
        /// <param name="rectangle">A RectangleF structure including the dimension of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        /// <param name="percentage">A value indicating the progress in percent.</param>
        public void DrawVerticalProgressBar(IBrush outline, IBrush fill, RectangleF rectangle, float stroke,
            float percentage)
        {
            DrawVerticalProgressBar(outline, fill, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom,
                stroke, percentage);
        }

        /// <summary>
        ///     Ends the current Scene (Frame).
        /// </summary>
        public void EndScene()
        {
            ThrowIfNotInitialized();
            if (!IsDrawing) return;

            try
            {
                _device.Object.EndDraw(0, 0).ThrowOnError();
            }
            catch
            {
                IsDrawing = false;
                Recreate();
                RecreateResources?.Invoke(this, new RecreateResourcesEventArgs(this));
                return;
            }

            IsDrawing = false;

            if (MeasureFPS && _watch.IsRunning)
            {
                if (_watch.ElapsedMilliseconds >= 1000)
                {
                    FPS = _fpsCount;

                    _fpsCount = 0;

                    _watch.Stop();
                }
                else
                {
                    _fpsCount++;
                }
            }
        }

        /// <summary>
        ///     Returns a value indicating whether this instance and a specified <see cref="T:System.Object" /> represent the same
        ///     type and value.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="obj" /> is a Graphics and equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Graphics gfx)
                return gfx.WindowHandle == WindowHandle
                       && gfx.IsInitialized == IsInitialized
                       && gfx._device?.Equals(_device) == true;
            return false;
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of Graphics represent the same value.
        /// </summary>
        /// <param name="value">An object to compare to this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="value" /> is equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool Equals(Graphics value)
        {
            return value != null
                   && value.WindowHandle == WindowHandle
                   && value.IsInitialized == IsInitialized
                   && value._device?.Equals(_device) == true;
        }

        /// <summary>
        ///     Fills a circle by using the given brush and dimesnion.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="x">The x-coordinate of the center of the circle.</param>
        /// <param name="y">The y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        public void FillCircle(IBrush brush, float x, float y, float radius)
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            _device.Object.FillEllipse(new D2D1_ELLIPSE(x, y, radius, radius), brush.NativeBrush.Object);
        }

        /// <summary>
        ///     Fills a circle by using the given brush and dimesnion.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        public void FillCircle(IBrush brush, PointF location, float radius)
        {
            FillCircle(brush, location.X, location.Y, radius);
        }

        /// <summary>
        ///     Fills a circle by using the given brush and dimesnion.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the circle.</param>
        /// <param name="circle">A Circle structure which includes the dimension of the circle.</param>
        public void FillCircle(IBrush brush, Circle circle)
        {
            FillCircle(brush, circle.Location.X, circle.Location.Y, circle.Radius);
        }

        /// <summary>
        ///     Fills an ellipse by using the given brush and dimesnion.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="x">The x-coordinate of the center of the ellipse.</param>
        /// <param name="y">The y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        public void FillEllipse(IBrush brush, float x, float y, float radiusX, float radiusY)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.FillEllipse(new D2D1_ELLIPSE(x, y, radiusX, radiusY), brush.NativeBrush.Object);
        }

        /// <summary>
        ///     Fills an ellipse by using the given brush and dimesnion.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        public void FillEllipse(IBrush brush, PointF location, float radiusX, float radiusY)
        {
            FillEllipse(brush, location.X, location.Y, radiusX, radiusY);
        }

        /// <summary>
        ///     Fills an ellipse by using the given brush and dimesnion.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the ellipse.</param>
        /// <param name="ellipse">An Ellipse structure which includes the dimension of the ellipse.</param>
        public void FillEllipse(IBrush brush, Ellipse ellipse)
        {
            FillEllipse(brush, ellipse.Location.X, ellipse.Location.Y, ellipse.RadiusX, ellipse.RadiusY);
        }

        /// <summary>
        ///     Fills the Geometry using the given brush.
        /// </summary>
        /// <param name="geometry">The Geometry to be drawn.</param>
        /// <param name="brush">A brush that determines the color of the text.</param>
        public void FillGeometry(Geometry geometry, IBrush brush)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.FillGeometry(geometry.NativeGeometry.Object, brush.NativeBrush.Object, null);
        }

        /// <summary>
        ///     Fills a rectangle by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        public void FillRectangle(IBrush brush, float left, float top, float right, float bottom)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.FillRectangle(new D2D_RECT_F(left, top, right, bottom), brush.NativeBrush.Object);
        }

        /// <summary>
        ///     Fills a rectangle by using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure that determines the boundaries of the rectangle.</param>
        public void FillRectangle(IBrush brush, RectangleF rectangle)
        {
            FillRectangle(brush, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <summary>
        ///     Fills a rounded rectangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="radius">A value that determines radius of corners.</param>
        public void FillRoundedRectangle(IBrush brush, float left, float top, float right, float bottom, float radius)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var rect = new D2D1_ROUNDED_RECT
            {
                radiusX = radius,
                radiusY = radius,
                rect = new D2D_RECT_F(left, top, right, bottom)
            };

            _device.Object.FillRoundedRectangle(rect, brush.NativeBrush.Object);
        }

        /// <summary>
        ///     Fills a rounded rectangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RoundedRectangle structure including the dimension of the rounded rectangle.</param>
        public void FillRoundedRectangle(IBrush brush, RoundedRectangle rectangle)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            _device.Object.FillRoundedRectangle(rectangle, brush.NativeBrush.Object);
        }

        /// <summary>
        ///     Fills a triangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="aX">The x-coordinate lower-left corner of the triangle.</param>
        /// <param name="aY">The y-coordinate lower-left corner of the triangle.</param>
        /// <param name="bX">The x-coordinate lower-right corner of the triangle.</param>
        /// <param name="bY">The y-coordinate lower-right corner of the triangle.</param>
        /// <param name="cX">The x-coordinate upper-center corner of the triangle.</param>
        /// <param name="cY">The y-coordinate upper-center corner of the triangle.</param>
        public void FillTriangle(IBrush brush, float aX, float aY, float bX, float bY, float cX, float cY)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            sink.BeginFigure(new D2D_POINT_2F(aX, aY), D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_FILLED);
            sink.AddLine(new D2D_POINT_2F(bX, bY));
            sink.AddLine(new D2D_POINT_2F(cX, cY));
            sink.EndFigure(D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED);

            sink.Close();

            _device.Object.FillGeometry(geometry.Object, brush.NativeBrush.Object, null);

            sink.Dispose();
            geometry.Dispose();
        }

        /// <summary>
        ///     Fills a triangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="a">A PointF structure including the coordinates of the lower-left corner of the triangle.</param>
        /// <param name="b">A PointF structure including the coordinates of the lower-right corner of the triangle.</param>
        /// <param name="c">A PointF structure including the coordinates of the upper-center corner of the triangle.</param>
        public void FillTriangle(IBrush brush, PointF a, PointF b, PointF c)
        {
            FillTriangle(brush, a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        /// <summary>
        ///     Fills a triangle using the given brush and dimension.
        /// </summary>
        /// <param name="brush">A brush that determines the color of the triangle.</param>
        /// <param name="triangle">A Triangle structure including the dimension of the triangle.</param>
        public void FillTriangle(IBrush brush, Triangle triangle)
        {
            FillTriangle(brush, triangle.A.X, triangle.A.Y, triangle.B.X, triangle.B.Y, triangle.C.X, triangle.C.Y);
        }

        /// <summary>
        ///     Gets the Factory used by this Graphics surface.
        /// </summary>
        /// <returns>The Factory of this Graphics surface.</returns>
        public IComObject<ID2D1Factory> GetFactory()
        {
            ThrowIfNotInitialized();

            return _factory;
        }

        /// <summary>
        ///     Gets the FontFactory used by this Graphics surface.
        /// </summary>
        /// <returns>The FontFactory of this Graphics surface.</returns>
        public IComObject<IDWriteFactory> GetFontFactory()
        {
            ThrowIfNotInitialized();

            return _fontFactory;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return OverrideHelper.HashCodes(
                WindowHandle.GetHashCode(),
                _watch.GetHashCode());
        }

        /// <summary>
        ///     Gets the RenderTarget used by this Graphics surface.
        /// </summary>
        /// <returns>The RenderTarget of this Graphics surface.</returns>
        public IComObject<ID2D1HwndRenderTarget> GetRenderTarget()
        {
            ThrowIfNotInitialized();

            return _device;
        }

        /// <summary>
        ///     Draws a circle with an outline around it using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the circle.</param>
        /// <param name="x">The x-coordinate of the center of the circle.</param>
        /// <param name="y">The y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void OutlineCircle(IBrush outline, IBrush fill, float x, float y, float radius, float stroke)
        {
            if (!IsDrawing) ThrowHelper.UseBeginScene();

            var ellipse = new D2D1_ELLIPSE(x, y, radius, radius);

            _device.Object.DrawEllipse(ellipse, fill.NativeBrush.Object, stroke);

            var halfStroke = stroke * 0.5f;

            ellipse.radiusX += halfStroke;
            ellipse.radiusY += halfStroke;

            _device.Object.DrawEllipse(ellipse, outline.NativeBrush.Object, halfStroke);

            ellipse.radiusX -= stroke;
            ellipse.radiusY -= stroke;

            _device.Object.DrawEllipse(ellipse, outline.NativeBrush.Object, halfStroke);
        }

        /// <summary>
        ///     Draws a circle with an outline around it using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the circle.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void OutlineCircle(IBrush outline, IBrush fill, PointF location, float radius, float stroke)
        {
            OutlineCircle(outline, fill, location.X, location.Y, radius, stroke);
        }

        /// <summary>
        ///     Draws a circle with an outline around it using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the circle.</param>
        /// <param name="circle">A Circle structure which includes the dimension of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void OutlineCircle(IBrush outline, IBrush fill, Circle circle, float stroke)
        {
            OutlineCircle(outline, fill, circle.Location.X, circle.Location.Y, circle.Radius, stroke);
        }

        /// <summary>
        ///     Draws an ellipse with an outline around it using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the ellipse.</param>
        /// <param name="x">The x-coordinate of the center of the ellipse.</param>
        /// <param name="y">The y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void OutlineEllipse(IBrush outline, IBrush fill, float x, float y, float radiusX, float radiusY,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var ellipse = new D2D1_ELLIPSE(x, y, radiusX, radiusY);

            _device.Object.DrawEllipse(ellipse, fill.NativeBrush.Object, stroke);

            var halfStroke = stroke * 0.5f;

            ellipse.radiusX += halfStroke;
            ellipse.radiusY += halfStroke;

            _device.Object.DrawEllipse(ellipse, outline.NativeBrush.Object, halfStroke);

            ellipse.radiusX -= stroke;
            ellipse.radiusY -= stroke;

            _device.Object.DrawEllipse(ellipse, outline.NativeBrush.Object, halfStroke);
        }

        /// <summary>
        ///     Draws an ellipse with an outline around it using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the ellipse.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void OutlineEllipse(IBrush outline, IBrush fill, PointF location, float radiusX, float radiusY,
            float stroke)
        {
            OutlineEllipse(outline, fill, location.X, location.Y, radiusX, radiusY, stroke);
        }

        /// <summary>
        ///     Draws an ellipse with an outline around it using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the ellipse.</param>
        /// <param name="ellipse">An Ellipse structure which includes the dimension of the ellipse.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void OutlineEllipse(IBrush outline, IBrush fill, Ellipse ellipse, float stroke)
        {
            OutlineEllipse(outline, fill, ellipse.Location.X, ellipse.Location.Y, ellipse.RadiusX, ellipse.RadiusY,
                stroke);
        }

        /// <summary>
        ///     Draws a filled circle with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the circle.</param>
        /// <param name="x">The x-coordinate of the center of the circle.</param>
        /// <param name="y">The y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void OutlineFillCircle(IBrush outline, IBrush fill, float x, float y, float radius, float stroke)
        {
            var ellipseGeometry = _factory.CreateEllipseGeometry(new D2D1_ELLIPSE(x, y, radius, radius));

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            ellipseGeometry.Object.Outline(sink.Object);

            sink.Close();

            _device.Object.FillGeometry(geometry.Object, fill.NativeBrush.Object, null);
            _device.Object.DrawGeometry(geometry.Object, outline.NativeBrush.Object, stroke, null);

            sink.Dispose();
            geometry.Dispose();
            ellipseGeometry.Dispose();
        }

        /// <summary>
        ///     Draws a filled circle with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the circle.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void OutlineFillCircle(IBrush outline, IBrush fill, PointF location, float radius, float stroke)
        {
            OutlineFillCircle(outline, fill, location.X, location.Y, radius, stroke);
        }

        /// <summary>
        ///     Draws a filled circle with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the circle.</param>
        /// <param name="circle">A Circle structure which includes the dimension of the circle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the circle.</param>
        public void OutlineFillCircle(IBrush outline, IBrush fill, Circle circle, float stroke)
        {
            OutlineFillCircle(outline, fill, circle.Location.X, circle.Location.Y, circle.Radius, stroke);
        }

        /// <summary>
        ///     Draws a filled ellipse with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the ellipse.</param>
        /// <param name="x">The x-coordinate of the center of the ellipse.</param>
        /// <param name="y">The y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void OutlineFillEllipse(IBrush outline, IBrush fill, float x, float y, float radiusX, float radiusY,
            float stroke)
        {
            var ellipseGeometry = _factory.CreateEllipseGeometry(new D2D1_ELLIPSE(x, y, radiusX, radiusY));

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            ellipseGeometry.Object.Outline(sink.Object);

            sink.Close();

            _device.Object.FillGeometry(geometry.Object, fill.NativeBrush.Object, null);
            _device.Object.DrawGeometry(geometry.Object, outline.NativeBrush.Object, stroke, null);

            sink.Dispose();
            geometry.Dispose();
            ellipseGeometry.Dispose();
        }

        /// <summary>
        ///     Draws a filled ellipse with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the ellipse.</param>
        /// <param name="location">A PointF structureure which includes the x- and y-coordinate of the center of the ellipse.</param>
        /// <param name="radiusX">The radius of the ellipse on the x-axis.</param>
        /// <param name="radiusY">The radius of the ellipse on the y-axis.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void OutlineFillEllipse(IBrush outline, IBrush fill, PointF location, float radiusX, float radiusY,
            float stroke)
        {
            OutlineFillEllipse(outline, fill, location.X, location.Y, radiusX, radiusY, stroke);
        }

        /// <summary>
        ///     Draws a filled ellipse with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the ellipse.</param>
        /// <param name="ellipse">An Ellipse structure which includes the dimension of the ellipse.</param>
        /// <param name="stroke">A value that determines the width/thickness of the ellipse.</param>
        public void OutlineFillEllipse(IBrush outline, IBrush fill, Ellipse ellipse, float stroke)
        {
            OutlineFillEllipse(outline, fill, ellipse.Location.X, ellipse.Location.Y, ellipse.RadiusX, ellipse.RadiusY,
                stroke);
        }

        /// <summary>
        ///     Draws a filled rectangle with an outline around it by using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineFillRectangle(IBrush outline, IBrush fill, float left, float top, float right, float bottom,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var rectangleGeometry = _factory.CreateRectangleGeometry(new D2D_RECT_F(left, top, right, bottom));

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            rectangleGeometry.Object.Widen(stroke, null, 0, 0.25f, sink.Object);
            rectangleGeometry.Object.Outline(sink.Object);

            sink.Close();

            _device.Object.FillGeometry(geometry.Object, fill.NativeBrush.Object, null);
            _device.Object.DrawGeometry(geometry.Object, outline.NativeBrush.Object, stroke, null);

            sink.Dispose();
            geometry.Dispose();
            rectangleGeometry.Dispose();
        }

        /// <summary>
        ///     Draws a filled rectangle with an outline around it by using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure that determines the boundaries of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineFillRectangle(IBrush outline, IBrush fill, RectangleF rectangle, float stroke)
        {
            OutlineFillRectangle(outline, fill, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom,
                stroke);
        }

        /// <summary>
        ///     Draws a line at the given start and end point with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the line.</param>
        /// <param name="startX">The start position of the line on the x-axis</param>
        /// <param name="startY">The start position of the line on the y-axis</param>
        /// <param name="endX">The end position of the line on the x-axis</param>
        /// <param name="endY">The end position of the line on the y-axis</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineLine(IBrush outline, IBrush fill, float startX, float startY, float endX, float endY,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var geometry = _factory.CreatePathGeometry();

            var sink = geometry.Open();

            var half = stroke / 2.0f;

            sink.BeginFigure(new D2D_POINT_2F(startX, startY - half), D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_FILLED);

            sink.AddLine(new D2D_POINT_2F(endX, endY - half));
            sink.AddLine(new D2D_POINT_2F(endX, endY + half));
            sink.AddLine(new D2D_POINT_2F(startX, startY + half));

            sink.EndFigure(D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED);

            _device.Object.DrawGeometry(geometry.Object, outline.NativeBrush.Object, half, null);
            _device.Object.FillGeometry(geometry.Object, fill.NativeBrush.Object, null);

            sink.Dispose();
            geometry.Dispose();
        }

        /// <summary>
        ///     Draws a line at the given start and end point with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the line.</param>
        /// <param name="start">A PointF structure including the start position of the line.</param>
        /// <param name="end">A PointF structure including the end position of the line.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineLine(IBrush outline, IBrush fill, PointF start, PointF end, float stroke)
        {
            OutlineLine(outline, fill, start.X, start.Y, end.X, end.Y, stroke);
        }

        /// <summary>
        ///     Draws a line at the given start and end point with an outline around it.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the line.</param>
        /// <param name="line">A Line structure including the start and end PointF of the line.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineLine(IBrush outline, IBrush fill, Line line, float stroke)
        {
            OutlineLine(outline, fill, line.Start.X, line.Start.Y, line.End.X, line.End.Y, stroke);
        }

        /// <summary>
        ///     Draws a rectangle with an outline around it by using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the rectangle.</param>
        /// <param name="left">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="top">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="right">The x-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="bottom">The y-coordinate of the lower-right corner of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineRectangle(IBrush outline, IBrush fill, float left, float top, float right, float bottom,
            float stroke)
        {
            if (!IsDrawing) throw ThrowHelper.UseBeginScene();

            var halfStroke = stroke / 2.0f;

            var width = right;
            var height = bottom;

            _device.Object.DrawRectangle(
                new D2D_RECT_F(left - halfStroke, top - halfStroke, width + halfStroke, height + halfStroke),
                outline.NativeBrush.Object, halfStroke);

            _device.Object.DrawRectangle(
                new D2D_RECT_F(left + halfStroke, top + halfStroke, width - halfStroke, height - halfStroke),
                outline.NativeBrush.Object, halfStroke);

            _device.Object.DrawRectangle(new D2D_RECT_F(left, top, width, height), fill.NativeBrush.Object, halfStroke);
        }

        /// <summary>
        ///     Draws a rectangle with an outline around it by using the given brush and dimension.
        /// </summary>
        /// <param name="outline">A brush that determines the color of the outline.</param>
        /// <param name="fill">A brush that determines the color of the rectangle.</param>
        /// <param name="rectangle">A RectangleF structure that determines the boundaries of the rectangle.</param>
        /// <param name="stroke">A value that determines the width/thickness of the line.</param>
        public void OutlineRectangle(IBrush outline, IBrush fill, RectangleF rectangle, float stroke)
        {
            OutlineRectangle(outline, fill, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, stroke);
        }

        /// <summary>
        ///     Destroys the current drawing device and creates a new one with the same attributes.
        /// </summary>
        /// <param name="hwnd">Uses the new window as the surface if set.</param>
        public void Recreate(IntPtr hwnd = default)
        {
            if (!IsInitialized) throw ThrowHelper.DeviceNotInitialized();
            if (IsDrawing) throw new InvalidOperationException();

            if (hwnd != default) WindowHandle = hwnd;

            try
            {
                _device.Dispose();
            }
            catch
            {
            }

            CreateDeviceForCurrentSurface();
        }

        /// <summary>
        ///     Tells the Graphics surface to resize itself on the next Scene.
        /// </summary>
        /// <param name="width">A value Determining the new width of this Graphics surface.</param>
        /// <param name="height">A value Determining the new height of this Graphics surface.</param>
        public void Resize(int width, int height)
        {
            if (Width == width && Height == height) return;

            if (IsInitialized)
            {
                _resizeWidth = width;
                _resizeHeight = height;
                _resize = true;
            }
            else
            {
                Width = width;
                Height = height;
            }
        }

        /// <summary>
        ///     Sets up and finishes the initialization of this Graphics surface by using this objects properties.
        /// </summary>
        public void Setup()
        {
            if (IsInitialized) throw new InvalidOperationException("Graphics device is already initialized");
            if (Width <= 0 || Height <= 0) throw new ArgumentOutOfRangeException("Width or Height is not valid");
            if (WindowHandle == IntPtr.Zero) throw new ArgumentOutOfRangeException("WindowHandle is zero");
            if (!Functions.IsWindow(new HWND(WindowHandle))) throw new ArgumentOutOfRangeException("WindowHandle is not valid");

            _factory = D2D1Functions.D2D1CreateFactory(UseMultiThreadedFactories ? D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED : D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED);
            Functions.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, typeof(IDWriteFactory).GUID, out var dwritePtr).ThrowOnError();
            _fontFactory = ComObject.FromPointer<IDWriteFactory>(dwritePtr)!;

            CreateDeviceForCurrentSurface();

            var strokeProps = new D2D1_STROKE_STYLE_PROPERTIES
            {
                dashCap = D2D1_CAP_STYLE.D2D1_CAP_STYLE_FLAT,
                dashOffset = -1.0f,
                dashStyle = D2D1_DASH_STYLE.D2D1_DASH_STYLE_DASH,
                endCap = D2D1_CAP_STYLE.D2D1_CAP_STYLE_FLAT,
                lineJoin = D2D1_LINE_JOIN.D2D1_LINE_JOIN_MITER_OR_BEVEL,
                miterLimit = 1.0f,
                startCap = D2D1_CAP_STYLE.D2D1_CAP_STYLE_FLAT
            };
            _strokeStyle = _factory.CreateStrokeStyle(strokeProps);

            IsInitialized = true;
        }

        /// <summary>
        ///     Converts this Graphics instance to a human-readable string.
        /// </summary>
        /// <returns>A string representation of this Graphics.</returns>
        public override string ToString()
        {
            return OverrideHelper.ToString(
                "WindowHandle", WindowHandle.ToString("X"),
                "Width", Width.ToString(),
                "Height", Height.ToString(),
                "IsInitialized", IsInitialized.ToString(),
                "IsDrawing", IsDrawing.ToString(),
                "AntiAliasing", (PerPrimitiveAntiAliasing || TextAntiAliasing).ToString(),
                "VSync", VSync.ToString());
        }

        /// <summary>
        ///     Creates a new Scene which handles BeginScene and EndScene within a using block.
        /// </summary>
        /// <returns>The Scene this method creates.</returns>
        public Scene UseScene()
        {
            return new Scene(this);
        }

        #region IDisposable Support

        private bool disposedValue;

        /// <summary>
        ///     Releases all resources used by this Graphics surface.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (IsInitialized)
                {
                    if (IsDrawing)
                        try
                        {
                            _device.EndDraw();
                        }
                        catch
                        {
                        }

                    Destroy();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        ///     Releases all resources used by this Graphics surface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}