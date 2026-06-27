using System;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents a SolidBrush which is used for drawing on a Graphics surface.
    /// </summary>
    public class SolidBrush : IDisposable, IBrush
    {
        private IComObject<ID2D1SolidColorBrush> _brush;

        private SolidBrush()
        {
        }

        /// <summary>
        ///     Initializes a new SolidBrush for the given render target using a transparent Color.
        /// </summary>
        /// <param name="renderTarget">A render target.</param>
        public SolidBrush(IComObject<ID2D1RenderTarget> renderTarget)
        {
            if (renderTarget == null) throw new ArgumentNullException(nameof(renderTarget));
            var color = new D3DCOLORVALUE { r = 0, g = 0, b = 0, a = 0 };
            _brush = renderTarget.CreateSolidColorBrush(color);
        }

        /// <summary>
        ///     Initializes a new SolidBrush for the given render target using the given Color.
        /// </summary>
        /// <param name="renderTarget">A render target.</param>
        /// <param name="color">A Color structure including the color components for this SolidBrush.</param>
        public SolidBrush(IComObject<ID2D1RenderTarget> renderTarget, Color color)
        {
            if (renderTarget == null) throw new ArgumentNullException(nameof(renderTarget));
            _brush = renderTarget.CreateSolidColorBrush((D3DCOLORVALUE)color);
        }

        /// <summary>
        ///     Gets the underlying native brush.
        /// </summary>
        public IComObject<ID2D1Brush> NativeBrush => _brush!;

        /// <summary>
        ///     Gets or sets the Color of the underlying Brush.
        /// </summary>
        public Color Color
        {
            get
            {
                var c = _brush.Object.GetColor();
                return new Color(c.r, c.g, c.b, c.a);
            }
            set
            {
                var c = new D3DCOLORVALUE { r = value.R, g = value.G, b = value.B, a = value.A };
                _brush.Object.SetColor(c);
            }
        }

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~SolidBrush()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Returns a value indicating whether this instance and a specified <see cref="T:System.Object" /> represent the same
        ///     type and value.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="obj" /> is a SolidBrush and equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is SolidBrush value)
                return value._brush == _brush;
            return false;
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of SolidBrush represent the same value.
        /// </summary>
        /// <param name="value">An object to compare to this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="value" /> is equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool Equals(SolidBrush value)
        {
            return value != null
                   && value._brush == _brush;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return OverrideHelper.HashCodes(
                _brush.GetHashCode());
        }

        /// <summary>
        ///     Converts this SolidBrush to a human-readable string.
        /// </summary>
        /// <returns>The string representation of this SolidBrush.</returns>
        public override string ToString()
        {
            return OverrideHelper.ToString(
                "Brush", "SolidBrush",
                "Color", Color.ToString());
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of SolidBrush represent the same value.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public static bool Equals(SolidBrush left, SolidBrush right)
        {
            return left?.Equals(right) == true;
        }

        #region IDisposable Support

        private bool disposedValue;

        /// <summary>
        ///     Releases all resources used by this SolidBrush.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_brush != null)
                {
                    _brush.Dispose();
                    _brush = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        ///     Releases all resources used by this SolidBrush.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

}
