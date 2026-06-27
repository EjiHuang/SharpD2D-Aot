using System;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents a linear gradient brush used with a Graphics surface.
    /// </summary>
    public class LinearGradientBrush : IDisposable, IBrush
    {
        private IComObject<ID2D1LinearGradientBrush> _brush;
        private IComObject<ID2D1GradientStopCollection> _stopCollection;

        private LinearGradientBrush()
        {
        }

        /// <summary>
        ///     Initializes a new LinearGradientBrush using the target device and an Color[].
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="colors">The colors</param>
        public LinearGradientBrush(IComObject<ID2D1RenderTarget> renderTarget, params Color[] colors)
        {
            if (renderTarget == null) throw new ArgumentNullException(nameof(renderTarget));
            if (colors == null || colors.Length == 0) throw new ArgumentNullException(nameof(colors));

            var position = 0.0f;
            var stepSize = 1.0f / colors.Length;
            var gradientStops = new D2D1_GRADIENT_STOP[colors.Length];

            for (var i = 0; i < colors.Length; i++)
            {
                gradientStops[i] = new D2D1_GRADIENT_STOP
                {
                    color = new D3DCOLORVALUE { r = colors[i].R, g = colors[i].G, b = colors[i].B, a = colors[i].A },
                    position = position
                };
                position += stepSize;
            }

            _stopCollection = renderTarget.CreateGradientStopCollection(gradientStops,
                D2D1_GAMMA.D2D1_GAMMA_2_2, D2D1_EXTEND_MODE.D2D1_EXTEND_MODE_CLAMP);

            var props = new D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES
            {
                startPoint = new D2D_POINT_2F(0, 0),
                endPoint = new D2D_POINT_2F(0, 0)
            };
            _brush = renderTarget.CreateLinearGradientBrush(props, _stopCollection);
        }

        /// <summary>
        ///     Gets the underlying native brush.
        /// </summary>
        public IComObject<ID2D1Brush> NativeBrush => _brush!;

        /// <summary>
        ///     Gets or sets the start point of this LinearGradientBrush.
        /// </summary>
        public D2D_POINT_2F Start
        {
            get => _brush.Object.GetStartPoint();
            set => _brush.Object.SetStartPoint(value);
        }

        /// <summary>
        ///     Gets or sets the end point of this LinearGradientBrush.
        /// </summary>
        public D2D_POINT_2F End
        {
            get => _brush.Object.GetEndPoint();
            set => _brush.Object.SetEndPoint(value);
        }

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~LinearGradientBrush()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Releases all resources used by this LinearGradientBrush.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _brush?.Dispose();
                _brush = null;
                _stopCollection?.Dispose();
                _stopCollection = null;
            }
        }

        /// <summary>
        ///     Releases all resources used by this LinearGradientBrush.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
