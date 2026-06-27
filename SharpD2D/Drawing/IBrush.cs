using DirectN;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents a Brush used to draw with a Graphics surface.
    /// </summary>
    public interface IBrush
    {
        /// <summary>
        ///     Gets the underlying native brush.
        /// </summary>
        IComObject<ID2D1Brush> NativeBrush { get; }
    }
}